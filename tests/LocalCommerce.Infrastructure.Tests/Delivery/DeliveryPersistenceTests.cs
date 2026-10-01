using LocalCommerce.Domain.Delivery;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalCommerce.Infrastructure.Tests.Delivery;

public sealed class DeliveryPersistenceTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    [Fact]
    public async Task Delivery_current_state_can_be_persisted_and_reloaded()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var driverId = Guid.NewGuid();

        var delivery = Delivery.Create(orderId, storeId);
        delivery.AssignDriver(driverId);

        await new EfDeliveryRepository(db).AddAsync(delivery, CancellationToken.None);

        await using var verify = CreateDb();
        var reloaded = await new EfDeliveryRepository(verify)
            .GetAsync(deliveryId, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Equal(storeId, reloaded.StoreId);
        Assert.Equal(driverId, reloaded.DriverId);
        Assert.Equal(DeliveryStatus.Assigned, reloaded.Status);
    }

    [Fact]
    public async Task Delivery_status_history_is_append_only_and_preserves_transition_metadata()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var delivery = Delivery.Create(Guid.NewGuid(), Guid.NewGuid());
        await new EfDeliveryRepository(db).AddAsync(delivery, CancellationToken.None);

        var history = new DeliveryStatusHistoryEntry(
            delivery.Id,
            "Driver",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DeliveryStatus.Unassigned,
            DeliveryStatus.Assigned,
            "AssignDriver",
            "correlation-1",
            null,
            null);

        await new EfDeliveryStatusHistoryRepository(db)
            .AppendAsync(history, CancellationToken.None);

        await using var verify = CreateDb();
        var entries = await new EfDeliveryStatusHistoryRepository(verify)
            .GetByDeliveryIdAsync(delivery.Id, CancellationToken.None);

        var entry = Assert.Single(entries);
        Assert.Equal(DeliveryStatus.Unassigned, entry.PreviousStatus);
        Assert.Equal(DeliveryStatus.Assigned, entry.NewStatus);
        Assert.Equal("AssignDriver", entry.CommandName);
        Assert.Equal("correlation-1", entry.CorrelationId);
    }

    [Fact]
    public async Task Only_one_active_delivery_can_exist_for_an_order()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        var first = Delivery.Create(orderId, storeId);
        var second = Delivery.Create(orderId, storeId);

        await new EfDeliveryRepository(db).AddAsync(first, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new EfDeliveryRepository(db).AddAsync(second, CancellationToken.None));
    }
}
