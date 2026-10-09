using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using LocalCommerce.Domain.Delivery;

namespace LocalCommerce.Application.Delivery;

public sealed record ConfirmPickupCommand(Guid DeliveryId, Guid ActorId, string IdempotencyKey);
public sealed record ConfirmPickupResult(Guid DeliveryId, Guid DriverId);

public sealed class ConfirmPickupRejectedException(string message, string code = ApplicationErrorCodes.DeliveryInvalidState) : ApplicationFailureException(code, message);

public interface IConfirmPickupDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IConfirmPickupAuthorization
{
    Task<bool> CanConfirmPickupAsync(Guid actorId, DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IConfirmPickupUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}

public sealed class ConfirmPickupHandler(
    IConfirmPickupDeliveryRepository deliveryRepository,
    IConfirmPickupAuthorization authorization,
    IIdempotencyStore idempotencyStore,
    IConfirmPickupUnitOfWork unitOfWork)
{
    private const string Operation = "ConfirmPickup";

    public async Task<ConfirmPickupResult> HandleAsync(
        ConfirmPickupCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DeliveryId == Guid.Empty) throw new ConfirmPickupRejectedException("Delivery is required.", ApplicationErrorCodes.RequestInvalid);
        if (command.ActorId == Guid.Empty) throw new ConfirmPickupRejectedException("Actor is required.", ApplicationErrorCodes.RequestInvalid);
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) throw new ConfirmPickupRejectedException("Idempotency key is required.", ApplicationErrorCodes.IdempotencyKeyRequired);

        var fingerprint = BuildFingerprint(command);
        var existing = await idempotencyStore.GetAsync(command.ActorId, Operation, command.IdempotencyKey, cancellationToken);
        if (existing is not null) return ValidateExisting(existing, fingerprint);

        ConfirmPickupResult? result = null;

        await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            var reserved = await idempotencyStore.ReserveAsync(
                command.ActorId, Operation, command.IdempotencyKey, fingerprint, transactionCancellationToken);

            if (reserved is not null)
            {
                result = ValidateExisting(reserved, fingerprint);
                return;
            }

            var delivery = await deliveryRepository.GetAsync(command.DeliveryId, transactionCancellationToken)
                ?? throw new ConfirmPickupRejectedException("Delivery was not found.", ApplicationErrorCodes.ResourceNotFound);

            if (delivery.Status != DeliveryStatus.Assigned)
                throw new ConfirmPickupRejectedException("Pickup can only be confirmed for an assigned Delivery.");

            if (!await authorization.CanConfirmPickupAsync(command.ActorId, delivery, transactionCancellationToken))
                throw new ConfirmPickupRejectedException("Actor is not authorized to confirm pickup.", ApplicationErrorCodes.AuthorizationForbidden);

            var driverId = delivery.DriverId
                ?? throw new ConfirmPickupRejectedException("Assigned Delivery must have a Driver.");

            try
            {
                delivery.ConfirmPickup(driverId);
            }
            catch (DomainRuleViolationException exception)
            {
                throw new ConfirmPickupRejectedException(exception.Message, ApplicationErrorCodes.DeliveryInvalidState);
            }
            await deliveryRepository.SaveAsync(delivery, transactionCancellationToken);

            result = new ConfirmPickupResult(delivery.Id, driverId);
            await idempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                new IdempotencyCompletion(
                    "Delivery",
                    result.DeliveryId,
                    System.Text.Json.JsonSerializer.Serialize(result)),
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new ApplicationFailureException(ApplicationErrorCodes.InternalUnexpected, "Pickup confirmation completed without a result.");
    }

    private static string BuildFingerprint(ConfirmPickupCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{command.DeliveryId:N}|{command.ActorId:N}")));

    private static ConfirmPickupResult ValidateExisting(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ConfirmPickupRejectedException("The idempotency key was already used with a different request.", ApplicationErrorCodes.IdempotencyKeyReused);

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new ConfirmPickupRejectedException("Idempotency record is incomplete.", ApplicationErrorCodes.IdempotencyResultUnavailable);

        var result = System.Text.Json.JsonSerializer.Deserialize<ConfirmPickupResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DriverId == Guid.Empty ||
            result.DeliveryId != existing.ResourceId.Value)
            throw new ConfirmPickupRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);

        return result;
    }
}
