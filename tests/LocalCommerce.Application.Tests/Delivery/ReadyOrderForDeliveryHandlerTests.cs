using LocalCommerce.Application.Delivery;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;
using Xunit;

namespace LocalCommerce.Application.Tests.Delivery;

public sealed class ReadyOrderForDeliveryHandlerTests
{
    [Fact]
    public async Task Preparing_order_is_marked_ready_and_delivery_is_created_in_one_operation()
    {
        var fixture = Fixture.PreparingOrder();

        var result = await fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        Assert.Equal(fixture.Order.Id, result.OrderId);
        Assert.Equal(OrderStatus.ReadyForPickup, fixture.Order.Status);
        Assert.Single(fixture.DeliveryRepository.Deliveries);

        var delivery = fixture.DeliveryRepository.Deliveries.Single();
        Assert.Equal(fixture.Order.Id, delivery.OrderId);
        Assert.Equal(fixture.Order.StoreId, delivery.StoreId);
        Assert.Equal(DeliveryStatus.Unassigned, delivery.Status);
    }

    [Fact]
    public async Task Order_that_is_not_preparing_is_rejected_without_creating_delivery()
    {
        var fixture = Fixture.AcceptedOrder();

        var act = () => fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        await Assert.ThrowsAsync<DomainRuleViolationException>(act);
        Assert.Empty(fixture.DeliveryRepository.Deliveries);
    }

    [Fact]
    public async Task Existing_active_delivery_prevents_duplicate_delivery_creation()
    {
        var fixture = Fixture.PreparingOrder();
        fixture.DeliveryRepository.AddExisting(
            DeliveryEntity.Create(fixture.Order.Id, fixture.Order.StoreId));

        var act = () => fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        await Assert.ThrowsAsync<ReadyOrderForDeliveryRejectedException>(act);
        Assert.Equal(OrderStatus.Preparing, fixture.Order.Status);
        Assert.Single(fixture.DeliveryRepository.Deliveries);
    }

    [Fact]
    public async Task Unauthorized_actor_cannot_mark_order_ready()
    {
        var fixture = Fixture.PreparingOrder();
        fixture.Authorization.Allowed = false;

        var act = () => fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        await Assert.ThrowsAsync<ReadyOrderForDeliveryRejectedException>(act);
        Assert.Equal(OrderStatus.Preparing, fixture.Order.Status);
        Assert.Empty(fixture.DeliveryRepository.Deliveries);
    }

    [Fact]
    public async Task Same_idempotency_key_and_same_fingerprint_returns_original_result()
    {
        var fixture = Fixture.PreparingOrder();
        var command = new ReadyOrderForDeliveryCommand(
            fixture.Order.Id,
            fixture.ActorId,
            "ready-1");

        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);

