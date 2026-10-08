using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;

namespace LocalCommerce.Application.Delivery;

public sealed record CreateReplacementDeliveryCommand(Guid OrderId, Guid StoreId, Guid ActorId, string IdempotencyKey);
public sealed record CreateReplacementDeliveryResult(Guid DeliveryId);

public sealed class CreateReplacementDeliveryRejectedException(string message) : Exception(message);

public interface ICreateReplacementDeliveryRepository
{
    Task<DeliveryEntity?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task AddAsync(DeliveryEntity delivery, CancellationToken ct);
}

public interface IReplacementDeliveryOrderLock
{
    Task<bool> LockOrderForMutationAsync(Guid orderId, CancellationToken ct);
}

public interface IReplacementDeliveryEligibility
{
    Task<bool> IsOrderEligibleAsync(Guid orderId, CancellationToken ct);
}

public interface ICreateReplacementDeliveryUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}

public sealed class CreateReplacementDeliveryHandler(
    ICreateReplacementDeliveryRepository repository,
    IReplacementDeliveryOrderLock orderLock,
    IReplacementDeliveryEligibility eligibility,
    IIdempotencyStore idempotencyStore,
    ICreateReplacementDeliveryUnitOfWork unitOfWork)
{
    private const string Operation = "CreateReplacementDelivery";

    public async Task<CreateReplacementDeliveryResult> HandleAsync(
        CreateReplacementDeliveryCommand command,
        CancellationToken ct = default)
    {
        if (command.OrderId == Guid.Empty || command.StoreId == Guid.Empty || command.ActorId == Guid.Empty)
            throw new CreateReplacementDeliveryRejectedException("Order, Store and Actor are required.");
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new CreateReplacementDeliveryRejectedException("Idempotency key is required.");

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{command.OrderId:N}|{command.StoreId:N}|{command.ActorId:N}")));

        var existing = await idempotencyStore.GetAsync(command.ActorId, Operation, command.IdempotencyKey, ct);
        if (existing is not null) return Resolve(existing, fingerprint);

        CreateReplacementDeliveryResult? result = null;

        await unitOfWork.ExecuteAsync(async tx =>
        {
            var reserved = await idempotencyStore.ReserveAsync(
                command.ActorId, Operation, command.IdempotencyKey, fingerprint, tx);

            if (reserved is not null)
            {
                result = Resolve(reserved, fingerprint);
                return;
            }

            // Use the same cross-aggregate lock order as ReadyOrderForDelivery and CancelOrder:
            // lock Order first, then inspect/lock its active Delivery.
            if (!await orderLock.LockOrderForMutationAsync(command.OrderId, tx))
                throw new CreateReplacementDeliveryRejectedException("Order was not found.");

            if (!await eligibility.IsOrderEligibleAsync(command.OrderId, tx))
                throw new CreateReplacementDeliveryRejectedException("Order is not eligible for replacement delivery.");

            if (await repository.GetActiveByOrderIdAsync(command.OrderId, tx) is not null)
                throw new CreateReplacementDeliveryRejectedException("An active Delivery already exists.");

            var delivery = DeliveryEntity.Create(command.OrderId, command.StoreId);
            await repository.AddAsync(delivery, tx);

            result = new CreateReplacementDeliveryResult(delivery.Id);
            await idempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                new IdempotencyCompletion(
                    "Delivery",
                    result.DeliveryId,
                    System.Text.Json.JsonSerializer.Serialize(result)),
                tx);
        }, ct);

        return result ?? throw new InvalidOperationException("Replacement delivery completed without a result.");
    }

    private static CreateReplacementDeliveryResult Resolve(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new CreateReplacementDeliveryRejectedException("The idempotency key was already used with a different request.");

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new CreateReplacementDeliveryRejectedException("Idempotency record is incomplete.");

        var result = System.Text.Json.JsonSerializer.Deserialize<CreateReplacementDeliveryResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DeliveryId != existing.ResourceId.Value)
            throw new CreateReplacementDeliveryRejectedException("Idempotency record is invalid.");

        return result;
    }
}
