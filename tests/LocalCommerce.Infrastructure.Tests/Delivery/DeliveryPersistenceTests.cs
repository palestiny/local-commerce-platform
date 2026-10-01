using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;
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
        await SeedOrderAndStoreAsync(db, orderId, storeId);

        var driverId = Guid.NewGuid();

        var delivery = DeliveryEntity.Create(orderId, storeId);
        delivery.AssignDriver(driverId);

        await new EfDeliveryRepository(db).AddAsync(delivery, CancellationToken.None);

        await using var verify = CreateDb();
        var reloaded = await new EfDeliveryRepository(verify)
            .GetAsync(delivery.Id, CancellationToken.None);

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

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(db, orderId, storeId);

        var delivery = DeliveryEntity.Create(orderId, storeId);
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
    public async Task Transactional_delivery_load_waits_for_previous_writer_and_reads_committed_state()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(setup, orderId, storeId);

        var delivery = DeliveryEntity.Create(orderId, storeId);
        await new EfDeliveryRepository(setup).AddAsync(delivery, CancellationToken.None);

        await using var first = CreateDb();
        await using var second = CreateDb();
        await using var firstTx = await first.Database.BeginTransactionAsync();

        var firstRepository = new EfDeliveryRepository(first);
        var locked = await firstRepository.GetAsync(delivery.Id, CancellationToken.None);

        Assert.NotNull(locked);
        locked!.AssignDriver(Guid.NewGuid());
        await firstRepository.SaveAsync(locked, CancellationToken.None);

        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRead = Task.Run(async () =>
        {
            await using var tx = await second.Database.BeginTransactionAsync();
            secondStarted.SetResult();
            var repository = new EfDeliveryRepository(second);
            var observed = await repository.GetAsync(delivery.Id, CancellationToken.None);
            await tx.CommitAsync();
            return observed;
        });

        await secondStarted.Task;
        await Task.Delay(50);

        Assert.False(secondRead.IsCompleted);

        await firstTx.CommitAsync();

        var reloaded = await secondRead;
        Assert.NotNull(reloaded);
        Assert.Equal(DeliveryStatus.Assigned, reloaded!.Status);
    }

    [Fact]
    public async Task Only_one_active_delivery_can_exist_for_an_order()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(db, orderId, storeId);

        var first = DeliveryEntity.Create(orderId, storeId);
        var second = DeliveryEntity.Create(orderId, storeId);

        await new EfDeliveryRepository(db).AddAsync(first, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new EfDeliveryRepository(db).AddAsync(second, CancellationToken.None));
    }

    private static async Task SeedOrderAndStoreAsync(
        CommerceDbContext db,
        Guid orderId,
        Guid storeId)
    {
        db.Stores.Add(new StoreEntity
        {
            Id = storeId,
            IsActive = true
        });

        db.Orders.Add(new OrderEntity
        {
            Id = orderId,
            StoreId = storeId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}",
            Status = OrderStatus.ReadyForPickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
