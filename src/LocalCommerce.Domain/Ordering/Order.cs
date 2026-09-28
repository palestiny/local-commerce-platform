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
    public decimal UnitPrice { get; set; }
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
        => throw new NotImplementedException();

    public void Accept() => throw new NotImplementedException();
    public void Prepare() => throw new NotImplementedException();
    public void MarkReadyForPickup() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
}

public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
}
