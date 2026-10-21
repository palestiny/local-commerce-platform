using System.Security.Cryptography;
using LocalCommerce.Application.Idempotency;
using LocalCommerce.Application.Errors;
using System.Text;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;

namespace LocalCommerce.Application.Delivery;

public sealed record StartDeliveryCommand(Guid DeliveryId, Guid ActorId, string IdempotencyKey);
public sealed record StartDeliveryResult(Guid DeliveryId, Guid DriverId);
public sealed class StartDeliveryRejectedException : ApplicationFailureException
{
    public StartDeliveryRejectedException(string message, string code = ApplicationErrorCodes.DeliveryInvalidState) : base(code, message) { }
}

public interface IStartDeliveryDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IStartDeliveryAuthorization
{
    Task<bool> CanStartDeliveryAsync(Guid actorId, DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IStartDeliveryUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}

public sealed class StartDeliveryHandler
{
    private const string Operation = "StartDelivery";

    private readonly IStartDeliveryDeliveryRepository _deliveries;
    private readonly IStartDeliveryAuthorization _authorization;
    private readonly IIdempotencyStore _idempotency;
    private readonly IStartDeliveryUnitOfWork _unitOfWork;

    public StartDeliveryHandler(
        IStartDeliveryDeliveryRepository deliveries,
        IStartDeliveryAuthorization authorization,
        IIdempotencyStore idempotency,
        IStartDeliveryUnitOfWork unitOfWork)
    {
        _deliveries = deliveries;
        _authorization = authorization;
        _idempotency = idempotency;
        _unitOfWork = unitOfWork;
    }

    public async Task<StartDeliveryResult> HandleAsync(
        StartDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DeliveryId == Guid.Empty)
            throw new StartDeliveryRejectedException("DeliveryId is required.", ApplicationErrorCodes.RequestInvalid);

        if (command.ActorId == Guid.Empty)
            throw new StartDeliveryRejectedException("ActorId is required.", ApplicationErrorCodes.RequestInvalid);

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new StartDeliveryRejectedException("IdempotencyKey is required.", ApplicationErrorCodes.IdempotencyKeyRequired);

        var fingerprint = BuildFingerprint(command);

        var existing = await _idempotency.GetAsync(
            command.ActorId, Operation, command.IdempotencyKey, cancellationToken);

        if (existing is not null)
            return ResolveExisting(existing, fingerprint);

        StartDeliveryResult? result = null;

        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var reserved = await _idempotency.ReserveAsync(
                command.ActorId, Operation, command.IdempotencyKey, fingerprint, ct);

            if (reserved is not null)
            {
                result = ResolveExisting(reserved, fingerprint);
                return;
            }

            var delivery = await _deliveries.GetAsync(command.DeliveryId, ct);

            if (delivery is null)
                throw new StartDeliveryRejectedException("Delivery was not found.", ApplicationErrorCodes.ResourceNotFound);

            if (delivery.Status != DeliveryEntityStatus.PickedUp)
                throw new StartDeliveryRejectedException("Delivery must be PICKED_UP.", ApplicationErrorCodes.DeliveryInvalidState);

            if (!await _authorization.CanStartDeliveryAsync(command.ActorId, delivery, ct))
                throw new StartDeliveryRejectedException("Actor is not authorized to start delivery.", ApplicationErrorCodes.AuthorizationForbidden);

            var driverId = delivery.DriverId;
            if (driverId is null || driverId == Guid.Empty)
                throw new StartDeliveryRejectedException("Picked-up Delivery must have an assigned driver.", ApplicationErrorCodes.DeliveryInvalidState);

            try
            {
                delivery.StartDelivery();
            }
            catch (DomainRuleViolationException exception)
            {
                throw new StartDeliveryRejectedException(exception.Message, ApplicationErrorCodes.DeliveryInvalidState);
            }

            await _deliveries.SaveAsync(delivery, ct);

            result = new StartDeliveryResult(delivery.Id, driverId.Value);

            await _idempotency.CompleteAsync(
                command.ActorId, Operation, command.IdempotencyKey, new IdempotencyCompletion("Delivery", result.DeliveryId, System.Text.Json.JsonSerializer.Serialize(result)), ct);
        }, cancellationToken);

        return result ?? throw new ApplicationFailureException(ApplicationErrorCodes.InternalUnexpected, "Delivery start completed without a result.");
    }

    private static StartDeliveryResult ResolveExisting(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) throw new StartDeliveryRejectedException("Idempotency key was already used with a different request.", ApplicationErrorCodes.IdempotencyKeyReused);
        if (existing.Status == IdempotencyStatus.Reserved) throw new StartDeliveryRejectedException("A persisted idempotency reservation cannot be proven to be in progress.", ApplicationErrorCodes.InternalUnexpected);
        if (existing.Status != IdempotencyStatus.Completed || !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) || existing.ResourceId is null || string.IsNullOrWhiteSpace(existing.ResultPayload)) throw new StartDeliveryRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);
        var result = System.Text.Json.JsonSerializer.Deserialize<StartDeliveryResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DriverId == Guid.Empty || result.DeliveryId != existing.ResourceId.Value) throw new StartDeliveryRejectedException("Idempotency record is invalid.", ApplicationErrorCodes.InternalUnexpected);
        return result;
    }

    private static string BuildFingerprint(StartDeliveryCommand command)
    {
        var input = $"{command.DeliveryId:N}|{command.ActorId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}
