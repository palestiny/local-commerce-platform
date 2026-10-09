using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DriverEntity = LocalCommerce.Domain.Delivery.Driver;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
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
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
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

        var error = await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.DeliveryInvalidState, error.Code);
        Assert.Equal(DeliveryEntityStatus.Unassigned, fixture.Delivery.Status);
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

        var error = await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.AuthorizationForbidden, error.Code);
        Assert.Equal(DeliveryEntityStatus.Unassigned, fixture.Delivery.Status);
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

        var error = await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.DeliveryInvalidState, error.Code);
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
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
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

        var error = await Assert.ThrowsAsync<AssignDriverRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.IdempotencyKeyReused, error.Code);
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
        Assert.Equal(DeliveryEntityStatus.Unassigned, fixture.Delivery.Status);
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

    private sealed class FakeDeliveryRepository(DeliveryEntity delivery) : IAssignDriverDeliveryRepository
    {
        public Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryEntity?>(delivery.Id == deliveryId ? delivery : null);

        public Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryEntity?>(delivery.OrderId == orderId && delivery.Status != DeliveryEntityStatus.Delivered && delivery.Status != DeliveryEntityStatus.Failed ? delivery : null);

        public Task AddAsync(DeliveryEntity delivery, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeDriverRepository(DriverEntity driver) : IDriverRepository
    {
        public Task<DriverEntity?> GetAsync(Guid driverId, CancellationToken cancellationToken) =>
            Task.FromResult<DriverEntity?>(driver.Id == driverId ? driver : null);
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

    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotencyRecord> _records = new();

        private static string Key(Guid scopeId, string operation, string key) =>
            $"{scopeId:N}|{operation}|{key}";

        public Task<IdempotencyRecord?> GetAsync(
            Guid scopeId,
            string operation,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(_records.TryGetValue(Key(scopeId, operation, key), out var record) ? record : null);

        public Task<IdempotencyRecord?> ReserveAsync(
            Guid scopeId,
            string operation,
            string key,
            string fingerprint,
            CancellationToken cancellationToken)
        {
            var recordKey = Key(scopeId, operation, key);
            if (_records.TryGetValue(recordKey, out var existing))
                return Task.FromResult<IdempotencyRecord?>(existing);

            _records[recordKey] = new IdempotencyRecord(
                scopeId,
                operation,
                key,
                fingerprint,
                IdempotencyStatus.Reserved,
                null,
                null,
                null);

            return Task.FromResult<IdempotencyRecord?>(null);
        }

        public Task CompleteAsync(
            Guid scopeId,
            string operation,
            string key,
            IdempotencyCompletion completion,
            CancellationToken cancellationToken)
        {
            var recordKey = Key(scopeId, operation, key);
            var existing = _records[recordKey];
            _records[recordKey] = existing with
            {
                Status = IdempotencyStatus.Completed,
                ResourceType = completion.ResourceType,
                ResourceId = completion.ResourceId,
                ResultPayload = completion.ResultPayload
            };
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
