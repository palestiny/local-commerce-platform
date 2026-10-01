using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Idempotency;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class CompleteDeliveryHandlerTests
{
    [Fact]
    public async Task Assigned_driver_can_complete_out_for_delivery()
    {
        var fixture = Fixture.Create();
        var result = await fixture.Handler.HandleAsync(new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1"));
        Assert.Equal(fixture.Delivery.Id, result.DeliveryId);
        Assert.Equal(fixture.DriverId, result.DriverId);
        Assert.Equal(DeliveryEntityStatus.Delivered, fixture.Delivery.Status);
        Assert.NotNull(fixture.Delivery.DeliveredAt);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_complete_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Authorization.Allowed = false;
        var act = () => fixture.Handler.HandleAsync(new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1"));
        await Assert.ThrowsAsync<CompleteDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.OutForDelivery, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Non_assigned_driver_cannot_complete_delivery()
    {
        var fixture = Fixture.Create();
        var act = () => fixture.Handler.HandleAsync(new CompleteDeliveryCommand(fixture.Delivery.Id, Guid.NewGuid(), "complete-1"));
        await Assert.ThrowsAsync<CompleteDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.OutForDelivery, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Completion_requires_out_for_delivery()
    {
        var fixture = Fixture.Create();
        fixture.Delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
        fixture.Delivery.AssignDriver(fixture.DriverId);
        fixture.Repository.SetDelivery(fixture.Delivery);
        var act = () => fixture.Handler.HandleAsync(new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1"));
        await Assert.ThrowsAsync<CompleteDeliveryRejectedException>(act);
        Assert.Equal(DeliveryEntityStatus.Assigned, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_replays_original_result()
    {
        var fixture = Fixture.Create();
        var command = new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1");
        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);
        Assert.Equal(first, second);
        Assert.Equal(DeliveryEntityStatus.Delivered, fixture.Delivery.Status);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_request_is_rejected()
    {
        var fixture = Fixture.Create();
        var command = new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1");
        await fixture.Handler.HandleAsync(command);
        var act = () => fixture.Handler.HandleAsync(command with { DeliveryId = Guid.NewGuid() });
        await Assert.ThrowsAsync<CompleteDeliveryRejectedException>(act);
    }

    [Fact]
    public async Task Unit_of_work_failure_leaves_delivery_out_for_delivery()
    {
        var fixture = Fixture.Create();
        fixture.UnitOfWork.FailBeforeOperation = true;
        var act = () => fixture.Handler.HandleAsync(new CompleteDeliveryCommand(fixture.Delivery.Id, fixture.DriverId, "complete-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(DeliveryEntityStatus.OutForDelivery, fixture.Delivery.Status);
    }

    private sealed class Fixture
    {
        private Fixture(DeliveryEntity delivery, Guid driverId)
        {
            Delivery = delivery; DriverId = driverId; Repository = new FakeDeliveryRepository(delivery);
            Authorization = new FakeAuthorization(); IdempotencyStore = new FakeIdempotencyStore(); UnitOfWork = new FakeUnitOfWork();
            Handler = new CompleteDeliveryHandler(Repository, Authorization, IdempotencyStore, UnitOfWork);
        }
        public DeliveryEntity Delivery { get; set; }
        public Guid DriverId { get; }
        public FakeDeliveryRepository Repository { get; }
        public FakeAuthorization Authorization { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public CompleteDeliveryHandler Handler { get; }
        public static Fixture Create()
        {
            var driverId = Guid.NewGuid(); var delivery = DeliveryEntity.Create(Guid.NewGuid(), Guid.NewGuid());
            delivery.AssignDriver(driverId); delivery.ConfirmPickup(driverId); delivery.StartDelivery(); return new Fixture(delivery, driverId);
        }
    }
    private sealed class FakeDeliveryRepository : ICompleteDeliveryDeliveryRepository
    {
        private DeliveryEntity _delivery; public FakeDeliveryRepository(DeliveryEntity delivery) => _delivery = delivery;
        public Task<DeliveryEntity?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<DeliveryEntity?>(_delivery.Id == id ? _delivery : null);
        public void SetDelivery(DeliveryEntity delivery) => _delivery = delivery;
        public Task SaveAsync(DeliveryEntity delivery, CancellationToken ct) => Task.CompletedTask;
    }
    private sealed class FakeAuthorization : ICompleteDeliveryAuthorization
    {
        public bool Allowed { get; set; } = true;
        public Task<bool> CanCompleteDeliveryAsync(Guid actorId, DeliveryEntity delivery, CancellationToken ct) => Task.FromResult(Allowed && delivery.DriverId == actorId);
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
    private sealed class FakeUnitOfWork : ICompleteDeliveryUnitOfWork
    { public bool FailBeforeOperation {get;set;} public Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct){if(FailBeforeOperation)throw new InvalidOperationException("Simulated transaction failure.");return operation(ct);} }
}
