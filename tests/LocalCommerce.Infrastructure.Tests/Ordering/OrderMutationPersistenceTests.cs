using LocalCommerce.Domain.Ordering;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalCommerce.Infrastructure.Tests.Ordering;

public sealed class OrderMutationPersistenceTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    [Fact]
    public async Task Order_mutation_repository_reloads_full_order_and_persists_status()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var storeId = Guid.NewGuid();
        var order = CreateOrder(storeId);
        db.Stores.Add(new StoreEntity { Id = storeId, IsActive = true });
        db.Orders.Add(new OrderEntity
        {
            Id = order.Id,
            StoreId = order.StoreId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Status = order.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Items = order.Items.Select(item => new OrderItemEntity
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                StoreId = item.StoreId,
                ProductName = item.ProductName,
                VariantName = item.VariantName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineDiscount = item.LineDiscount,
                LineTotal = item.LineTotal
            }).ToList()
        });
        await db.SaveChangesAsync();

        await using var mutation = CreateDb();
        await using var tx = await mutation.Database.BeginTransactionAsync();
        var repository = new EfOrderForDeliveryRepository(mutation);
        var reloaded = await repository.GetAsync(order.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(order.Id, reloaded!.Id);
        Assert.Single(reloaded.Items);

        reloaded.SubmitForStoreConfirmation();
        await repository.SaveAsync(reloaded, CancellationToken.None);
        await tx.CommitAsync();

        await using var verify = CreateDb();
        var status = await verify.Orders
            .Where(x => x.Id == order.Id)
            .Select(x => x.Status)
            .SingleAsync();

        Assert.Equal(OrderStatus.PendingStoreConfirmation, status);
    }

    [Fact]
    public async Task Transactional_order_load_waits_on_existing_order_writer()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var storeId = Guid.NewGuid();
        var order = CreateOrder(storeId);
        dbSeed(setup, order, storeId);
        await setup.SaveChangesAsync();

        await using var first = CreateDb();
        await using var second = CreateDb();
        await using var firstTx = await first.Database.BeginTransactionAsync();
        var firstRepository = new EfOrderForDeliveryRepository(first);
        var locked = await firstRepository.GetAsync(order.Id, CancellationToken.None);
        Assert.NotNull(locked);

        await using var secondTx = await second.Database.BeginTransactionAsync();
        await second.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '250ms'");
        second.Database.SetCommandTimeout(2);

        var secondRepository = new EfOrderForDeliveryRepository(second);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            secondRepository.GetAsync(order.Id, CancellationToken.None));

        Assert.Contains("Timeout", exception.ToString(), StringComparison.OrdinalIgnoreCase);

        await secondTx.RollbackAsync();
        await firstTx.CommitAsync();
    }

    private static Order CreateOrder(Guid storeId)
    {
        return Order.Create(
            storeId,
            [new OrderItem(
                Guid.NewGuid(), storeId, "Whole Milk", "1L", 30m, 1, 0m, 30m)]);
    }

    private static void dbSeed(CommerceDbContext db, Order order, Guid storeId)
    {
        db.Stores.Add(new StoreEntity { Id = storeId, IsActive = true });
        db.Orders.Add(new OrderEntity
        {
            Id = order.Id,
            StoreId = storeId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Status = order.Status,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Items = order.Items.Select(item => new OrderItemEntity
            {
                Id = Guid.NewGuid(), OrderId = order.Id, ProductId = item.ProductId,
                StoreId = item.StoreId, ProductName = item.ProductName, VariantName = item.VariantName,
                UnitPrice = item.UnitPrice, Quantity = item.Quantity, LineDiscount = item.LineDiscount,
                LineTotal = item.LineTotal
            }).ToList()
        });
    }
}
