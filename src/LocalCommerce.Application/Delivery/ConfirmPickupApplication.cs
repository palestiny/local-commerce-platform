using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;

namespace LocalCommerce.Application.Delivery;

public sealed record ConfirmPickupCommand(
    Guid DeliveryId,
    Guid ActorId,
    string IdempotencyKey);

public sealed record ConfirmPickupResult(
    Guid DeliveryId,
    Guid DriverId);

public sealed record ConfirmPickupIdempotencyRecord(
    string Key,
    string Fingerprint,
    ConfirmPickupResult Result);

public sealed class ConfirmPickupRejectedException : Exception
{
    public ConfirmPickupRejectedException(string message) : base(message) { }
}

public interface IConfirmPickupDeliveryRepository
{
    Task<DeliveryEntity?> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task SaveAsync(DeliveryEntity delivery, CancellationToken cancellationToken);
}

public interface IConfirmPickupAuthorization
{
    Task<bool> CanConfirmPickupAsync(
        Guid actorId,
        DeliveryEntity delivery,
        CancellationToken cancellationToken);
}

public interface IConfirmPickupIdempotencyStore
{
    Task<ConfirmPickupIdempotencyRecord?> GetAsync(
        Guid actorId,
        string operation,
        string key,
        CancellationToken cancellationToken);

    Task<ConfirmPickupIdempotencyRecord?> ReserveAsync(
        Guid actorId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid actorId,
        string operation,
        string key,
        ConfirmPickupResult result,
        CancellationToken cancellationToken);
}

public interface IConfirmPickupUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public sealed class ConfirmPickupHandler
{
    private const string Operation = "ConfirmPickup";

    public ConfirmPickupHandler(
        IConfirmPickupDeliveryRepository deliveryRepository,
        IConfirmPickupAuthorization authorization,
        IConfirmPickupIdempotencyStore idempotencyStore,
        IConfirmPickupUnitOfWork unitOfWork)
    {
        DeliveryRepository = deliveryRepository;
        Authorization = authorization;
        IdempotencyStore = idempotencyStore;
        UnitOfWork = unitOfWork;
    }

    private IConfirmPickupDeliveryRepository DeliveryRepository { get; }
    private IConfirmPickupAuthorization Authorization { get; }
    private IConfirmPickupIdempotencyStore IdempotencyStore { get; }
    private IConfirmPickupUnitOfWork UnitOfWork { get; }

    public async Task<ConfirmPickupResult> HandleAsync(
        ConfirmPickupCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DeliveryId == Guid.Empty)
            throw new ConfirmPickupRejectedException("Delivery is required.");

        if (command.ActorId == Guid.Empty)
            throw new ConfirmPickupRejectedException("Actor is required.");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new ConfirmPickupRejectedException("Idempotency key is required.");

        var fingerprint = BuildFingerprint(command);

        var existing = await IdempotencyStore.GetAsync(
            command.ActorId,
            Operation,
            command.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ValidateExisting(existing, fingerprint);

        var delivery = await DeliveryRepository.GetAsync(
            command.DeliveryId,
            cancellationToken);

        if (delivery is null)
            throw new ConfirmPickupRejectedException("Delivery was not found.");

        if (delivery.Status != LocalCommerce.Domain.Delivery.DeliveryStatus.Assigned)
            throw new ConfirmPickupRejectedException(
                "Pickup can only be confirmed for an assigned Delivery.");

        if (!await Authorization.CanConfirmPickupAsync(
                command.ActorId,
                delivery,
                cancellationToken))
            throw new ConfirmPickupRejectedException(
                "Actor is not authorized to confirm pickup.");

        ConfirmPickupResult? result = null;

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

            var driverId = delivery.DriverId
                ?? throw new ConfirmPickupRejectedException(
                    "Assigned Delivery must have a Driver.");

            delivery.ConfirmPickup(driverId);

            await DeliveryRepository.SaveAsync(
                delivery,
                transactionCancellationToken);

            result = new ConfirmPickupResult(delivery.Id, driverId);

            await IdempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                result,
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new InvalidOperationException(
            "Pickup confirmation completed without a result.");
    }

    private static string BuildFingerprint(ConfirmPickupCommand command) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{command.DeliveryId:N}|{command.ActorId:N}")));

    private static ConfirmPickupResult ValidateExisting(
        ConfirmPickupIdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ConfirmPickupRejectedException(
                "The idempotency key was already used with a different request.");

        return existing.Result;
    }
}
