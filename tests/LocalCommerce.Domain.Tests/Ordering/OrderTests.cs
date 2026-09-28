using LocalCommerce.Domain.Ordering;
using Xunit;

namespace LocalCommerce.Domain.Tests.Ordering;

public sealed class OrderTests
{
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Empty_order_cannot_exist()
    {
        var act = () => Order.Create(Guid.NewGuid(), []);

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Order_must_contain_at_least_one_item()
    {
        var act = () => Order.Create(StoreId, []);

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void All_order_items_must_belong_to_the_same_store()
    {
        var items = new[]
        {
            CreateItem(StoreId),
            CreateItem(Guid.NewGuid())
        };

        var act = () => Order.Create(StoreId, items);

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Purchased_unit_price_is_immutable_after_creation()
    {
        var item = CreateItem(StoreId);
        var order = Order.Create(StoreId, [item]);

        var originalPrice = item.UnitPrice;

        item.UnitPrice = originalPrice + 10m;

        Assert.Equal(originalPrice, order.Items.Single().UnitPrice);
    }

    [Fact]
    public void Valid_commercial_transitions_are_accepted()
    {
        var order = Order.Create(StoreId, [CreateItem(StoreId)]);

        Assert.Equal(OrderStatus.PendingStoreConfirmation, order.Status);

        order.Accept();
        order.Prepare();
        order.MarkReadyForPickup();

        Assert.Equal(OrderStatus.ReadyForPickup, order.Status);
    }

    [Fact]
    public void Invalid_commercial_transition_is_rejected()
    {
        var order = Order.Create(StoreId, [CreateItem(StoreId)]);

        var act = () => order.Prepare();

        Assert.Throws<DomainRuleViolationException>(act);
    }

    [Fact]
    public void Order_cannot_transition_directly_into_delivery_states()
    {
        var order = Order.Create(StoreId, [CreateItem(StoreId)]);

        Assert.DoesNotContain(
            typeof(OrderStatus).GetEnumNames(),
            status => status is "DriverAssigned" or "PickedUp" or "OutForDelivery" or "Delivered");
    }

    [Fact]
    public void Cancellation_is_allowed_only_from_explicit_commercial_states()
    {
        var order = Order.Create(StoreId, [CreateItem(StoreId)]);

        Assert.Equal(OrderStatus.PendingStoreConfirmation, order.Status);

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    private static OrderItem CreateItem(Guid storeId) =>
        new(
            ProductId,
            storeId,
            "Whole Milk",
            "1L",
            30m,
            2,
            0m,
            60m);
}
