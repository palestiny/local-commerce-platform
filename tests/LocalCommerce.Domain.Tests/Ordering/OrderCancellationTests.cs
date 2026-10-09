using LocalCommerce.Domain.Ordering;
using Xunit;

namespace LocalCommerce.Domain.Tests.Ordering;

public sealed class OrderCancellationTests
{
    [Fact]
    public void Ready_for_pickup_order_can_be_cancelled_before_pickup()
    {
        var storeId=Guid.NewGuid();
        var productId=Guid.NewGuid();
        var item=new OrderItem(productId,storeId,"Milk",null,10m,1,0m,10m);
        var order=Order.Create(storeId,new[]{item});
        order.SubmitForStoreConfirmation();
        order.Accept();
        order.Prepare();
        order.MarkReadyForPickup();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled,order.Status);
    }
}