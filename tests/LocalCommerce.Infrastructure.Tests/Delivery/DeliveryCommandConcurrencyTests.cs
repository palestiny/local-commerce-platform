using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Domain;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalCommerce.Infrastructure.Tests.Delivery;

public sealed class DeliveryCommandConcurrencyTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    [Fact]
    public async Task Concurrent_assign_driver_commands_have_one_authoritative_result()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(setup, orderId, storeId);

        var delivery = LocalCommerce.Domain.Delivery.Delivery.Create(orderId, storeId);
        await new EfDeliveryRepository(setup).AddAsync(delivery, CancellationToken.None);

        var driverA = Driver.Create(Guid.NewGuid());
        var driverB = Driver.Create(Guid.NewGuid());
        var actorA = Guid.NewGuid();
        var actorB = Guid.NewGuid();

        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var ready = new CountdownEvent(2);
        var first = RunAssignAsync(delivery.Id, actorA, driverA, ready, start.Task);
        var second = RunAssignAsync(delivery.Id, actorB, driverB, ready, start.Task);

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Both assignment commands must reach the start gate.");
        start.SetResult(true);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => x.Failure is DomainRuleViolationException));

        await using var verify = CreateDb();
        var persisted = await new EfDeliveryRepository(verify)
            .GetAsync(delivery.Id, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(DeliveryStatus.Assigned, persisted!.Status);
        Assert.True(persisted.DriverId == driverA.Id || persisted.DriverId == driverB.Id);
    }

    [Fact]
    public async Task Concurrent_replacement_creation_converges_to_one_active_delivery()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(setup, orderId, storeId);

        var failed = LocalCommerce.Domain.Delivery.Delivery.Create(orderId, storeId);
        failed.Fail("CUSTOMER_UNAVAILABLE", "Customer unavailable");
        await new EfDeliveryRepository(setup).AddAsync(failed, CancellationToken.None);

        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var ready = new CountdownEvent(2);
        var first = RunReplacementAsync(orderId, storeId, ready, start.Task);
        var second = RunReplacementAsync(orderId, storeId, ready, start.Task);

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Both replacement commands must reach the start gate.");
        start.SetResult(true);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => x.Failure is CreateReplacementDeliveryRejectedException or DbUpdateException));

        await using var verify = CreateDb();
        var active = await new EfDeliveryRepository(verify)
            .GetActiveByOrderIdAsync(orderId, CancellationToken.None);

        Assert.NotNull(active);
        Assert.Equal(DeliveryStatus.Unassigned, active!.Status);
    }

    private static async Task<(bool Success, Exception? Failure)> RunAssignAsync(
        Guid deliveryId,
        Guid actorId,
        Driver driver,
        CountdownEvent ready,
        Task startTask)
    {
        ready.Signal();
        await startTask;
        await using var db = CreateDb();

        var handler = new AssignDriverHandler(
            new EfAssignDriverDeliveryRepository(db),
            new EfTestDriverRepository(driver),
            new AllowAssignAuthorization(),
            new EfGeneralizedIdempotencyStore(db),
            new EfAssignDriverUnitOfWork(db));

        try
        {
            await handler.HandleAsync(
                new AssignDriverCommand(
                    deliveryId,
                    driver.Id,
                    actorId,
                    $"assign-{Guid.NewGuid():N}"));

            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }

    private static async Task<(bool Success, Exception? Failure)> RunReplacementAsync(
        Guid orderId,
        Guid storeId,
        CountdownEvent ready,
        Task startTask)
    {
        ready.Signal();
        await startTask;
        await using var db = CreateDb();

        var handler = new CreateReplacementDeliveryHandler(
            new EfReplacementDeliveryRepository(db),
            new EfReplacementDeliveryOrderLock(db),
            new EfReplacementDeliveryEligibility(db),
            new EfGeneralizedIdempotencyStore(db),
            new EfReplacementUnitOfWork(db));

        try
        {
            await handler.HandleAsync(
                new CreateReplacementDeliveryCommand(
                    orderId,
                    storeId,
                    Guid.NewGuid(),
                    $"replacement-{Guid.NewGuid():N}"));

            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
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
            Status = LocalCommerce.Domain.Ordering.OrderStatus.ReadyForPickup,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }

    private sealed class EfAssignDriverDeliveryRepository(CommerceDbContext db)
        : IAssignDriverDeliveryRepository
    {
        private readonly EfDeliveryRepository _inner = new(db);

        public Task<LocalCommerce.Domain.Delivery.Delivery?> GetAsync(
            Guid deliveryId,
            CancellationToken cancellationToken) =>
            _inner.GetAsync(deliveryId, cancellationToken);

        public Task SaveAsync(
            LocalCommerce.Domain.Delivery.Delivery delivery,
            CancellationToken cancellationToken) =>
            _inner.SaveAsync(delivery, cancellationToken);
    }

    private sealed class EfTestDriverRepository(Driver driver) : IDriverRepository
    {
        public Task<Driver?> GetAsync(Guid driverId, CancellationToken cancellationToken) =>
            Task.FromResult<Driver?>(driver.Id == driverId ? driver : null);
    }

    private sealed class AllowAssignAuthorization : IAssignDriverAuthorization
    {
        public Task<bool> CanAssignAsync(
            Guid actorId,
            LocalCommerce.Domain.Delivery.Delivery delivery,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class EfAssignDriverUnitOfWork(CommerceDbContext db)
        : IAssignDriverUnitOfWork
    {
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private sealed class EfReplacementDeliveryRepository(CommerceDbContext db)
        : ICreateReplacementDeliveryRepository
    {
        private readonly EfDeliveryRepository _inner = new(db);

        public Task<LocalCommerce.Domain.Delivery.Delivery?> GetActiveByOrderIdAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            _inner.GetActiveByOrderIdAsync(orderId, cancellationToken);

        public Task AddAsync(
            LocalCommerce.Domain.Delivery.Delivery delivery,
            CancellationToken cancellationToken) =>
            _inner.AddAsync(delivery, cancellationToken);
    }

    private sealed class EligibleReplacementOrder : IReplacementDeliveryEligibility
    {
        public Task<bool> IsOrderEligibleAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class EfReplacementUnitOfWork(CommerceDbContext db)
        : ICreateReplacementDeliveryUnitOfWork
    {
        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
