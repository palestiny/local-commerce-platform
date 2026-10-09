using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;

namespace LocalCommerce.Application.Delivery;

public sealed record FailDeliveryCommand(Guid DeliveryId, Guid ActorId, string FailureCode, string FailureReason, string IdempotencyKey);
public sealed record FailDeliveryResult(Guid DeliveryId);

public sealed class FailDeliveryRejectedException(string message, string code = ApplicationErrorCodes.DeliveryInvalidState) : ApplicationFailureException(code, message);

public interface IFailDeliveryDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken ct);
}

public interface IFailDeliveryAuthorization
{
    Task<bool> CanFailDeliveryAsync(Guid actorId, DeliveryEntity delivery, CancellationToken ct);
}

public interface IFailDeliveryUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}

public sealed class FailDeliveryHandler(
    IFailDeliveryDeliveryRepository deliveries,
    IFailDeliveryAuthorization authorization,
    IIdempotencyStore idempotencyStore,
    IFailDeliveryUnitOfWork unitOfWork)
{
    private const string Operation = "FailDelivery";

    public async Task<FailDeliveryResult> HandleAsync(
        FailDeliveryCommand command,
        CancellationToken ct = default)
    {
        if (command.DeliveryId == Guid.Empty || command.ActorId == Guid.Empty)
            throw new FailDeliveryRejectedException("Delivery and Actor are required.", ApplicationErrorCodes.RequestInvalid);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new FailDeliveryRejectedException("Idempotency key is required.", ApplicationErrorCodes.IdempotencyKeyRequired);
        if (string.IsNullOrWhiteSpace(command.FailureCode) || string.IsNullOrWhiteSpace(command.FailureReason))
            throw new FailDeliveryRejectedException("Failure code and reason are required.", ApplicationErrorCodes.RequestInvalid);

        var fingerprint = Fingerprint(command);
        var existing = await idempotencyStore.GetAsync(command.ActorId, Operation, command.IdempotencyKey, ct);
        if (existing is not null) return Resolve(existing, fingerprint);

        FailDeliveryResult? result = null;

        await unitOfWork.ExecuteAsync(async tx =>
        {
            var reserved = await idempotencyStore.ReserveAsync(command.ActorId, Operation, command.IdempotencyKey, fingerprint, tx);
            if (reserved is not null)
            {
                result = Resolve(reserved, fingerprint);
                return;
            }

            var delivery = await deliveries.GetAsync(command.DeliveryId, tx)
                ?? throw new FailDeliveryRejectedException("Delivery was not found.", ApplicationErrorCodes.ResourceNotFound);

            if (delivery.Status is DeliveryEntityStatus.Delivered or DeliveryEntityStatus.Failed)
                throw new FailDeliveryRejectedException("Terminal Delivery cannot be failed.");

            if (!await authorization.CanFailDeliveryAsync(command.ActorId, delivery, tx))
                throw new FailDeliveryRejectedException("Actor is not authorized to fail delivery.", ApplicationErrorCodes.AuthorizationForbidden);

            try { delivery.Fail(command.FailureCode, command.FailureReason); }
            catch (DomainRuleViolationException exception) { throw new FailDeliveryRejectedException(exception.Message, ApplicationErrorCodes.DeliveryInvalidState); }
            await deliveries.SaveAsync(delivery, tx);

            result = new FailDeliveryResult(delivery.Id);
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

        return result ?? throw new ApplicationFailureException(ApplicationErrorCodes.InternalUnexpected, "Delivery failure completed without a result.");
    }

    private static string Fingerprint(FailDeliveryCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{command.DeliveryId:N}|{command.ActorId:N}|{command.FailureCode}|{command.FailureReason}")));

    private static FailDeliveryResult Resolve(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new FailDeliveryRejectedException("The idempotency key was already used with a different request.", ApplicationErrorCodes.IdempotencyKeyReused);

        if (existing.Status == IdempotencyStatus.Reserved)
            throw new FailDeliveryRejectedException("Idempotency operation is still in progress.", ApplicationErrorCodes.IdempotencyResultUnavailable);

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new FailDeliveryRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        var result = System.Text.Json.JsonSerializer.Deserialize<FailDeliveryResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DeliveryId != existing.ResourceId.Value)
            throw new FailDeliveryRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        return result;
    }
}