        Assert.Equal(first, second);
        Assert.Single(fixture.DeliveryRepository.Deliveries);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_order_is_rejected()
    {
        var fixture = Fixture.PreparingOrder();

        await fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        var otherOrder = Fixture.PreparingOrder().Order;

        var act = () => fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                otherOrder.Id,
                fixture.ActorId,
                "ready-1"));

        await Assert.ThrowsAsync<ReadyOrderForDeliveryRejectedException>(act);
    }

    [Fact]
    public async Task Unit_of_work_failure_before_operation_leaves_order_and_delivery_unchanged()
    {
        var fixture = Fixture.PreparingOrder();
        fixture.UnitOfWork.FailBeforeOperation = true;

        var act = () => fixture.Handler.HandleAsync(
            new ReadyOrderForDeliveryCommand(
                fixture.Order.Id,
                fixture.ActorId,
                "ready-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(OrderStatus.Preparing, fixture.Order.Status);
        Assert.Empty(fixture.DeliveryRepository.Deliveries);
    }

    private sealed class Fixture
    {
        private Fixture(Order order)
        {
            Order = order;
            ActorId = Guid.NewGuid();
            OrderRepository = new FakeOrderRepository(order);
            DeliveryRepository = new FakeDeliveryRepository();
            Authorization = new FakeAuthorization();
            IdempotencyStore = new FakeIdempotencyStore();
            UnitOfWork = new FakeUnitOfWork();
            Handler = new ReadyOrderForDeliveryHandler(
                OrderRepository,
                DeliveryRepository,
                Authorization,
                IdempotencyStore,
                UnitOfWork);
        }

        public Order Order { get; }
        public Guid ActorId { get; }
        public FakeOrderRepository OrderRepository { get; }
        public FakeDeliveryRepository DeliveryRepository { get; }
        public FakeAuthorization Authorization { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public ReadyOrderForDeliveryHandler Handler { get; }

        public static Fixture PreparingOrder()
        {
            var order = CreateOrder();
            order.SubmitForStoreConfirmation();
            order.Accept();
            order.Prepare();
            return new Fixture(order);
        }

        public static Fixture AcceptedOrder()
        {
            var order = CreateOrder();
            order.SubmitForStoreConfirmation();
            order.Accept();
            return new Fixture(order);
        }

        private static Order CreateOrder()
        {
            var storeId = Guid.NewGuid();

            return Order.Create(
                storeId,
                [
                    new OrderItem(
                        Guid.NewGuid(),
                        storeId,
                        "Whole Milk",
                        "1L",
                        30m,
                        1,
                        0m,
                        30m)
                ]);
        }
    }

    private sealed class FakeOrderRepository(Order order) : IOrderForDeliveryRepository
    {
        public Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(order.Id == orderId ? order : null);

        public Task SaveAsync(Order order, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeDeliveryRepository : IDeliveryRepository
    {
        public List<DeliveryEntity> Deliveries { get; } = [];

        public Task<DeliveryEntity?> GetActiveByOrderIdAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Delivery?>(
                Deliveries.SingleOrDefault(x =>
                    x.OrderId == orderId &&
                    x.Status is not DeliveryStatus.Failed and not DeliveryStatus.Delivered));

        public Task AddAsync(DeliveryEntity delivery, CancellationToken cancellationToken)
        {
            Deliveries.Add(delivery);
            return Task.CompletedTask;
        }

        public void AddExisting(DeliveryEntity delivery) => Deliveries.Add(delivery);
    }

    private sealed class FakeAuthorization : IReadyForDeliveryAuthorization
    {
        public bool Allowed { get; set; } = true;

        public Task<bool> CanMarkReadyAsync(
            Guid actorId,
            Order order,
            CancellationToken cancellationToken) =>
            Task.FromResult(Allowed);
    }

    private sealed class FakeIdempotencyStore : IReadyForDeliveryIdempotencyStore
    {
        private readonly Dictionary<string, DeliveryIdempotencyRecord> records = [];

        public Task<DeliveryIdempotencyRecord?> GetAsync(
            Guid actorId,
            string operation,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                records.TryGetValue(key, out var record)
                    ? record
                    : null);

        public Task<DeliveryIdempotencyRecord?> ReserveAsync(
            Guid actorId,
            string operation,
            string key,
            string fingerprint,
            CancellationToken cancellationToken)
        {
            if (records.TryGetValue(key, out var existing))
                return Task.FromResult<DeliveryIdempotencyRecord?>(existing);

            records[key] = new DeliveryIdempotencyRecord(
                key,
                fingerprint,
                new ReadyOrderForDeliveryResult(Guid.Empty, Guid.Empty));

            return Task.FromResult<DeliveryIdempotencyRecord?>(null);
        }

        public Task CompleteAsync(
            Guid actorId,
            string operation,
            string key,
            ReadyOrderForDeliveryResult result,
            CancellationToken cancellationToken)
        {
            var existing = records[key];
            records[key] = existing with { Result = result };
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IReadyForDeliveryUnitOfWork
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
