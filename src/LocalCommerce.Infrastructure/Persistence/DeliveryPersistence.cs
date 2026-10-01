using LocalCommerce.Domain.Delivery;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Persistence;

public sealed class DeliveryEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid StoreId { get; set; }
    public DeliveryStatus Status { get; set; }
    public Guid? DriverId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? OutForDeliveryAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class DeliveryStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid DeliveryId { get; set; }
    public string ActorType { get; set; } = null!;
    public Guid? ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DeliveryStatus PreviousStatus { get; set; }
    public DeliveryStatus NewStatus { get; set; }
    public string CommandName { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public string? FailureCode { get; set; }
    public string? FailureReason { get; set; }
}

public sealed record DeliveryStatusHistoryEntry(
    Guid DeliveryId,
    string ActorType,
    Guid? ActorId,
    DateTimeOffset OccurredAt,
    DeliveryStatus PreviousStatus,
    DeliveryStatus NewStatus,
    string CommandName,
    string CorrelationId,
    string? FailureCode,
    string? FailureReason);

public interface IDeliveryRepository
{
    Task AddAsync(Delivery delivery, CancellationToken cancellationToken);
    Task SaveAsync(Delivery delivery, CancellationToken cancellationToken);
    Task<Delivery?> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
    Task<Delivery?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}

public interface IDeliveryStatusHistoryRepository
{
    Task AppendAsync(DeliveryStatusHistoryEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeliveryStatusHistoryEntry>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken);
}

public sealed class EfDeliveryRepository(CommerceDbContext db) : IDeliveryRepository
{
    public async Task AddAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        db.Deliveries.Add(Map(delivery, now, now));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        var entity = await db.Deliveries.SingleAsync(x => x.Id == delivery.Id, cancellationToken);
        entity.OrderId = delivery.OrderId;
        entity.StoreId = delivery.StoreId;
        entity.Status = delivery.Status;
        entity.DriverId = delivery.DriverId;
        entity.AssignedAt = delivery.AssignedAt;
        entity.PickedUpAt = delivery.PickedUpAt;
        entity.OutForDeliveryAt = delivery.OutForDeliveryAt;
        entity.DeliveredAt = delivery.DeliveredAt;
        entity.FailedAt = delivery.FailedAt;
        entity.FailureCode = delivery.FailureCode;
        entity.FailureReason = delivery.FailureReason;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Delivery?> GetAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var entity = await db.Deliveries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Delivery?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var entity = await db.Deliveries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderId == orderId && IsActive(x.Status), cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    private static bool IsActive(DeliveryStatus status) =>
        status is DeliveryStatus.Unassigned or DeliveryStatus.Assigned or DeliveryStatus.PickedUp or DeliveryStatus.OutForDelivery;

    private static DeliveryEntity Map(Delivery delivery, DateTimeOffset createdAt, DateTimeOffset updatedAt) => new()
    {
        Id = delivery.Id,
        OrderId = delivery.OrderId,
        StoreId = delivery.StoreId,
        Status = delivery.Status,
        DriverId = delivery.DriverId,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt,
        AssignedAt = delivery.AssignedAt,
        PickedUpAt = delivery.PickedUpAt,
        OutForDeliveryAt = delivery.OutForDeliveryAt,
        DeliveredAt = delivery.DeliveredAt,
        FailedAt = delivery.FailedAt,
        FailureCode = delivery.FailureCode,
        FailureReason = delivery.FailureReason
    };

    private static Delivery ToDomain(DeliveryEntity entity) =>
        Delivery.Restore(entity.Id, entity.OrderId, entity.StoreId, entity.Status, entity.DriverId,
            entity.AssignedAt, entity.PickedUpAt, entity.OutForDeliveryAt, entity.DeliveredAt,
            entity.FailedAt, entity.FailureCode, entity.FailureReason);
}

public sealed class EfDeliveryStatusHistoryRepository(CommerceDbContext db) : IDeliveryStatusHistoryRepository
{
    public async Task AppendAsync(DeliveryStatusHistoryEntry entry, CancellationToken cancellationToken)
    {
        db.DeliveryStatusHistory.Add(new DeliveryStatusHistoryEntity
        {
            Id = Guid.NewGuid(),
            DeliveryId = entry.DeliveryId,
            ActorType = entry.ActorType,
            ActorId = entry.ActorId,
            OccurredAt = entry.OccurredAt,
            PreviousStatus = entry.PreviousStatus,
            NewStatus = entry.NewStatus,
            CommandName = entry.CommandName,
            CorrelationId = entry.CorrelationId,
            FailureCode = entry.FailureCode,
            FailureReason = entry.FailureReason
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryStatusHistoryEntry>> GetByDeliveryIdAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        await db.DeliveryStatusHistory.AsNoTracking()
            .Where(x => x.DeliveryId == deliveryId)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .Select(x => new DeliveryStatusHistoryEntry(
                x.DeliveryId, x.ActorType, x.ActorId, x.OccurredAt, x.PreviousStatus,
                x.NewStatus, x.CommandName, x.CorrelationId, x.FailureCode, x.FailureReason))
            .ToListAsync(cancellationToken);
}
