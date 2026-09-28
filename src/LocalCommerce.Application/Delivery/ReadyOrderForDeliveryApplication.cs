using LocalCommerce.Domain.Delivery;
using LocalCommerce.Domain.Ordering;

namespace LocalCommerce.Application.Delivery;

public sealed record ReadyOrderForDeliveryCommand(
    Guid OrderId,
    Guid ActorId,
    string IdempotencyKey);

public sealed record ReadyOrderForDeliveryResult(
    Guid OrderId,
    Guid DeliveryId);

public interface IOrderForDeliveryRepository
{
    Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken);
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public interface IDeliveryRepository
{
    Task<Delivery?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(Delivery delivery, CancellationToken cancellationToken);
}

public interface IReadyForDeliveryUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public sealed class ReadyOrderForDeliveryRejectedException : Exception
{
    public ReadyOrderForDeliveryRejectedException(string message) : base(message) { }
}

public sealed class ReadyOrderForDeliveryHandler
{
    public ReadyOrderForDeliveryHandler(
        IOrderForDeliveryRepository orderRepository,
        IDeliveryRepository deliveryRepository,
        IReadyForDeliveryUnitOfWork unitOfWork)
    {
        OrderRepository = orderRepository;
        DeliveryRepository = deliveryRepository;
        UnitOfWork = unitOfWork;
    }

    private IOrderForDeliveryRepository OrderRepository { get; }
    private IDeliveryRepository DeliveryRepository { get; }
    private IReadyForDeliveryUnitOfWork UnitOfWork { get; }

    public Task<ReadyOrderForDeliveryResult> HandleAsync(
        ReadyOrderForDeliveryCommand command,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("M1 Application TDD RED: handler behavior is not implemented yet.");
}
