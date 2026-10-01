using DeliveryEntity = LocalCommerce.Domain.Delivery.Delivery;
using DriverEntity = LocalCommerce.Domain.Delivery.Driver;

namespace LocalCommerce.Application.Delivery;

public sealed record AssignDriverCommand(
    Guid DeliveryId,
    Guid DriverId,
    Guid ActorId,
    string IdempotencyKey);

public sealed record AssignDriverResult(
    Guid DeliveryId,
    Guid DriverId);

public sealed record DriverAssignmentIdempotencyRecord(
    string Key,
    string Fingerprint,
    AssignDriverResult Result);

public sealed class AssignDriverRejectedException : Exception
{
    public AssignDriverRejectedException(string message) : base(message) { }
}

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
    Task<bool> CanAssignAsync(
        Guid actorId,
        DeliveryEntity delivery,
        CancellationToken cancellationToken);
}

public interface IAssignDriverIdempotencyStore
{
    Task<DriverAssignmentIdempotencyRecord?> GetAsync(
        Guid actorId,
        string operation,
        string key,
        CancellationToken cancellationToken);

    Task<DriverAssignmentIdempotencyRecord?> ReserveAsync(
        Guid actorId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid actorId,
        string operation,
        string key,
        AssignDriverResult result,
        CancellationToken cancellationToken);
}

public interface IAssignDriverUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public sealed class AssignDriverHandler
{
    private const string Operation = "AssignDriver";

    public AssignDriverHandler(
        IAssignDriverDeliveryRepository deliveryRepository,
        IDriverRepository driverRepository,
        IAssignDriverAuthorization authorization,
        IAssignDriverIdempotencyStore idempotencyStore,
        IAssignDriverUnitOfWork unitOfWork)
    {
        DeliveryRepository = deliveryRepository;
        DriverRepository = driverRepository;
        Authorization = authorization;
        IdempotencyStore = idempotencyStore;
        UnitOfWork = unitOfWork;
    }

    private IAssignDriverDeliveryRepository DeliveryRepository { get; }
    private IDriverRepository DriverRepository { get; }
    private IAssignDriverAuthorization Authorization { get; }
    private IAssignDriverIdempotencyStore IdempotencyStore { get; }
    private IAssignDriverUnitOfWork UnitOfWork { get; }

    public async Task<AssignDriverResult> HandleAsync(
        AssignDriverCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.DeliveryId == Guid.Empty)
            throw new AssignDriverRejectedException("Delivery is required.");

        if (command.DriverId == Guid.Empty)
            throw new AssignDriverRejectedException("Driver is required.");

        if (command.ActorId == Guid.Empty)
            throw new AssignDriverRejectedException("Actor is required.");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new AssignDriverRejectedException("Idempotency key is required.");

        var fingerprint = BuildFingerprint(command);

        var existing = await IdempotencyStore.GetAsync(
            command.ActorId,
            Operation,
            command.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ValidateExisting(existing, fingerprint);

        AssignDriverResult? result = null;

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

            var delivery = await DeliveryRepository.GetAsync(
                command.DeliveryId,
                transactionCancellationToken);

            if (delivery is null)
                throw new AssignDriverRejectedException("Delivery was not found.");

            var driver = await DriverRepository.GetAsync(
                command.DriverId,
                transactionCancellationToken);

            if (driver is null)
                throw new AssignDriverRejectedException("Driver was not found.");

            if (!driver.IsActive)
                throw new AssignDriverRejectedException("Driver is inactive.");

            if (!await Authorization.CanAssignAsync(
                    command.ActorId,
                    delivery,
                    transactionCancellationToken))
                throw new AssignDriverRejectedException(
                    "Actor is not authorized to assign a Driver.");

            delivery.AssignDriver(driver.Id);

            await DeliveryRepository.SaveAsync(
                delivery,
                transactionCancellationToken);

            result = new AssignDriverResult(delivery.Id, driver.Id);

            await IdempotencyStore.CompleteAsync(
                command.ActorId,
                Operation,
                command.IdempotencyKey,
                result,
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new InvalidOperationException(
            "Driver assignment completed without a result.");
    }

    private static string BuildFingerprint(AssignDriverCommand command) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{command.DeliveryId:N}|{command.DriverId:N}|{command.ActorId:N}")));

    private static AssignDriverResult ValidateExisting(
        DriverAssignmentIdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new AssignDriverRejectedException(
                "The idempotency key was already used with a different request.");

        return existing.Result;
    }
}
