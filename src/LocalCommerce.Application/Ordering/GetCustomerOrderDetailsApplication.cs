using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;

namespace LocalCommerce.Application.Ordering;

public sealed record GetCustomerOrderDetailsQuery(
    Guid CustomerId,
    Guid OrderId);

public sealed record CustomerOrderSnapshot(
    Order Order,
    string OrderNumber);

public sealed record CustomerOrderDetailsDelivery(
    Guid DeliveryId,
    DeliveryStatus Status,
    DateTimeOffset? AssignedAt);

public sealed record CustomerOrderDetailsResult(
    Guid OrderId,
    string OrderNumber,
    OrderStatus OrderStatus,
    CustomerOrderDetailsDelivery? Delivery);

public interface ICustomerOrderDetailsOrderRepository
{
    Task<CustomerOrderSnapshot?> GetAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface ICustomerOrderDetailsDeliveryRepository
{
    Task<Delivery?> GetActiveByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken);
}

public interface ICustomerOrderDetailsAuthorization
{
    Task<bool> CanReadAsync(
        Guid customerId,
        CustomerOrderSnapshot order,
        CancellationToken cancellationToken);
}

public sealed class GetCustomerOrderDetailsRejectedException : Exception
{
    public GetCustomerOrderDetailsRejectedException(string message)
        : base(message) { }
}

public sealed class GetCustomerOrderDetailsHandler
{
    public GetCustomerOrderDetailsHandler(
        ICustomerOrderDetailsOrderRepository orderRepository,
        ICustomerOrderDetailsDeliveryRepository deliveryRepository,
        ICustomerOrderDetailsAuthorization authorization)
    {
        OrderRepository = orderRepository;
        DeliveryRepository = deliveryRepository;
        Authorization = authorization;
    }

    private ICustomerOrderDetailsOrderRepository OrderRepository { get; }
    private ICustomerOrderDetailsDeliveryRepository DeliveryRepository { get; }
    private ICustomerOrderDetailsAuthorization Authorization { get; }

    public async Task<CustomerOrderDetailsResult> HandleAsync(
        GetCustomerOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CustomerId == Guid.Empty)
            throw new GetCustomerOrderDetailsRejectedException("Customer is required.");

        if (query.OrderId == Guid.Empty)
            throw new GetCustomerOrderDetailsRejectedException("Order is required.");

        var order = await OrderRepository.GetAsync(
            query.OrderId,
            cancellationToken);

        if (order is null)
            throw new GetCustomerOrderDetailsRejectedException("Order was not found.");

        if (!await Authorization.CanReadAsync(
                query.CustomerId,
                order,
                cancellationToken))
            throw new GetCustomerOrderDetailsRejectedException(
                "Customer is not authorized to read this Order.");

        var delivery = await DeliveryRepository.GetActiveByOrderIdAsync(
            order.Order.Id,
            cancellationToken);

        var deliverySummary = delivery is null
            ? null
            : new CustomerOrderDetailsDelivery(
                delivery.Id,
                delivery.Status,
                delivery.AssignedAt);

        return new CustomerOrderDetailsResult(
            order.Order.Id,
            order.OrderNumber,
            order.Order.Status,
            deliverySummary);
    }
}
