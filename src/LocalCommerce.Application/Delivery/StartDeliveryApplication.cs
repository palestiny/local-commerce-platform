using System.Security.Cryptography;
using LocalCommerce.Application.Idempotency;
using System.Text;
using LocalCommerce.Domain;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DeliveryEntityStatus = LocalCommerce.Domain.Delivery.DeliveryStatus;

namespace LocalCommerce.Application.Delivery;

public sealed record StartDeliveryCommand(Guid DeliveryId, Guid ActorId, string IdempotencyKey);
public sealed record StartDeliveryResult(Guid DeliveryId, Guid DriverId);
public sealed class StartDeliveryRejectedException : Exception
{
    public StartDeliveryRejectedException(string message) : base(message) { }
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

public interface IIdempotencyStore
{
    Task<StartDeliveryIdempotencyRecord?> GetAsync(Guid actorId, string operation, string key, CancellationToken cancellationToken);
    Task<StartDeliveryIdempotencyRecord?> ReserveAsync(Guid actorId, string operation, string key, string fingerprint, CancellationToken cancellationToken);
    Task CompleteAsync(Guid actorId, string operation, string key, StartDeliveryResult result, CancellationToken cancellationToken);
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
            throw new StartDeliveryRejectedException("DeliveryId is required.");

        if (command.ActorId == Guid.Empty)
            throw new StartDeliveryRejectedException("ActorId is required.");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new StartDeliveryRejectedException("IdempotencyKey is required.");

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
                throw new StartDeliveryRejectedException("Delivery was not found.");

            if (delivery.Status != DeliveryEntityStatus.PickedUp)
                throw new StartDeliveryRejectedException("Delivery must be PICKED_UP.");

            if (!await _authorization.CanStartDeliveryAsync(command.ActorId, delivery, ct))
                throw new StartDeliveryRejectedException("Actor is not authorized to start delivery.");

            var driverId = delivery.DriverId;
            if (driverId is null || driverId == Guid.Empty)
                throw new StartDeliveryRejectedException("Picked-up Delivery must have an assigned driver.");

            delivery.StartDelivery();

            await _deliveries.SaveAsync(delivery, ct);

            result = new StartDeliveryResult(delivery.Id, driverId.Value);

            await _idempotency.CompleteAsync(
                command.ActorId, Operation, command.IdempotencyKey, new IdempotencyCompletion("Delivery", result.DeliveryId, System.Text.Json.JsonSerializer.Serialize(result)), ct);
        }, cancellationToken);

        return result!;
    }

    private static StartDeliveryResult ResolveExisting(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) throw new StartDeliveryRejectedException("Idempotency key was already used with a different request.");
        if (existing.Status != IdempotencyStatus.Completed || !string.Equals(existing.ResourceType, "Delivery", StringComparison.Ordinal) || existing.ResourceId is null || string.IsNullOrWhiteSpace(existing.ResultPayload)) throw new StartDeliveryRejectedException("Idempotency record is incomplete.");
        var result = System.Text.Json.JsonSerializer.Deserialize<StartDeliveryResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DriverId == Guid.Empty || result.DeliveryId != existing.ResourceId.Value) throw new StartDeliveryRejectedException("Idempotency record is invalid.");
        return result;
    }

    private static string BuildFingerprint(StartDeliveryCommand command)
    {
        var input = $"{command.DeliveryId:N}|{command.ActorId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}
