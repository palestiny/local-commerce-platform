using DeliveryEntity=LocalCommerce.Domain.Delivery.Delivery;
using DeliveryStatus=LocalCommerce.Domain.Delivery.DeliveryStatus;
using LocalCommerce.Domain;
using Xunit;

namespace LocalCommerce.Domain.Tests.Delivery;

public sealed class DeliveryCancellationTests
{
    [Fact]
    public void Active_delivery_before_pickup_can_be_cancelled()
    {
        var delivery=DeliveryEntity.Create(Guid.NewGuid(),Guid.NewGuid());
        delivery.AssignDriver(Guid.NewGuid());

        delivery.CancelBeforePickup();

        Assert.Equal(DeliveryStatus.Cancelled,delivery.Status);
    }

    [Fact]
    public void Pickup_started_cannot_be_cancelled_by_normal_cancellation()
    {
        var driver=Guid.NewGuid();
        var delivery=DeliveryEntity.Create(Guid.NewGuid(),Guid.NewGuid());
        delivery.AssignDriver(driver);
        delivery.ConfirmPickup(driver);

        Action act=()=>delivery.CancelBeforePickup();
        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Delivered_delivery_cannot_be_cancelled()
    {
        var driver=Guid.NewGuid();
        var delivery=DeliveryEntity.Create(Guid.NewGuid(),Guid.NewGuid());
        delivery.AssignDriver(driver);
        delivery.ConfirmPickup(driver);
        delivery.StartDelivery();
        delivery.Complete();

        Assert.Throws<DomainRuleViolationException>(()=>delivery.CancelBeforePickup());
    }
}