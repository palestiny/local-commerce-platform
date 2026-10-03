using System.Security.Cryptography;
using System.Text;
using LocalCommerce.Application.Idempotency;
using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DriverEntity = LocalCommerce.Domain.Delivery.Driver;

namespace LocalCommerce.Application.Delivery;

public sealed record AssignDriverCommand(Guid DeliveryId, Guid DriverId, Guid ActorId, string IdempotencyKey);
public sealed record AssignDriverResult(Guid DeliveryId, Guid DriverId);

public sealed class AssignDriverRejectedException(string message) : Exception(message);

public interface IAssignDriverDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IDriverRepository
{
    Task<DriverEntity?> GetAsync(Guid driverId, CancellationToken cancellationToken);
}

public interface IAssignDriverAuthorization
{
    Task<bool> CanAssignAsync(Guid actorId, DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IAssignDriverUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}

public sealed class AssignDriverHandler(
    IAssignDriverDeliveryRepository deliveryRepository,
    IDriverRepository driverRepository,
    IAssignDriverAuthorization authorization,
    IIdempotencyStore idempotencyStore,
    IAssignDriverUnitOfWork unitOfWork)
{
    private const string Operation = "AssignDriver";

    public async Task<AssignDriverResult> HandleAsync(
        AssignDriverCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DeliveryId == Guid.Empty) throw new AssignDriverRejectedException("Delivery is required.");
        if (command.DriverId == Guid.Empty) throw new AssignDriverRejectedException("Driver is required.");
        if (command.ActorId == Guid.Empty) throw new AssignDriverRejectedException("Actor is required.");
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)) throw new AssignDriverRejectedException("Idempotency key is required.");

        var fingerprint = BuildFingerprint(command);
        var existing = await idempotencyStore.GetAsync(command.ActorId, Operation, command.IdempotencyKey, cancellationToken);
        if (existing is not null) return ValidateExisting(existing, fingerprint);

        AssignDriverResult? result = null;

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
                ?? throw new AssignDriverRejectedException("Delivery was not found.");
            var driver = await driverRepository.GetAsync(command.DriverId, transactionCancellationToken)
                ?? throw new AssignDriverRejectedException("Driver was not found.");

            if (!driver.IsActive) throw new AssignDriverRejectedException("Driver is inactive.");
            if (!await authorization.CanAssignAsync(command.ActorId, delivery, transactionCancellationToken))
                throw new AssignDriverRejectedException("Actor is not authorized to assign a Driver.");

            delivery.AssignDriver(driver.Id);
            await deliveryRepository.SaveAsync(delivery, transactionCancellationToken);

            result = new AssignDriverResult(delivery.Id, driver.Id);
            await idempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                new IdempotencyCompletion(
                    "DeliveryAssignment",
                    result.DeliveryId,
                    System.Text.Json.JsonSerializer.Serialize(result)),
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new InvalidOperationException("Driver assignment completed without a result.");
    }

    private static string BuildFingerprint(AssignDriverCommand command) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{command.DeliveryId:N}|{command.DriverId:N}|{command.ActorId:N}")));

    private static AssignDriverResult ValidateExisting(IdempotencyRecord existing, string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new AssignDriverRejectedException("The idempotency key was already used with a different request.");

        if (existing.Status != IdempotencyStatus.Completed ||
            !string.Equals(existing.ResourceType, "DeliveryAssignment", StringComparison.Ordinal) ||
            existing.ResourceId is null ||
            string.IsNullOrWhiteSpace(existing.ResultPayload))
            throw new AssignDriverRejectedException("Idempotency record is incomplete.");

        var result = System.Text.Json.JsonSerializer.Deserialize<AssignDriverResult>(existing.ResultPayload);
        if (result is null || result.DeliveryId == Guid.Empty || result.DriverId == Guid.Empty ||
            result.DeliveryId != existing.ResourceId.Value)
            throw new AssignDriverRejectedException("Idempotency record is invalid.");

        return result;
    }
}
