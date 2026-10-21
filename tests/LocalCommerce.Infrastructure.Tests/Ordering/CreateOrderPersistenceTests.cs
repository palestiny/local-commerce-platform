using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Ordering.CreateOrder;
using LocalCommerce.Domain.Ordering;
using LocalCommerce.Infrastructure;
using LocalCommerce.Infrastructure.Ordering;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;


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

    private static string QuoteGeneratedSchemaIdentifier(string schema)
    {
        if (string.IsNullOrWhiteSpace(schema) ||
            schema.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            throw new ArgumentException("Schema identifiers must contain only ASCII letters, digits, and underscores.", nameof(schema));
        }

        return $"\"{schema}\"";
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

    private static CreateOrderHandler Handler(CommerceDbContext db, ICartCheckout? checkout=null, IOrderWriter? writer=null, IOrderNumberGenerator? generator=null, ICreateOrderUnitOfWork? unitOfWork=null)
        => new(new EfCartReader(db),checkout??new EfCartCheckout(db),new EfStoreReader(db),new EfProductReader(db),
            writer??new EfOrderWriter(db),new EfGeneralizedIdempotencyStore(db),unitOfWork??new EfCreateOrderUnitOfWork(db),
            generator??new SequentialOrderNumberGenerator());

    [Fact]
    public async Task Successful_create_persists_snapshot_and_consumes_cart()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var s=await SeedAsync(db);
        var result=await Handler(db).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,"key-1"));

        var order=await db.Orders.Include(x=>x.Items).SingleAsync(x=>x.Id==result.OrderId);
        var cart=await db.Carts.AsNoTracking().SingleAsync(x=>x.Id==s.cartId);

        Assert.Equal(s.customerId, order.CustomerId);
        Assert.Equal("Milk",order.Items.Single().ProductName);
        Assert.Equal(12.50m,order.Items.Single().UnitPrice);
        Assert.Equal(2,order.Items.Single().Quantity);
        Assert.False(cart.IsActive);
    }

    [Fact]
    public async Task Idempotency_key_at_storage_limit_is_accepted()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var s=await SeedAsync(db);
        var key=new string('k',256);

        await Handler(db).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,key));

        var record=await db.IdempotencyRecords.SingleAsync();
        Assert.Equal(key,record.IdempotencyKey);
        Assert.Equal(1,await db.Orders.CountAsync());
    }

    [Fact]
    public async Task Idempotency_key_over_storage_limit_is_rejected_before_persistence()
    {
        await using var db=CreateDb(); await DatabaseInitializer.InitializeAsync(db); await ResetAsync(db);
        var s=await SeedAsync(db);
        var key=new string('k',257);

        var error=await Assert.ThrowsAsync<CreateOrderRejectedException>(()=>
            Handler(db).HandleAsync(new CreateOrderCommand(s.customerId,s.cartId,key)));

        Assert.Equal(LocalCommerce.Application.Errors.ApplicationErrorCodes.RequestInvalid,error.Code);
        Assert.Equal(0,await db.IdempotencyRecords.CountAsync());
        Assert.Equal(0,await db.Orders.CountAsync());
        Assert.True((await db.Carts.SingleAsync(x=>x.Id==s.cartId)).IsActive);
    }

    [Fact]
    public async Task Initializer_adds_customer_ownership_to_an_existing_legacy_schema()
    {
        var schema = "migration_test_" + Guid.NewGuid().ToString("N");
        await using var admin = CreateDb();
        // Schema identifiers cannot be SQL parameters; the helper strictly validates the generated identifier.
#pragma warning disable EF1003 // Validated SQL identifier; parameterization is not supported for identifiers.
        await admin.Database.ExecuteSqlRawAsync("CREATE SCHEMA " + QuoteGeneratedSchemaIdentifier(schema));
#pragma warning restore EF1003

        try
        {
            var options = new DbContextOptionsBuilder<CommerceDbContext>()
                .UseNpgsql($"{ConnectionString};Search Path={schema}")
                .Options;
            await using var legacyDb = new CommerceDbContext(options);

            var assembly = typeof(DatabaseInitializer).Assembly;
            await using var stream = assembly.GetManifestResourceStream(
                "LocalCommerce.Infrastructure.Persistence.Migrations.001_initial.sql")
                ?? throw new InvalidOperationException("Initial SQL migration resource is missing.");
            using var reader = new StreamReader(stream);
            var legacySql = await reader.ReadToEndAsync();
            legacySql = legacySql.Replace("\"CustomerId\" uuid NULL,", "", StringComparison.Ordinal);
            await legacyDb.Database.ExecuteSqlRawAsync(legacySql);

            // Seed an existing order before applying the ownership migration. Its owner must remain unknown.
            var legacyStoreId = Guid.NewGuid();
            var legacyOrderId = Guid.NewGuid();
            var legacyOrderNumber = "LEGACY-" + Guid.NewGuid().ToString("N");
            var legacyTimestamp = DateTimeOffset.UtcNow;
            await legacyDb.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "Stores" ("Id", "IsActive") VALUES ({legacyStoreId}, TRUE)""");
            await legacyDb.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "Orders" ("Id", "StoreId", "OrderNumber", "Status", "CreatedAt", "UpdatedAt")
                    VALUES ({legacyOrderId}, {legacyStoreId}, {legacyOrderNumber}, {"Pending"}, {legacyTimestamp}, {legacyTimestamp})""");

            await DatabaseInitializer.InitializeAsync(legacyDb);

            var exists = await legacyDb.Database.SqlQueryRaw<bool>("""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema()
                      AND table_name = 'Orders'
                      AND column_name = 'CustomerId'
                ) AS "Value"
                """).SingleAsync();

            Assert.True(exists);

            var preservedLegacyOrder = await legacyDb.Database.SqlQuery<Guid>(
                $"""SELECT "Id" AS "Value" FROM "Orders" WHERE "Id" = {legacyOrderId}""")
                .SingleAsync();
            var legacyCustomerId = await legacyDb.Database.SqlQuery<Guid?>(
                $"""SELECT "CustomerId" AS "Value" FROM "Orders" WHERE "Id" = {legacyOrderId}""")
                .SingleAsync();

            Assert.Equal(legacyOrderId, preservedLegacyOrder);
            Assert.Null(legacyCustomerId);
        }
        finally
        {
            // Schema identifiers cannot be SQL parameters; the helper strictly validates the generated identifier.
#pragma warning disable EF1003 // Validated SQL identifier; parameterization is not supported for identifiers.
            await admin.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS " + QuoteGeneratedSchemaIdentifier(schema) + " CASCADE");
#pragma warning restore EF1003
        }
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
    public async Task Sequential_retry_with_same_key_returns_original_order_after_cart_consumed()
    {
        await using var seedDb = CreateDb();
        await DatabaseInitializer.InitializeAsync(seedDb);
        await ResetAsync(seedDb);
        var s = await SeedAsync(seedDb);
        var command = new CreateOrderCommand(s.customerId, s.cartId, "replay-key");

        CreateOrderResult first;
        await using (var firstDb = CreateDb())
            first = await Handler(firstDb).HandleAsync(command);

        await using (var retryDb = CreateDb())
        {
            var replay = await Handler(retryDb).HandleAsync(command);
            Assert.Equal(first, replay);
        }

        await using var verifyDb = CreateDb();
        Assert.Equal(1, await verifyDb.Orders.CountAsync());
        Assert.Equal(1, await verifyDb.IdempotencyRecords.CountAsync());
        Assert.False((await verifyDb.Carts.SingleAsync(x => x.Id == s.cartId)).IsActive);
    }

    [Fact]
    public async Task Retry_after_committed_order_but_lost_response_replays_original_result()
    {
        await using var seedDb = CreateDb();
        await DatabaseInitializer.InitializeAsync(seedDb);
        await ResetAsync(seedDb);
        var s = await SeedAsync(seedDb);
        var command = new CreateOrderCommand(s.customerId, s.cartId, "lost-response-key");

        await using (var firstDb = CreateDb())
        {
            var unitOfWork = new CommitThenLoseResponseUnitOfWork(new EfCreateOrderUnitOfWork(firstDb));
            await Assert.ThrowsAsync<IOException>(() =>
                Handler(firstDb, unitOfWork: unitOfWork).HandleAsync(command));
        }

        await using (var retryDb = CreateDb())
        {
            var replay = await Handler(retryDb).HandleAsync(command);
            await using var verifyDb = CreateDb();
            var persisted = await verifyDb.Orders.SingleAsync();
            Assert.Equal(persisted.Id, replay.OrderId);
            Assert.Equal(1, await verifyDb.IdempotencyRecords.CountAsync());
            Assert.False((await verifyDb.Carts.SingleAsync(x => x.Id == s.cartId)).IsActive);
        }
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

    private sealed class CommitThenLoseResponseUnitOfWork(ICreateOrderUnitOfWork inner) : ICreateOrderUnitOfWork
    {
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            await inner.ExecuteAsync(operation, cancellationToken);
            throw new IOException("Simulated lost response after the database transaction committed.");
        }
    }

    private sealed class ThrowingCartCheckout : ICartCheckout
    {
        public Task ConsumeAsync(Guid cartId,CancellationToken cancellationToken)
            => throw new InvalidOperationException("forced persistence failure");
    }

    private sealed class SaveThenFailOrderWriter(CommerceDbContext db) : IOrderWriter
    {
        public async Task AddAsync(Order order,Guid customerId,string orderNumber,CancellationToken ct)
        {
            await new EfOrderWriter(db).AddAsync(order,customerId,orderNumber,ct);
            await db.SaveChangesAsync(ct);
        }
    }
}
