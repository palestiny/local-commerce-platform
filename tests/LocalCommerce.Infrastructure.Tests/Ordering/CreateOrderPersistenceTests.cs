using LocalCommerce.Application.Ordering.CreateOrder;
using LocalCommerce.Domain.Ordering;
using LocalCommerce.Infrastructure;
using LocalCommerce.Infrastructure.Ordering;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LocalCommerce.Infrastructure.Tests.Ordering;

public sealed class CreateOrderPersistenceTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>().UseNpgsql(ConnectionString).Options);

    private static async Task ResetAsync(CommerceDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "IdempotencyRecords","OrderItems","Orders","CartLines","Carts","Products","Stores" CASCADE
            """);
    }

    private static async Task<(Guid customerId, Guid cartId, Guid productId, Guid storeId)> SeedAsync(CommerceDbContext db)
    {
        var customerId=Guid.NewGuid(); var cartId=Guid.NewGuid(); var productId=Guid.NewGuid(); var storeId=Guid.NewGuid();
        db.Stores.Add(new StoreEntity{Id=storeId,IsActive=true});
        db.Products.Add(new ProductEntity{Id=productId,StoreId=storeId,Name="Milk",VariantName="1L",UnitPrice=12.50m,IsOrderable=true});
        db.Carts.Add(new CartEntity{Id=cartId,CustomerId=customerId,StoreId=storeId,IsActive=true,
            Lines=[new CartLineEntity{Id=Guid.NewGuid(),CartId=cartId,ProductId=productId,VariantName="1L",Quantity=2}]});
        await db.SaveChangesAsync();
        return(customerId,cartId,productId,storeId);
    }

    private static CreateOrderHandler Handler(CommerceDbContext db, ICartCheckout? checkout=null, IOrderWriter? writer=null, IOrderNumberGenerator? generator=null)
        => new(new EfCartReader(db),checkout??new EfCartCheckout(db),new EfStoreReader(db),new EfProductReader(db),
            writer??new EfOrderWriter(db),new EfIdempotencyStore(db),new EfCreateOrderUnitOfWork(db),
            generator??new SequentialOrderNumberGenerator());

    [Fact]
    public async Task Successful_create_persists_snapshot_and_consumes_cart()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var s=await SeedAsync(db);
        var result=await Handler(db).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,"key-1"));

        var order=await db.Orders.Include(x=>x.Items).SingleAsync(x=>x.Id==result.OrderId);
        var cart=await db.Carts.SingleAsync(x=>x.Id==s.cartId);

        Assert.Equal("Milk",order.Items.Single().ProductName);
        Assert.Equal(12.50m,order.Items.Single().UnitPrice);
        Assert.Equal(2,order.Items.Single().Quantity);
        Assert.False(cart.IsActive);
    }

    [Fact]
    public async Task Same_idempotency_key_converges_to_one_order()
    {
        await using var seedDb=CreateDb(); await DatabaseInitializer.InitializeAsync(seedDb); await ResetAsync(seedDb);
        var s=await SeedAsync(seedDb);

        await using var db1=CreateDb(); await using var db2=CreateDb();
        var command=new CreateOrderCommand(s.customerId,s.cartId,"same-key");
        var results=await Task.WhenAll(Handler(db1).HandleAsync(command),Handler(db2).HandleAsync(command));

        Assert.Equal(results[0].OrderId,results[1].OrderId);
        await using var verify=CreateDb();
        Assert.Equal(1,await verify.Orders.CountAsync());
    }

    [Fact]
    public async Task Concurrent_different_keys_on_same_cart_create_only_one_order()
    {
        await using var seedDb=CreateDb(); await DatabaseInitializer.InitializeAsync(seedDb); await ResetAsync(seedDb);
        var s=await SeedAsync(seedDb);

        await using var db1=CreateDb(); await using var db2=CreateDb();
        var a=Handler(db1).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,"key-a"));
        var b=Handler(db2).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,"key-b"));
        var settled=await Task.WhenAll(Capture(a),Capture(b));

        Assert.Equal(1,settled.Count(x=>x.Success));
        await using var verify=CreateDb();
        Assert.Equal(1,await verify.Orders.CountAsync());
        Assert.False((await verify.Carts.SingleAsync(x=>x.Id==s.cartId)).IsActive);
    }

    [Fact]
    public async Task Failure_after_order_write_rolls_back_order_and_cart()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var s=await SeedAsync(db);
        var writer=new SaveThenFailOrderWriter(db);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>
            Handler(db,new ThrowingCartCheckout(),writer).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,"rollback-key")));

        Assert.Equal(0,await db.Orders.CountAsync());
        Assert.Equal(0,await db.IdempotencyRecords.CountAsync());
        Assert.True((await db.Carts.SingleAsync(x=>x.Id==s.cartId)).IsActive);
    }

    [Fact]
    public async Task Reusing_key_with_different_cart_is_rejected()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var first=await SeedAsync(db);
        var secondCart=Guid.NewGuid();
        db.Carts.Add(new CartEntity{Id=secondCart,CustomerId=first.customerId,StoreId=first.storeId,IsActive=true,
            Lines=[new CartLineEntity{Id=Guid.NewGuid(),CartId=secondCart,ProductId=first.productId,VariantName="1L",Quantity=1}]});
        await db.SaveChangesAsync();

        var handler=Handler(db);
        await handler.HandleAsync(new CreateOrderCommand(first.customerId,first.cartId,"conflict-key"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(()=>
            handler.HandleAsync(new CreateOrderCommand(first.customerId,secondCart,"conflict-key")));
    }

    private static async Task<(bool Success, CreateOrderResult? Result)> Capture(Task<CreateOrderResult> task)
    {
        try { return (true,await task); } catch(CreateOrderRejectedException) { return (false,null); }
    }

    private sealed class ThrowingCartCheckout : ICartCheckout
    {
        public Task ConsumeAsync(Guid cartId,CancellationToken cancellationToken)
            => throw new InvalidOperationException("forced persistence failure");
    }

    private sealed class SaveThenFailOrderWriter(CommerceDbContext db) : IOrderWriter
    {
        public async Task AddAsync(Order order,string orderNumber,CancellationToken ct)
        {
            await new EfOrderWriter(db).AddAsync(order,orderNumber,ct);
            await db.SaveChangesAsync(ct);
        }
    }
}
