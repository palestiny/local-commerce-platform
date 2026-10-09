using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Domain;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Application.Ordering;
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
        Assert.Equal(1, results.Count(x => x.Failure is AssignDriverRejectedException));

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

    [Fact]
    public async Task Replacement_with_different_store_is_rejected_and_persists_no_active_delivery()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var orderId = Guid.NewGuid();
        var orderStoreId = Guid.NewGuid();
        var wrongStoreId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(setup, orderId, orderStoreId);
        setup.Stores.Add(new StoreEntity { Id = wrongStoreId, IsActive = true });
        await setup.SaveChangesAsync();

        var failed = LocalCommerce.Domain.Delivery.Delivery.Create(orderId, orderStoreId);
        failed.Fail("CUSTOMER_UNAVAILABLE", "Customer unavailable");
        await new EfDeliveryRepository(setup).AddAsync(failed, CancellationToken.None);

        await using var commandDb = CreateDb();
        var handler = new CreateReplacementDeliveryHandler(
            new EfReplacementDeliveryRepository(commandDb),
            new EfReplacementDeliveryOrderLock(commandDb),
            new EfReplacementDeliveryEligibility(commandDb),
            new EfGeneralizedIdempotencyStore(commandDb),
            new EfOrderDeliveryUnitOfWork(commandDb));

        await Assert.ThrowsAsync<CreateReplacementDeliveryRejectedException>(() =>
            handler.HandleAsync(new CreateReplacementDeliveryCommand(
                orderId, wrongStoreId, Guid.NewGuid(), $"replacement-mismatch-{Guid.NewGuid():N}")));

        await using var verify = CreateDb();
        var active = await new EfDeliveryRepository(verify)
            .GetActiveByOrderIdAsync(orderId, CancellationToken.None);
        Assert.Null(active);
    }

    [Fact]
    public async Task Concurrent_order_cancellation_and_replacement_never_leave_active_delivery_for_cancelled_order()
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
        var cancellation = RunCancellationAsync(orderId, ready, start.Task);
        var replacement = RunReplacementAsync(orderId, storeId, ready, start.Task);

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Both commands must reach the start gate.");
        start.SetResult(true);

        var results = await Task.WhenAll(cancellation, replacement);
        Assert.True(results[0].Success, $"Cancellation must succeed; failure: {results[0].Failure}");

        await using var verify = CreateDb();
        var persistedOrder = await verify.Orders.SingleAsync(x => x.Id == orderId);
        var active = await new EfDeliveryRepository(verify)
            .GetActiveByOrderIdAsync(orderId, CancellationToken.None);

        Assert.Equal(LocalCommerce.Domain.Ordering.OrderStatus.Cancelled, persistedOrder.Status);
        Assert.Null(active);
    }

    [Theory]
    [InlineData("ConfirmPickup")]
    [InlineData("StartDelivery")]
    [InlineData("CompleteDelivery")]
    [InlineData("FailDelivery")]
    public async Task Concurrent_delivery_state_transitions_allow_only_one_winner(string operation)
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        await SeedOrderAndStoreAsync(setup, orderId, storeId);

        var driverId = Guid.NewGuid();
        var delivery = LocalCommerce.Domain.Delivery.Delivery.Create(orderId, storeId);
        delivery.AssignDriver(driverId);
        if (operation is "StartDelivery" or "CompleteDelivery" or "FailDelivery")
            delivery.ConfirmPickup(driverId);
        if (operation is "CompleteDelivery" or "FailDelivery")
            delivery.StartDelivery();
        await new EfDeliveryRepository(setup).AddAsync(delivery, CancellationToken.None);

        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var ready = new CountdownEvent(2);
        var first = RunTransitionAsync(operation, delivery.Id, driverId, ready, start.Task);
        var second = RunTransitionAsync(operation, delivery.Id, driverId, ready, start.Task);

        Assert.True(ready.Wait(TimeSpan.FromSeconds(5)), "Both transition commands must reach the start gate.");
        start.SetResult(true);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => !x.Success));

        var failure = results.Single(x => !x.Success).Failure;
        switch (operation)
        {
            case "ConfirmPickup":
                Assert.IsType<ConfirmPickupRejectedException>(failure);
                break;
            case "StartDelivery":
                Assert.IsType<StartDeliveryRejectedException>(failure);
                break;
            case "CompleteDelivery":
                Assert.IsType<CompleteDeliveryRejectedException>(failure);
                break;
            case "FailDelivery":
                Assert.IsType<FailDeliveryRejectedException>(failure);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }

        await using var verify = CreateDb();
        var persisted = await new EfDeliveryRepository(verify).GetAsync(delivery.Id, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(operation switch
        {
            "ConfirmPickup" => DeliveryStatus.PickedUp,
            "StartDelivery" => DeliveryStatus.OutForDelivery,
            "CompleteDelivery" => DeliveryStatus.Delivered,
            "FailDelivery" => DeliveryStatus.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        }, persisted!.Status);
    }

    private static async Task<(bool Success, Exception? Failure)> RunTransitionAsync(
        string operation,
        Guid deliveryId,
        Guid driverId,
        CountdownEvent ready,
        Task startTask)
    {
        ready.Signal();
        await startTask;
        await using var db = CreateDb();
        var repository = new EfTransitionDeliveryRepository(db);
        var idempotency = new EfGeneralizedIdempotencyStore(db);
        var unitOfWork = new EfTransitionUnitOfWork(db);
        var authorization = new AllowTransitionAuthorization();

        try
        {
            switch (operation)
            {
                case "ConfirmPickup":
                    await new ConfirmPickupHandler(repository, authorization, idempotency, unitOfWork)
                        .HandleAsync(new ConfirmPickupCommand(deliveryId, driverId, $"pickup-{Guid.NewGuid():N}"));
                    break;
                case "StartDelivery":
                    await new StartDeliveryHandler(repository, authorization, idempotency, unitOfWork)
                        .HandleAsync(new StartDeliveryCommand(deliveryId, driverId, $"start-{Guid.NewGuid():N}"));
                    break;
                case "CompleteDelivery":
                    await new CompleteDeliveryHandler(repository, authorization, idempotency, unitOfWork)
                        .HandleAsync(new CompleteDeliveryCommand(deliveryId, driverId, $"complete-{Guid.NewGuid():N}"));
                    break;
                case "FailDelivery":
                    await new FailDeliveryHandler(repository, authorization, idempotency, unitOfWork)
                        .HandleAsync(new FailDeliveryCommand(deliveryId, driverId, "RACE_TEST", "Concurrent failure transition", $"fail-{Guid.NewGuid():N}"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }

            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }

    private sealed class EfTransitionDeliveryRepository(CommerceDbContext db) :
        IConfirmPickupDeliveryRepository,
        IStartDeliveryDeliveryRepository,
        ICompleteDeliveryDeliveryRepository,
        IFailDeliveryDeliveryRepository
    {
        private readonly EfDeliveryRepository _inner = new(db);
        public Task<LocalCommerce.Domain.Delivery.Delivery?> GetAsync(Guid id, CancellationToken ct) => _inner.GetAsync(id, ct);
        public Task SaveAsync(LocalCommerce.Domain.Delivery.Delivery delivery, CancellationToken ct) => _inner.SaveAsync(delivery, ct);
    }

    private sealed class AllowTransitionAuthorization :
        IConfirmPickupAuthorization,
        IStartDeliveryAuthorization,
        ICompleteDeliveryAuthorization,
        IFailDeliveryAuthorization
    {
        public Task<bool> CanConfirmPickupAsync(Guid actorId, LocalCommerce.Domain.Delivery.Delivery delivery, CancellationToken ct) =>
            Task.FromResult(delivery.DriverId == actorId);
        public Task<bool> CanStartDeliveryAsync(Guid actorId, LocalCommerce.Domain.Delivery.Delivery delivery, CancellationToken ct) =>
            Task.FromResult(delivery.DriverId == actorId);
        public Task<bool> CanCompleteDeliveryAsync(Guid actorId, LocalCommerce.Domain.Delivery.Delivery delivery, CancellationToken ct) =>
            Task.FromResult(delivery.DriverId == actorId);
        public Task<bool> CanFailDeliveryAsync(Guid actorId, LocalCommerce.Domain.Delivery.Delivery delivery, CancellationToken ct) =>
            Task.FromResult(delivery.DriverId == actorId);
    }

    private sealed class EfTransitionUnitOfWork(CommerceDbContext db) :
        IConfirmPickupUnitOfWork,
        IStartDeliveryUnitOfWork,
        ICompleteDeliveryUnitOfWork,
        IFailDeliveryUnitOfWork
    {
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await operation(ct);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }
    }

    private static async Task<(bool Success, Exception? Failure)> RunCancellationAsync(
        Guid orderId,
        CountdownEvent ready,
        Task startTask)
    {
        ready.Signal();
        await startTask;
        await using var db = CreateDb();

        var handler = new CancelOrderHandler(
            new EfOrderForDeliveryRepository(db),
            new EfDeliveryRepository(db),
            new AllowCancelAuthorization(),
            new EfGeneralizedIdempotencyStore(db),
            new EfOrderDeliveryUnitOfWork(db));

        try
        {
            await handler.HandleAsync(new CancelOrderCommand(orderId, Guid.NewGuid(), $"cancel-{Guid.NewGuid():N}"));
            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
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
            new EfOrderDeliveryUnitOfWork(db));

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
            UpdatedAt = DateTimeOffset.UtcNow,
            Items =
            [
                new OrderItemEntity
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = Guid.NewGuid(),
                    StoreId = storeId,
                    ProductName = "Concurrency test item",
                    VariantName = null,
                    UnitPrice = 10m,
                    Quantity = 1,
                    LineDiscount = 0m,
                    LineTotal = 10m
                }
            ]
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

    private sealed class AllowCancelAuthorization : ICancelOrderAuthorization
    {
        public Task<bool> CanCancelAsync(
            Guid actorId,
            LocalCommerce.Domain.Ordering.Order order,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
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
}
