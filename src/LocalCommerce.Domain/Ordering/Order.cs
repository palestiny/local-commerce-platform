namespace LocalCommerce.Domain.Ordering;

public enum OrderStatus
{
    Created,
    PendingStoreConfirmation,
    Accepted,
    Preparing,
    ReadyForPickup,
    Rejected,
    Cancelled
}

public sealed class OrderItem
{
    public OrderItem(
        Guid productId,
        Guid storeId,
        string productName,
        string? variantName,
        decimal unitPrice,
        int quantity,
        decimal lineDiscount,
        decimal lineTotal)
    {
        ProductId = productId;
        StoreId = storeId;
        ProductName = productName;
        VariantName = variantName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineDiscount = lineDiscount;
        LineTotal = lineTotal;
    }

    public Guid ProductId { get; }
    public Guid StoreId { get; }
    public string ProductName { get; }
    public string? VariantName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
    public decimal LineDiscount { get; }
    public decimal LineTotal { get; }
}

public sealed class Order
{
    private Order(Guid storeId, IReadOnlyCollection<OrderItem> items)
    {
        Id = Guid.NewGuid();
        StoreId = storeId;
        Items = items;
        Status = OrderStatus.Created;
    }

    public Guid Id { get; }
    public Guid StoreId { get; }
    public IReadOnlyCollection<OrderItem> Items { get; }
    public OrderStatus Status { get; private set; }

    public static Order Create(Guid storeId, IReadOnlyCollection<OrderItem> items)
    {
        if (items is null || items.Count == 0)
            throw new DomainRuleViolationException("An Order must contain at least one item.");

        if (items.Any(item => item.StoreId != storeId))
            throw new DomainRuleViolationException("All Order items must belong to the Order store.");

        var snapshots = items
            .Select(item => new OrderItem(
                item.ProductId,
                item.StoreId,
                item.ProductName,
                item.VariantName,
                item.UnitPrice,
                item.Quantity,
                item.LineDiscount,
                item.LineTotal))
            .ToArray();

        return new Order(storeId, snapshots);
    }

    public void SubmitForStoreConfirmation() =>
        Transition(OrderStatus.Created, OrderStatus.PendingStoreConfirmation);

    public void Accept() =>
        Transition(OrderStatus.PendingStoreConfirmation, OrderStatus.Accepted);

    public void Prepare() =>
        Transition(OrderStatus.Accepted, OrderStatus.Preparing);

    public void MarkReadyForPickup() =>
        Transition(OrderStatus.Preparing, OrderStatus.ReadyForPickup);

    public void Cancel() =>
        Transition(
            OrderStatus.Created,
            OrderStatus.Cancelled,
            OrderStatus.PendingStoreConfirmation,
            OrderStatus.Accepted,
            OrderStatus.Preparing);

    private void Transition(OrderStatus expectedCurrent, OrderStatus next)
    {
        if (Status != expectedCurrent)
            throw new DomainRuleViolationException(
                $"Invalid Order transition: {Status} -> {next}.");

        Status = next;
    }

    private void Transition(
        OrderStatus expectedCurrent1,
        OrderStatus next,
        params OrderStatus[] additionalAllowedCurrentStates)
    {
        if (Status != expectedCurrent1 && !additionalAllowedCurrentStates.Contains(Status))
            throw new DomainRuleViolationException(
                $"Invalid Order transition: {Status} -> {next}.");

        Status = next;
    }
}

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
}
