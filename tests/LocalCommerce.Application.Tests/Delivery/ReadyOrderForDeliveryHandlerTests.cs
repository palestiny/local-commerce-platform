using LocalCommerce.Application.Delivery;
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
        Assert.Equal(fixture.Order.Id, fixture.DeliveryRepository.Deliveries.Single().OrderId);
        Assert.Equal(fixture.Order.StoreId, fixture.DeliveryRepository.Deliveries.Single().StoreId);
        Assert.Equal(DeliveryStatus.Unassigned, fixture.DeliveryRepository.Deliveries.Single().Status);
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

        await Assert.ThrowsAsync<ReadyOrderForDeliveryRejectedException>(act);
        Assert.Empty(fixture.DeliveryRepository.Deliveries);
    }

    [Fact]
    public async Task Existing_active_delivery_prevents_duplicate_delivery_creation()
    {
        var fixture = Fixture.PreparingOrder();
        fixture.DeliveryRepository.AddExisting(
            Delivery.Create(fixture.Order.Id, fixture.Order.StoreId));

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
    public async Task Failure_during_coordinated_operation_does_not_leave_order_ready_or_delivery_created()
    {
        var fixture = Fixture.PreparingOrder();
        fixture.UnitOfWork.FailAfterOperation = true;

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
            UnitOfWork = new FakeUnitOfWork(OrderRepository, DeliveryRepository);
            Handler = new ReadyOrderForDeliveryHandler(
                OrderRepository,
                DeliveryRepository,
                UnitOfWork);
        }

        public Order Order { get; }
        public Guid ActorId { get; }
        public FakeOrderRepository OrderRepository { get; }
        public FakeDeliveryRepository DeliveryRepository { get; }
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

        private static Order CreateOrder() =>
            Order.Create(
                Guid.NewGuid(),
                [
                    new OrderItem(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "Whole Milk",
                        "1L",
                        30m,
                        1,
                        0m,
                        30m)
                ]);
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
        public List<Delivery> Deliveries { get; } = [];

        public Task<Delivery?> GetActiveByOrderIdAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Delivery?>(
                Deliveries.SingleOrDefault(x =>
                    x.OrderId == orderId &&
                    x.Status != DeliveryStatus.Failed));

        public Task AddAsync(Delivery delivery, CancellationToken cancellationToken)
        {
            Deliveries.Add(delivery);
            return Task.CompletedTask;
        }

        public void AddExisting(Delivery delivery) => Deliveries.Add(delivery);
    }

    private sealed class FakeUnitOfWork(
        FakeOrderRepository orderRepository,
        FakeDeliveryRepository deliveryRepository) : IReadyForDeliveryUnitOfWork
    {
        public bool FailAfterOperation { get; set; }

        public async Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            var originalOrderStatus = orderRepository
                .GetAsync(Guid.Empty, cancellationToken);

            await operation(cancellationToken);

            if (FailAfterOperation)
            {
                deliveryRepository.Deliveries.Clear();
                throw new InvalidOperationException("Persistence failure.");
            }

            _ = originalOrderStatus;
        }
    }
}
