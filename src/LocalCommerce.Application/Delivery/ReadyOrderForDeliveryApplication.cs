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

public sealed record DeliveryIdempotencyRecord(
    string Key,
    string Fingerprint,
    ReadyOrderForDeliveryResult Result);

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

public interface IReadyForDeliveryAuthorization
{
    Task<bool> CanMarkReadyAsync(
        Guid actorId,
        Order order,
        CancellationToken cancellationToken);
}

public interface IReadyForDeliveryIdempotencyStore
{
    Task<DeliveryIdempotencyRecord?> GetAsync(
        Guid actorId,
        string operation,
        string key,
        CancellationToken cancellationToken);

    Task<DeliveryIdempotencyRecord?> ReserveAsync(
        Guid actorId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid actorId,
        string operation,
        string key,
        ReadyOrderForDeliveryResult result,
        CancellationToken cancellationToken);
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
    private const string Operation = "ReadyOrderForDelivery";

    public ReadyOrderForDeliveryHandler(
        IOrderForDeliveryRepository orderRepository,
        IDeliveryRepository deliveryRepository,
        IReadyForDeliveryAuthorization authorization,
        IReadyForDeliveryIdempotencyStore idempotencyStore,
        IReadyForDeliveryUnitOfWork unitOfWork)
    {
        OrderRepository = orderRepository;
        DeliveryRepository = deliveryRepository;
        Authorization = authorization;
        IdempotencyStore = idempotencyStore;
        UnitOfWork = unitOfWork;
    }

    private IOrderForDeliveryRepository OrderRepository { get; }
    private IDeliveryRepository DeliveryRepository { get; }
    private IReadyForDeliveryAuthorization Authorization { get; }
    private IReadyForDeliveryIdempotencyStore IdempotencyStore { get; }
    private IReadyForDeliveryUnitOfWork UnitOfWork { get; }

    public async Task<ReadyOrderForDeliveryResult> HandleAsync(
        ReadyOrderForDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrderId == Guid.Empty)
            throw new ReadyOrderForDeliveryRejectedException("Order is required.");

        if (command.ActorId == Guid.Empty)
            throw new ReadyOrderForDeliveryRejectedException("Actor is required.");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ReadyOrderForDeliveryRejectedException("Idempotency key is required.");

        var fingerprint = BuildFingerprint(command);

        var existing = await IdempotencyStore.GetAsync(
            command.ActorId,
            Operation,
            command.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ValidateExisting(existing, fingerprint);

        var order = await OrderRepository.GetAsync(command.OrderId, cancellationToken);

        if (order is null)
            throw new ReadyOrderForDeliveryRejectedException("Order was not found.");

        if (!await Authorization.CanMarkReadyAsync(
                command.ActorId,
                order,
                cancellationToken))
            throw new ReadyOrderForDeliveryRejectedException("Actor is not authorized to mark this Order ready.");

        ReadyOrderForDeliveryResult? result = null;

        await UnitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            var reserved = await IdempotencyStore.ReserveAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                fingerprint,
                transactionCancellationToken);

            if (reserved is not null)
            {
                result = ValidateExisting(reserved, fingerprint);
                return;
            }

            var activeDelivery = await DeliveryRepository.GetActiveByOrderIdAsync(
                order.Id,
                transactionCancellationToken);

            if (activeDelivery is not null)
                throw new ReadyOrderForDeliveryRejectedException(
                    "The Order already has an active Delivery.");

            order.MarkReadyForPickup();

            var delivery = Delivery.Create(order.Id, order.StoreId);

            await OrderRepository.SaveAsync(order, transactionCancellationToken);
            await DeliveryRepository.AddAsync(delivery, transactionCancellationToken);

            result = new ReadyOrderForDeliveryResult(order.Id, delivery.Id);

            await IdempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                result,
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new InvalidOperationException(
            "Ready Order for Delivery completed without a result.");
    }

    private static string BuildFingerprint(ReadyOrderForDeliveryCommand command) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{command.OrderId:N}|{command.ActorId:N}")));

    private static ReadyOrderForDeliveryResult ValidateExisting(
        DeliveryIdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency key was already used with a different request.");

        return existing.Result;
    }
}
