using LocalCommerce.Domain;

namespace LocalCommerce.Domain.Delivery;

public enum DeliveryStatus
{
    Unassigned,
    Assigned,
    PickedUp,
    OutForDelivery,
    Delivered,
    Failed
}

public sealed class Delivery
{
    private Delivery(Guid orderId, Guid storeId)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        StoreId = storeId;
        Status = DeliveryStatus.Unassigned;
    }

    public Guid Id { get; }
    public Guid OrderId { get; }
    public Guid StoreId { get; }
    public DeliveryStatus Status { get; private set; }
    public Guid? DriverId { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? OutForDeliveryAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }

    public static Delivery Create(Guid orderId, Guid storeId)
    {
        if (orderId == Guid.Empty)
            throw new DomainRuleViolationException("A Delivery requires an Order.");

        if (storeId == Guid.Empty)
            throw new DomainRuleViolationException("A Delivery requires a Store.");

        return new Delivery(orderId, storeId);
    }

    public void AssignDriver(Guid driverId)
    {
        if (Status != DeliveryStatus.Unassigned)
            throw InvalidTransition(DeliveryStatus.Assigned);

        if (driverId == Guid.Empty)
            throw new DomainRuleViolationException("A Delivery requires a Driver.");

        DriverId = driverId;
        AssignedAt = DateTimeOffset.UtcNow;
        Status = DeliveryStatus.Assigned;
    }

    public void ConfirmPickup(Guid driverId)
    {
        if (Status != DeliveryStatus.Assigned)
            throw InvalidTransition(DeliveryStatus.PickedUp);

        if (DriverId != driverId)
            throw new DomainRuleViolationException("Only the assigned Driver can confirm pickup.");

        PickedUpAt = DateTimeOffset.UtcNow;
        Status = DeliveryStatus.PickedUp;
    }

    public void StartDelivery()
    {
        if (Status != DeliveryStatus.PickedUp)
            throw InvalidTransition(DeliveryStatus.OutForDelivery);

        OutForDeliveryAt = DateTimeOffset.UtcNow;
        Status = DeliveryStatus.OutForDelivery;
    }

    public void Complete()
    {
        if (Status != DeliveryStatus.OutForDelivery)
            throw InvalidTransition(DeliveryStatus.Delivered);

        DeliveredAt = DateTimeOffset.UtcNow;
        Status = DeliveryStatus.Delivered;
    }

    public void Fail(string failureCode, string failureReason)
    {
        if (Status is DeliveryStatus.Delivered or DeliveryStatus.Failed)
            throw InvalidTransition(DeliveryStatus.Failed);

        if (string.IsNullOrWhiteSpace(failureCode))
            throw new DomainRuleViolationException("A Delivery failure requires a failure code.");

        if (string.IsNullOrWhiteSpace(failureReason))
            throw new DomainRuleViolationException("A Delivery failure requires a failure reason.");

        FailureCode = failureCode;
        FailureReason = failureReason;
        FailedAt = DateTimeOffset.UtcNow;
        Status = DeliveryStatus.Failed;
    }

    private static DomainRuleViolationException InvalidTransition(DeliveryStatus next) =>
        new($"Invalid Delivery transition to {next}.");
}
