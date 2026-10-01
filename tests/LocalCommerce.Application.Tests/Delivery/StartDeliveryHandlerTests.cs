using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class StartDeliveryHandlerTests
{
    [Fact]
    public async Task Assigned_driver_can_start_delivery_after_pickup()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);

        var result = await fixture.Handler.HandleAsync(
            new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1"));

        Assert.Equal(fixture.Delivery.Id, result.DeliveryId);
        Assert.Equal(fixture.DriverId, result.DriverId);
        Assert.Equal(DeliveryEntityStatus.OutForDelivery, fixture.Delivery.Status);
        Assert.NotNull(fixture.Delivery.OutForDeliveryAt);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_start_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);
        fixture.Authorization.Allowed = false;

        var act = () => fixture.Handler.HandleAsync(
            new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1"));

        await Assert.ThrowsAsync<StartDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.PickedUp, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Non_assigned_driver_cannot_start_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);
        var otherDriverId = Guid.NewGuid();

        var act = () => fixture.Handler.HandleAsync(
            new StartDeliveryCommand(fixture.Delivery.Id, otherDriverId, "start-1"));

        await Assert.ThrowsAsync<StartDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.PickedUp, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Start_delivery_requires_picked_up_delivery()
    {
        var fixture = Fixture.Create();

        var act = () => fixture.Handler.HandleAsync(
            new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1"));

        await Assert.ThrowsAsync<StartDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_and_same_fingerprint_returns_original_result()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);
        var command = new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1");

        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);

        Assert.Equal(first, second);
        Assert.Equal(DeliveryEntityStatus.OutForDelivery, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_request_is_rejected()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);
        var command = new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1");

        await fixture.Handler.HandleAsync(command);

        var act = () => fixture.Handler.HandleAsync(
            command with { DeliveryId = Guid.NewGuid() });

        await Assert.ThrowsAsync<StartDeliveryRejectedException>(act);
    }

    [Fact]
    public async Task Unit_of_work_failure_leaves_delivery_picked_up()
    {
        var fixture = Fixture.Create();
        fixture.Delivery.ConfirmPickup(fixture.DriverId);
        fixture.UnitOfWork.FailBeforeOperation = true;

        var act = () => fixture.Handler.HandleAsync(
            new StartDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "start-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(DeliveryEntityStatus.PickedUp, fixture.Delivery.Status);
    }

    private sealed class Fixture
    {
        private Fixture(DeliveryEntity delivery, Guid driverId)
        {
            Delivery = delivery;
            DriverId = driverId;
            Repository = new FakeDeliveryRepository(delivery);
            Authorization = new FakeAuthorization();
            IdempotencyStore = new FakeIdempotencyStore();
            UnitOfWork = new FakeUnitOfWork();
            Handler = new StartDeliveryHandler(Repository, Authorization, IdempotencyStore, UnitOfWork);
        }

        public DeliveryEntity Delivery { get; }
        public Guid DriverId { get; }
        public FakeDeliveryRepository Repository { get; }
        public FakeAuthorization Authorization { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public StartDeliveryHandler Handler { get; }

        public static Fixture Create()
        {
            var driverId = Guid.NewGuid();
            var delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
            delivery.AssignDriver(driverId);
            return new Fixture(delivery, driverId);
        }
    }

    private sealed class FakeDeliveryRepository : IStartDeliveryDeliveryRepository
    {
        private readonly DeliveryEntity _delivery;
        public FakeDeliveryRepository(DeliveryEntity delivery) => _delivery = delivery;

        public Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryEntity?>(_delivery.Id == deliveryId ? _delivery : null);

        public Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeAuthorization : IStartDeliveryAuthorization
    {
        public bool Allowed { get; set; } = true;

        public Task<bool> CanStartDeliveryAsync(
            Guid actorId, DeliveryEntity delivery, CancellationToken cancellationToken) =>
            Task.FromResult(Allowed && delivery.DriverId == actorId);
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

    private sealed class FakeUnitOfWork : IStartDeliveryUnitOfWork
    {
        public bool FailBeforeOperation { get; set; }

        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            if (FailBeforeOperation)
                throw new InvalidOperationException("Simulated transaction failure.");

            return operation(cancellationToken);
        }
    }
}
