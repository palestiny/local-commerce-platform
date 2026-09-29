using LocalCommerce.Application.Delivery;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class ConfirmPickupHandlerTests
{
    [Fact]
    public async Task Assigned_driver_can_confirm_pickup()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Handler.HandleAsync(
            new ConfirmPickupCommand(
                fixture.Delivery.Id,
                fixture.DriverId,
                "pickup-1"));

        Assert.Equal(fixture.Delivery.Id, result.DeliveryId);
        Assert.Equal(fixture.DriverId, result.DriverId);
        Assert.Equal(DeliveryEntityStatus.PickedUp, fixture.Delivery.Status);
        Assert.NotNull(fixture.Delivery.PickedUpAt);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_confirm_pickup()
    {
        var fixture = Fixture.Create();
        fixture.Authorization.Allowed = false;

        var act = () => fixture.Handler.HandleAsync(
            new ConfirmPickupCommand(
                fixture.Delivery.Id,
                fixture.DriverId,
                "pickup-1"));

        await Assert.ThrowsAsync<ConfirmPickupRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Non_assigned_driver_cannot_confirm_pickup()
    {
        var fixture = Fixture.Create();
        var otherDriverId = Guid.NewGuid();

        fixture.Authorization.Allowed = true;

        var act = () => fixture.Handler.HandleAsync(
            new ConfirmPickupCommand(
                fixture.Delivery.Id,
                otherDriverId,
                "pickup-1"));

        await Assert.ThrowsAsync<ConfirmPickupRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Pickup_requires_assigned_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
        fixture.Repository.SetDelivery(fixture.Delivery);

        var act = () => fixture.Handler.HandleAsync(
            new ConfirmPickupCommand(
                fixture.Delivery.Id,
                fixture.DriverId,
                "pickup-1"));

        await Assert.ThrowsAsync<ConfirmPickupRejectedException>(act);
    }

    [Fact]
    public async Task Same_idempotency_key_and_same_fingerprint_returns_original_result()
    {
        var fixture = Fixture.Create();
        var command = new ConfirmPickupCommand(
            fixture.Delivery.Id,
            fixture.DriverId,
            "pickup-1");

        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);

        Assert.Equal(first, second);
        Assert.Equal(DeliveryEntityStatus.PickedUp, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_request_is_rejected()
    {
        var fixture = Fixture.Create();
        var command = new ConfirmPickupCommand(
            fixture.Delivery.Id,
            fixture.DriverId,
            "pickup-1");

        await fixture.Handler.HandleAsync(command);

        var act = () => fixture.Handler.HandleAsync(
            command with { DeliveryId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ConfirmPickupRejectedException>(act);
    }

    [Fact]
    public async Task Unit_of_work_failure_leaves_delivery_assigned()
    {
        var fixture = Fixture.Create();
        fixture.UnitOfWork.FailBeforeOperation = true;

        var act = () => fixture.Handler.HandleAsync(
            new ConfirmPickupCommand(
                fixture.Delivery.Id,
                fixture.DriverId,
                "pickup-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
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

            Handler = new ConfirmPickupHandler(
                Repository,
                Authorization,
                IdempotencyStore,
                UnitOfWork);
        }

        public DeliveryEntity Delivery { get; set; }
        public Guid DriverId { get; }
        public FakeDeliveryRepository Repository { get; }
        public FakeAuthorization Authorization { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public ConfirmPickupHandler Handler { get; }

        public static Fixture Create()
        {
            var driverId = Guid.NewGuid();
            var delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
            delivery.AssignDriver(driverId);
            return new Fixture(delivery, driverId);
        }
    }

    private sealed class FakeDeliveryRepository : IConfirmPickupDeliveryRepository
    {
        private DeliveryEntity _delivery;

        public FakeDeliveryRepository(DeliveryEntity delivery) => _delivery = delivery;

        public Task<DeliveryEntity?> GetAsync(
            Guid deliveryId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DeliveryEntity?>(
                _delivery.Id == deliveryId ? _delivery : null);

        public void SetDelivery(DeliveryEntity delivery) => _delivery = delivery;

        public Task SaveAsync(
            DeliveryEntity delivery,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeAuthorization : IConfirmPickupAuthorization
    {
        public bool Allowed { get; set; } = true;

        public Task<bool> CanConfirmPickupAsync(
            Guid actorId,
            DeliveryEntity delivery,
            CancellationToken cancellationToken) =>
            Task.FromResult(Allowed && delivery.DriverId == actorId);
    }

    private sealed class FakeIdempotencyStore : IConfirmPickupIdempotencyStore
    {
        private readonly Dictionary<string, ConfirmPickupIdempotencyRecord> _records = new();

        public Task<ConfirmPickupIdempotencyRecord?> GetAsync(
            Guid actorId,
            string operation,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                _records.TryGetValue(BuildKey(actorId, operation, key), out var record)
                    ? record
                    : null);

        public Task<ConfirmPickupIdempotencyRecord?> ReserveAsync(
            Guid actorId,
            string operation,
            string key,
            string fingerprint,
            CancellationToken cancellationToken)
        {
            var recordKey = BuildKey(actorId, operation, key);

            if (_records.TryGetValue(recordKey, out var existing))
                return Task.FromResult<ConfirmPickupIdempotencyRecord?>(existing);

            _records[recordKey] = new ConfirmPickupIdempotencyRecord(
                key,
                fingerprint,
                new ConfirmPickupResult(Guid.Empty, Guid.Empty));

            return Task.FromResult<ConfirmPickupIdempotencyRecord?>(null);
        }

        public Task CompleteAsync(
            Guid actorId,
            string operation,
            string key,
            ConfirmPickupResult result,
            CancellationToken cancellationToken)
        {
            var recordKey = BuildKey(actorId, operation, key);
            var current = _records[recordKey];
            _records[recordKey] = current with { Result = result };
            return Task.CompletedTask;
        }

        private static string BuildKey(Guid actorId, string operation, string key) =>
            $"{actorId:N}|{operation}|{key}";
    }

    private sealed class FakeUnitOfWork : IConfirmPickupUnitOfWork
    {
        public bool FailBeforeOperation { get; set; }

        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            if (FailBeforeOperation)
                throw new InvalidOperationException("Simulated transaction failure.");

            return operation(cancellationToken);
        }
    }
}
