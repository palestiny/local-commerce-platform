using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
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
    Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IReadyForDeliveryAuthorization
{
    Task<bool> CanMarkReadyAsync(Guid actorId, Order order, CancellationToken cancellationToken);
}

public interface IReadyForDeliveryUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public sealed class ReadyOrderForDeliveryRejectedException : ApplicationFailureException
{
    public ReadyOrderForDeliveryRejectedException(string message, string code = ApplicationErrorCodes.OrderInvalidState) : base(code, message) { }
}

public sealed class ReadyOrderForDeliveryHandler
{
    private const string Operation = "ReadyOrderForDelivery";

    public ReadyOrderForDeliveryHandler(
        IOrderForDeliveryRepository orderRepository,
        IDeliveryRepository deliveryRepository,
        IReadyForDeliveryAuthorization authorization,
        IIdempotencyStore idempotencyStore,
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
    private IIdempotencyStore IdempotencyStore { get; }
    private IReadyForDeliveryUnitOfWork UnitOfWork { get; }

    public async Task<ReadyOrderForDeliveryResult> HandleAsync(
        ReadyOrderForDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrderId == Guid.Empty)
            throw new ReadyOrderForDeliveryRejectedException("Order is required.", ApplicationErrorCodes.RequestInvalid);
        if (command.ActorId == Guid.Empty)
            throw new ReadyOrderForDeliveryRejectedException("Actor is required.", ApplicationErrorCodes.RequestInvalid);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ReadyOrderForDeliveryRejectedException("Idempotency key is required.", ApplicationErrorCodes.IdempotencyKeyRequired);

        var fingerprint = BuildFingerprint(command);
        var existing = await IdempotencyStore.GetAsync(
            command.ActorId,
            Operation,
            command.IdempotencyKey,
            cancellationToken);
        if (existing is not null)
            return ValidateExisting(existing, fingerprint);

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

            // Cross-aggregate mutation lock order is Order first, then Delivery.
            // Cancellation follows the same order to avoid lock inversion.
            var order = await OrderRepository.GetAsync(
                command.OrderId,
                transactionCancellationToken);

            if (order is null)
                throw new ReadyOrderForDeliveryRejectedException("Order was not found.", ApplicationErrorCodes.ResourceNotFound);

            if (!await Authorization.CanMarkReadyAsync(
                    command.ActorId,
                    order,
                    transactionCancellationToken))
                throw new ReadyOrderForDeliveryRejectedException("Actor is not authorized to mark this Order ready.", ApplicationErrorCodes.AuthorizationForbidden);

            var activeDelivery = await DeliveryRepository.GetActiveByOrderIdAsync(
                order.Id,
                transactionCancellationToken);

            if (activeDelivery is not null)
                throw new ReadyOrderForDeliveryRejectedException(
                    "The Order already has an active Delivery.", ApplicationErrorCodes.DeliveryInvalidState);

            try { order.MarkReadyForPickup(); }
            catch (DomainRuleViolationException exception) { throw new ReadyOrderForDeliveryRejectedException(exception.Message, ApplicationErrorCodes.OrderInvalidState); }

            var delivery = DeliveryEntity.Create(order.Id, order.StoreId);

            await OrderRepository.SaveAsync(order, transactionCancellationToken);
            await DeliveryRepository.AddAsync(delivery, transactionCancellationToken);

            result = new ReadyOrderForDeliveryResult(order.Id, delivery.Id);

            await IdempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                new IdempotencyCompletion(
                    "Delivery",
                    result.DeliveryId,
                    JsonSerializer.Serialize(result)),
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new ApplicationFailureException(ApplicationErrorCodes.InternalUnexpected,
            "Ready Order for Delivery completed without a result.");
    }

    private static string BuildFingerprint(ReadyOrderForDeliveryCommand command) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    $"{command.OrderId:N}|{command.ActorId:N}")));

    private static ReadyOrderForDeliveryResult ValidateExisting(
        IdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency key was already used with a different request.", ApplicationErrorCodes.IdempotencyKeyReused);

        if (existing.Status == IdempotencyStatus.Reserved)
            throw new ReadyOrderForDeliveryRejectedException(
                "A persisted idempotency reservation cannot be proven to be in progress.", ApplicationErrorCodes.InternalUnexpected);

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        var result = JsonSerializer.Deserialize<ReadyOrderForDeliveryResult>(
            existing.ResultPayload);

        if (result is null ||
            result.OrderId == Guid.Empty ||
            result.DeliveryId == Guid.Empty ||
            result.DeliveryId != existing.ResourceId.Value)
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        return result;
    }
}
