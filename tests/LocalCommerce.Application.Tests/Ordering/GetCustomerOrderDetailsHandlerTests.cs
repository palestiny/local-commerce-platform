using LocalCommerce.Application.Ordering;
using LocalCommerce.Application.Errors;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;
using Xunit;

namespace LocalCommerce.Application.Tests.Ordering;

public sealed class GetCustomerOrderDetailsHandlerTests
{
    [Fact]
    public async Task Customer_can_read_own_order_with_delivery_summary()
    {
        var fixture = Fixture.Create();

        var result = await fixture.Handler.HandleAsync(
            new GetCustomerOrderDetailsQuery(fixture.CustomerId, fixture.Order.Id));

        Assert.Equal(fixture.Order.Id, result.OrderId);
        Assert.Equal(fixture.OrderNumber, result.OrderNumber);
        Assert.Equal(OrderStatus.ReadyForPickup, result.OrderStatus);
        Assert.NotNull(result.Delivery);
        Assert.Equal(fixture.Delivery!.Id, result.Delivery!.DeliveryId);
        Assert.Equal(DeliveryStatus.Assigned, result.Delivery.Status);
        Assert.Equal(fixture.Delivery.AssignedAt, result.Delivery.AssignedAt);
    }

    [Fact]
    public async Task Customer_can_read_own_order_before_delivery_exists()
    {
        var fixture = Fixture.Create(withDelivery: false);

        var result = await fixture.Handler.HandleAsync(
            new GetCustomerOrderDetailsQuery(fixture.CustomerId, fixture.Order.Id));

        Assert.Equal(fixture.Order.Id, result.OrderId);
        Assert.Equal(OrderStatus.ReadyForPickup, result.OrderStatus);
        Assert.Null(result.Delivery);
    }

    [Fact]
    public async Task Customer_cannot_read_another_customers_order()
    {
        var fixture = Fixture.Create();
        fixture.Authorization.Allowed = false;

        var act = () => fixture.Handler.HandleAsync(
            new GetCustomerOrderDetailsQuery(Guid.NewGuid(), fixture.Order.Id));

        var error = await Assert.ThrowsAsync<GetCustomerOrderDetailsRejectedException>(act);
        Assert.Equal(ApplicationErrorCodes.ResourceNotFound, error.Code);
    }

    [Fact]
    public async Task Missing_order_is_rejected()
    {
        var fixture = Fixture.Create();

        var act = () => fixture.Handler.HandleAsync(
            new GetCustomerOrderDetailsQuery(fixture.CustomerId, Guid.NewGuid()));

        await Assert.ThrowsAsync<GetCustomerOrderDetailsRejectedException>(act);
    }

    [Fact]
    public async Task Read_does_not_mutate_order_or_delivery()
    {
        var fixture = Fixture.Create();
        var orderStatus = fixture.Order.Status;
        var deliveryStatus = fixture.Delivery!.Status;

        _ = await fixture.Handler.HandleAsync(
            new GetCustomerOrderDetailsQuery(fixture.CustomerId, fixture.Order.Id));

        Assert.Equal(orderStatus, fixture.Order.Status);
        Assert.Equal(deliveryStatus, fixture.Delivery.Status);
    }

    private sealed class Fixture
    {
        private Fixture(Order order, DeliveryEntity? delivery, Guid customerId, string orderNumber)
        {
            Order = order;
            Delivery = delivery;
            CustomerId = customerId;
            OrderNumber = orderNumber;
            OrderRepository = new FakeOrderRepository(order, orderNumber);
            DeliveryRepository = new FakeDeliveryRepository(delivery);
            Authorization = new FakeAuthorization();
            Handler = new GetCustomerOrderDetailsHandler(
                OrderRepository,
                DeliveryRepository,
                Authorization);
        }

        public Order Order { get; }
        public DeliveryEntity? Delivery { get; }
        public Guid CustomerId { get; }
        public string OrderNumber { get; }
        public FakeOrderRepository OrderRepository { get; }
        public FakeDeliveryRepository DeliveryRepository { get; }
        public FakeAuthorization Authorization { get; }
        public GetCustomerOrderDetailsHandler Handler { get; }

        public static Fixture Create(bool withDelivery = true)
        {
            var customerId = Guid.NewGuid();
            var storeId = Guid.NewGuid();
            var item = new OrderItem(
                Guid.NewGuid(),
                storeId,
                "Milk",
                null,
                12.50m,
                1,
                0m,
                12.50m);

            var order = Order.Create(storeId, new[] { item });
            order.SubmitForStoreConfirmation();
            order.Accept();
            order.Prepare();
            order.MarkReadyForPickup();

            DeliveryEntity? delivery = null;
            if (withDelivery)
            {
                delivery = DeliveryEntity.Create(order.Id, storeId);
                delivery.AssignDriver(Guid.NewGuid());
            }

            return new Fixture(order, delivery, customerId, "ORD-TEST-001");
        }
    }

    private sealed class FakeOrderRepository : ICustomerOrderDetailsOrderRepository
    {
        private readonly Order _order;
        private readonly string _orderNumber;

        public FakeOrderRepository(Order order, string orderNumber)
        {
            _order = order;
            _orderNumber = orderNumber;
        }

        public Task<CustomerOrderSnapshot?> GetAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CustomerOrderSnapshot?>(
                _order.Id == orderId
                    ? new CustomerOrderSnapshot(_order, _orderNumber)
                    : null);
    }

    private sealed class FakeDeliveryRepository : ICustomerOrderDetailsDeliveryRepository
    {
        private readonly DeliveryEntity? _delivery;

        public FakeDeliveryRepository(DeliveryEntity? delivery) => _delivery = delivery;

        public Task<DeliveryEntity?> GetActiveByOrderIdAsync(
            Guid orderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                _delivery is not null && _delivery.OrderId == orderId
                    ? _delivery
                    : null);
    }

    private sealed class FakeAuthorization : ICustomerOrderDetailsAuthorization
    {
        public bool Allowed { get; set; } = true;

        public Task<bool> CanReadAsync(
            Guid customerId,
            CustomerOrderSnapshot order,
            CancellationToken cancellationToken) =>
            Task.FromResult(Allowed);
    }
}
