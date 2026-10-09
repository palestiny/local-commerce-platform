using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;

namespace LocalCommerce.Application.Delivery;

public sealed record CompleteDeliveryCommand(Guid DeliveryId, Guid ActorId, string IdempotencyKey);
public sealed record CompleteDeliveryResult(Guid DeliveryId, Guid DriverId);

public sealed class CompleteDeliveryRejectedException(string message, string code = ApplicationErrorCodes.DeliveryInvalidState) : ApplicationFailureException(code, message);

public interface ICompleteDeliveryDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken ct);
}

public interface ICompleteDeliveryAuthorization
{
    Task<bool> CanCompleteDeliveryAsync(Guid actorId, DeliveryEntity delivery, CancellationToken ct);
}

public interface ICompleteDeliveryUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}

public sealed class CompleteDeliveryHandler(
    ICompleteDeliveryDeliveryRepository deliveries,
    ICompleteDeliveryAuthorization authorization,
    IIdempotencyStore idempotencyStore,
    ICompleteDeliveryUnitOfWork unitOfWork)
{
    private const string Operation = "CompleteDelivery";

    public async Task<CompleteDeliveryResult> HandleAsync(
        CompleteDeliveryCommand command,
        CancellationToken ct = default)
    {
        if (command.DeliveryId == Guid.Empty) throw new CompleteDeliveryRejectedException("DeliveryId is required.", ApplicationErrorCodes.RequestInvalid);
        if (command.ActorId == Guid.Empty) throw new CompleteDeliveryRejectedException("ActorId is required.", ApplicationErrorCodes.RequestInvalid);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) throw new CompleteDeliveryRejectedException("IdempotencyKey is required.", ApplicationErrorCodes.IdempotencyKeyRequired);

        var fingerprint = Fingerprint(command);
        var existing = await idempotencyStore.GetAsync(command.ActorId, Operation, command.IdempotencyKey, ct);
        if (existing is not null) return Resolve(existing, fingerprint);

        CompleteDeliveryResult? result = null;

        await unitOfWork.ExecuteAsync(async tx =>
        {
            var reserved = await idempotencyStore.ReserveAsync(command.ActorId, Operation, command.IdempotencyKey, fingerprint, tx);
            if (reserved is not null)
            {
                result = Resolve(reserved, fingerprint);
                return;
            }

            var delivery = await deliveries.GetAsync(command.DeliveryId, tx)
                ?? throw new CompleteDeliveryRejectedException("Delivery was not found.", ApplicationErrorCodes.ResourceNotFound);

            if (delivery.Status != DeliveryEntityStatus.OutForDelivery)
                throw new CompleteDeliveryRejectedException("Delivery must be OUT_FOR_DELIVERY.");

            if (!await authorization.CanCompleteDeliveryAsync(command.ActorId, delivery, tx))
                throw new CompleteDeliveryRejectedException("Actor is not authorized to complete delivery.", ApplicationErrorCodes.AuthorizationForbidden);

            var driverId = delivery.DriverId;
            if (driverId is null || driverId == Guid.Empty)
                throw new CompleteDeliveryRejectedException("Delivery must have an assigned driver.");

            try { delivery.Complete(); }
            catch (DomainRuleViolationException exception) { throw new CompleteDeliveryRejectedException(exception.Message, ApplicationErrorCodes.DeliveryInvalidState); }
            await deliveries.SaveAsync(delivery, tx);

            result = new CompleteDeliveryResult(delivery.Id, driverId.Value);
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

        return result ?? throw new ApplicationFailureException(ApplicationErrorCodes.InternalUnexpected, "Delivery completion completed without a result.");
    }

    private static CompleteDeliveryResult Resolve(IdempotencyRecord existing, string fp)
    {
        if (!string.Equals(existing.Fingerprint, fp, StringComparison.Ordinal))
            throw new CompleteDeliveryRejectedException("Idempotency key was already used with a different request.", ApplicationErrorCodes.IdempotencyKeyReused);

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new CompleteDeliveryRejectedException("Idempotency record is incomplete.", ApplicationErrorCodes.IdempotencyResultUnavailable);

        var result = System.Text.Json.JsonSerializer.Deserialize<CompleteDeliveryResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DriverId == Guid.Empty ||
            result.DeliveryId != existing.ResourceId.Value)
            throw new CompleteDeliveryRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        return result;
    }

    private static string Fingerprint(CompleteDeliveryCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{command.DeliveryId:N}|{command.ActorId:N}")));
}
