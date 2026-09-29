using LocalCommerce.Application.Delivery;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DriverEntity = LocalCommerce.Domain.Delivery.Driver;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class AssignDriverHandlerTests
{
    [Fact]
    public async Task Active_driver_is_assigned_to_unassigned_delivery()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Handler.HandleAsync(
            new AssignDriverCommand(
                fixture.Delivery.Id,
                fixture.Driver.Id,
                fixture.ActorId,
                "assign-1"));

        Assert.Equal(fixture.Delivery.Id, result.DeliveryId);
        Assert.Equal(fixture.Driver.Id, fixture.Delivery.DriverId);
        Assert.Equal(DeliveryStatus.Assigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Inactive_driver_is_rejected_without_mutating_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Driver.Deactivate();

        var act = () => fixture.Handler.HandleAsync(
            new AssignDriverCommand(
                fixture.Delivery.Id,
                fixture.Driver.Id,
                fixture.ActorId,
                "assign-1"));

        await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(DeliveryStatus.Unassigned, fixture.Delivery.Status);
        Assert.Null(fixture.Delivery.DriverId);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_assign_driver()
    {
        var fixture = Fixture.Create();
        fixture.Authorization.Allowed = false;

        var act = () => fixture.Handler.HandleAsync(
            new AssignDriverCommand(
                fixture.Delivery.Id,
                fixture.Driver.Id,
                fixture.ActorId,
                "assign-1"));

        await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(DeliveryStatus.Unassigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Already_assigned_delivery_is_rejected()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.AssignDriver(Guid.NewGuid());

        var act = () => fixture.Handler.HandleAsync(
            new AssignDriverCommand(
                fixture.Delivery.Id,
                fixture.Driver.Id,
                fixture.ActorId,
                "assign-1"));

        await Assert.ThrowsAsync<DomainRuleViolationException>(act);
    }

    [Fact]
    public async Task Same_idempotency_key_and_same_fingerprint_returns_original_result()
    {
        var fixture = Fixture.Create();
        var command = new AssignDriverCommand(
            fixture.Delivery.Id,
            fixture.Driver.Id,
            fixture.ActorId,
            "assign-1");

        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);

        Assert.Equal(first, second);
        Assert.Equal(DeliveryStatus.Assigned, fixture.Delivery.Status);
        Assert.Equal(fixture.Driver.Id, fixture.Delivery.DriverId);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_driver_is_rejected()
    {
        var fixture = Fixture.Create();
        var command = new AssignDriverCommand(
            fixture.Delivery.Id,
            fixture.Driver.Id,
            fixture.ActorId,
            "assign-1");

        await fixture.Handler.HandleAsync(command);

        var otherDriver = DriverEntity.Create(Guid.NewGuid());

        var act = () => fixture.Handler.HandleAsync(
            command with { DriverId = otherDriver.Id });

        await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
    }

    [Fact]
    public async Task Unit_of_work_failure_leaves_delivery_unassigned()
    {
        var fixture = Fixture.Create();
        fixture.UnitOfWork.FailBeforeOperation = true;

        var act = () => fixture.Handler.HandleAsync(
            new AssignDriverCommand(
                fixture.Delivery.Id,
                fixture.Driver.Id,
                fixture.ActorId,
                "assign-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(DeliveryStatus.Unassigned, fixture.Delivery.Status);
        Assert.Null(fixture.Delivery.DriverId);
    }

    private sealed class Fixture
    {
        private Fixture(DeliveryEntity delivery, DriverEntity driver)
        {
            Delivery = delivery;
            Driver = driver;
            ActorId = Guid.NewGuid();
            DeliveryRepository = new FakeDeliveryRepository(delivery);
            DriverRepository = new FakeDriverRepository(driver);
            Authorization = new FakeAuthorization();
            IdempotencyStore = new FakeIdempotencyStore();
            UnitOfWork = new FakeUnitOfWork();

            Handler = new AssignDriverHandler(
                DeliveryRepository,
                DriverRepository,
                Authorization,
                IdempotencyStore,
                UnitOfWork);
        }

        public DeliveryEntity Delivery { get; }
        public DriverEntity Driver { get; }
        public Guid ActorId { get; }
        public FakeDeliveryRepository DeliveryRepository { get; }
        public FakeDriverRepository DriverRepository { get; }
        public FakeAuthorization Authorization { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public AssignDriverHandler Handler { get; }

        public static Fixture Create()
        {
            var delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
            var driver = DriverEntity.Create(Guid.NewGuid());
            return new Fixture(delivery, driver);
        }
    }

    private sealed class FakeDeliveryRepository(DeliveryEntity delivery) : IDeliveryRepository
    {
        public Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryEntity?>(delivery.Id == deliveryId ? delivery : null);

        public Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeDriverRepository(DriverEntity driver) : IDriverRepository
    {
        public Task<DriverEntity?> GetAsync(Guid driverId, CancellationToken cancellationToken) =>
            Task.FromResult<Driver?>(driver.Id == driverId ? driver : null);
    }

    private sealed class FakeAuthorization : IAssignDriverAuthorization
    {
        public bool Allowed { get; set; } = true;

        public Task<bool> CanAssignAsync(
            Guid actorId,
            DeliveryEntity delivery,
            CancellationToken cancellationToken) =>
            Task.FromResult(Allowed);
    }

    private sealed class FakeIdempotencyStore : IAssignDriverIdempotencyStore
    {
        private readonly Dictionary<string, DriverAssignmentIdempotencyRecord> records = [];

        public Task<DriverAssignmentIdempotencyRecord?> GetAsync(
            Guid actorId,
            string operation,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                records.TryGetValue(key, out var record)
                    ? record
                    : null);

        public Task<DriverAssignmentIdempotencyRecord?> ReserveAsync(
            Guid actorId,
            string operation,
            string key,
            string fingerprint,
            CancellationToken cancellationToken)
        {
            if (records.TryGetValue(key, out var existing))
                return Task.FromResult<DriverAssignmentIdempotencyRecord?>(existing);

            records[key] = new DriverAssignmentIdempotencyRecord(
                key,
                fingerprint,
                new AssignDriverResult(Guid.Empty, Guid.Empty));

            return Task.FromResult<DriverAssignmentIdempotencyRecord?>(null);
        }

        public Task CompleteAsync(
            Guid actorId,
            string operation,
            string key,
            AssignDriverResult result,
            CancellationToken cancellationToken)
        {
            var existing = records[key];
            records[key] = existing with { Result = result };
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IAssignDriverUnitOfWork
    {
        public bool FailBeforeOperation { get; set; }

        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            if (FailBeforeOperation)
                throw new InvalidOperationException("Persistence failure.");

            await operation(cancellationToken);
        }
    }
}
