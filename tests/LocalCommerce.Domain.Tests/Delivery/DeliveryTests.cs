using LocalCommerce.Domain.Delivery;
using Xunit;

namespace LocalCommerce.Domain.Tests.Delivery;

public sealed class DeliveryTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid DriverId = Guid.NewGuid();

    [Fact]
    public void Delivery_starts_unassigned()
    {
        var delivery = Delivery.Create(OrderId, StoreId);

        Assert.Equal(DeliveryStatus.Unassigned, delivery.Status);
        Assert.Null(delivery.DriverId);
    }

    [Fact]
    public void Delivery_can_be_assigned_to_an_active_driver()
    {
        var delivery = Delivery.Create(OrderId, StoreId);

        delivery.AssignDriver(DriverId);

        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(DriverId, delivery.DriverId);
        Assert.NotNull(delivery.AssignedAt);
    }

    [Fact]
    public void Delivery_cannot_be_assigned_twice()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);

        var act = () => delivery.AssignDriver(Guid.NewGuid());

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Only_assigned_driver_can_confirm_pickup()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);

        var act = () => delivery.ConfirmPickup(Guid.NewGuid());

        Assert.Throws<DomainRuleViolationException>(act);

        delivery.ConfirmPickup(DriverId);

        Assert.Equal(DeliveryStatus.PickedUp, delivery.Status);
        Assert.NotNull(delivery.PickedUpAt);
    }

    [Fact]
    public void Delivery_can_progress_to_delivered()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);
        delivery.ConfirmPickup(DriverId);
        delivery.StartDelivery();

        delivery.Complete();

        Assert.Equal(DeliveryStatus.Delivered, delivery.Status);
        Assert.NotNull(delivery.DeliveredAt);
    }

    [Fact]
    public void Delivery_cannot_start_delivery_before_pickup()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);

        var act = () => delivery.StartDelivery();

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Delivery_failure_is_terminal_for_the_current_attempt()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);

        delivery.Fail("DRIVER_UNAVAILABLE", "Driver became unavailable.");

        Assert.Equal(DeliveryStatus.Failed, delivery.Status);
        Assert.NotNull(delivery.FailedAt);
        Assert.Equal("DRIVER_UNAVAILABLE", delivery.FailureCode);
        Assert.Equal("Driver became unavailable.", delivery.FailureReason);

        Assert.Throws<DomainRuleViolationException>(() => delivery.StartDelivery());
        Assert.Throws<DomainRuleViolationException>(() => delivery.Complete());
    }

    [Fact]
    public void Delivered_delivery_is_terminal()
    {
        var delivery = Delivery.Create(OrderId, StoreId);
        delivery.AssignDriver(DriverId);
        delivery.ConfirmPickup(DriverId);
        delivery.StartDelivery();
        delivery.Complete();

        Assert.Throws<DomainRuleViolationException>(() => delivery.Fail("LATE", "Too late."));
    }

    [Fact]
    public void Delivery_requires_order_and_store()
    {
        Assert.Throws<DomainRuleViolationException>(() => Delivery.Create(Guid.Empty, StoreId));
        Assert.Throws<DomainRuleViolationException>(() => Delivery.Create(OrderId, Guid.Empty));
    }
}
