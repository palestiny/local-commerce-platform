using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalCommerce.Application.Idempotency;
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

        return result ?? throw new InvalidOperationException(
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
                "The idempotency key was already used with a different request.");

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency record is incomplete.");

        var result = JsonSerializer.Deserialize<ReadyOrderForDeliveryResult>(
            existing.ResultPayload);

        if (result is null ||
            result.OrderId == Guid.Empty ||
            result.DeliveryId == Guid.Empty ||
            result.DeliveryId != existing.ResourceId.Value)
            throw new ReadyOrderForDeliveryRejectedException(
                "The idempotency record is invalid.");

        return result;
    }
}
