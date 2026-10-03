using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Ordering;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalCommerce.Infrastructure.Tests.Ordering;

public sealed class OrderDeliveryMutationIntegrationTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>().UseNpgsql(ConnectionString).Options);

    [Fact]
    public async Task Ready_order_for_delivery_persists_order_and_delivery_in_one_transaction()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var storeId = Guid.NewGuid();
        var order = CreateOrder(storeId);
        order.SubmitForStoreConfirmation();
        order.Accept();
        order.Prepare();
        await SeedOrderAsync(db, order, storeId);

        var actorId = Guid.NewGuid();
        var key = $"ready-{Guid.NewGuid():N}";
        var handler = new ReadyOrderForDeliveryHandler(
            new EfOrderForDeliveryRepository(db),
            new EfDeliveryRepository(db),
            new AllowReadyAuthorization(),
            new EfGeneralizedIdempotencyStore(db),
            new EfOrderDeliveryUnitOfWork(db));

        var result = await handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(order.Id, actorId, key));

        var persistedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        var persistedDelivery = await db.Deliveries.SingleAsync(x => x.Id == result.DeliveryId);
        var idempotency = await db.IdempotencyRecords.SingleAsync(
            x => x.ScopeId == actorId && x.Operation == "ReadyOrderForDelivery" && x.IdempotencyKey == key);

        Assert.Equal(OrderStatus.ReadyForPickup, persistedOrder.Status);
        Assert.Equal(DeliveryStatus.Unassigned, persistedDelivery.Status);
        Assert.Equal(IdempotencyStatus.Completed, idempotency.Status);
        Assert.Equal(result.DeliveryId, idempotency.ResourceId);
    }

    [Fact]
    public async Task Cancel_order_persists_order_and_active_delivery_cancellation_atomically()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var storeId = Guid.NewGuid();
        var order = CreateOrder(storeId);
        order.SubmitForStoreConfirmation();
        order.Accept();
        order.Prepare();
        order.MarkReadyForPickup();
        await SeedOrderAsync(db, order, storeId);

        var delivery = Delivery.Create(order.Id, storeId);
        await new EfDeliveryRepository(db).AddAsync(delivery, CancellationToken.None);

        var actorId = Guid.NewGuid();
        var key = $"cancel-{Guid.NewGuid():N}";
        var handler = new CancelOrderHandler(
            new EfOrderForDeliveryRepository(db),
            new EfDeliveryRepository(db),
            new AllowCancelAuthorization(),
            new EfGeneralizedIdempotencyStore(db),
            new EfOrderDeliveryUnitOfWork(db));

        var result = await handler.HandleAsync(
            new CancelOrderCommand(order.Id, actorId, key));

        var persistedOrder = await db.Orders.SingleAsync(x => x.Id == order.Id);
        var persistedDelivery = await db.Deliveries.SingleAsync(x => x.Id == delivery.Id);
        var idempotency = await db.IdempotencyRecords.SingleAsync(
            x => x.ScopeId == actorId && x.Operation == "CancelOrder" && x.IdempotencyKey == key);

        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(delivery.Id, result.DeliveryId);
        Assert.Equal(OrderStatus.Cancelled, persistedOrder.Status);
        Assert.Equal(DeliveryStatus.Cancelled, persistedDelivery.Status);
        Assert.Equal(IdempotencyStatus.Completed, idempotency.Status);
    }

    private static async Task SeedOrderAsync(CommerceDbContext db, Order order, Guid storeId)
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
        await db.SaveChangesAsync();
    }

    private static Order CreateOrder(Guid storeId) =>
        Order.Create(storeId, [new OrderItem(
            Guid.NewGuid(), storeId, "Whole Milk", "1L", 30m, 1, 0m, 30m)]);

    private sealed class AllowReadyAuthorization : IReadyForDeliveryAuthorization
    {
        public Task<bool> CanMarkReadyAsync(Guid actorId, Order order, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class AllowCancelAuthorization : ICancelOrderAuthorization
    {
        public Task<bool> CanCancelAsync(Guid actorId, Order order, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
