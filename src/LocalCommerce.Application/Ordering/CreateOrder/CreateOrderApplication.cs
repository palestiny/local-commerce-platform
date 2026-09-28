using LocalCommerce.Domain.Ordering;

namespace LocalCommerce.Application.Ordering.CreateOrder;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    Guid CartId,
    string IdempotencyKey);

public sealed record CreateOrderResult(
    Guid OrderId,
    string OrderNumber);

public sealed record CartLine(
    Guid ProductId,
    string? VariantName,
    int Quantity);

public sealed record CartSnapshot(
    Guid CartId,
    Guid CustomerId,
    Guid StoreId,
    bool IsActive,
    IReadOnlyCollection<CartLine> Lines);

public sealed record StoreSnapshot(
    Guid StoreId,
    bool IsActive);

public sealed record ProductSnapshot(
    Guid ProductId,
    Guid StoreId,
    string ProductName,
    string? VariantName,
    decimal UnitPrice,
    bool IsOrderable);

public interface ICartReader
{
    Task<CartSnapshot?> GetAsync(Guid cartId, CancellationToken cancellationToken);
}

public interface ICartCheckout
{
    Task ConsumeAsync(Guid cartId, CancellationToken cancellationToken);
}

public interface IStoreReader
{
    Task<StoreSnapshot?> GetAsync(Guid storeId, CancellationToken cancellationToken);
}

public interface IProductReader
{
    Task<IReadOnlyCollection<ProductSnapshot>> GetAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}

public interface IOrderWriter
{
    Task AddAsync(Order order, string orderNumber, CancellationToken cancellationToken);
}

public sealed record IdempotencyRecord(
    string Key,
    string Fingerprint,
    CreateOrderResult Result);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(
        Guid customerId,
        string operation,
        string key,
        CancellationToken cancellationToken);

    Task ReserveAsync(
        Guid customerId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid customerId,
        string operation,
        string key,
        CreateOrderResult result,
        CancellationToken cancellationToken);
}

public interface ICreateOrderUnitOfWork
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}

public interface IOrderNumberGenerator
{
    string Next();
}

public sealed class CreateOrderRejectedException : Exception
{
    public CreateOrderRejectedException(string message) : base(message) { }
}

public sealed class CreateOrderHandler
{
    private const string Operation = "CreateOrder";

    public CreateOrderHandler(
        ICartReader cartReader,
        ICartCheckout cartCheckout,
        IStoreReader storeReader,
        IProductReader productReader,
        IOrderWriter orderWriter,
        IIdempotencyStore idempotencyStore,
        ICreateOrderUnitOfWork unitOfWork,
        IOrderNumberGenerator orderNumberGenerator)
    {
        CartReader = cartReader;
        CartCheckout = cartCheckout;
        StoreReader = storeReader;
        ProductReader = productReader;
        OrderWriter = orderWriter;
        IdempotencyStore = idempotencyStore;
        UnitOfWork = unitOfWork;
        OrderNumberGenerator = orderNumberGenerator;
    }

    private ICartReader CartReader { get; }
    private ICartCheckout CartCheckout { get; }
    private IStoreReader StoreReader { get; }
    private IProductReader ProductReader { get; }
    private IOrderWriter OrderWriter { get; }
    private IIdempotencyStore IdempotencyStore { get; }
    private ICreateOrderUnitOfWork UnitOfWork { get; }
    private IOrderNumberGenerator OrderNumberGenerator { get; }

    public Task<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Create Order application behavior is intentionally RED.");
    }
}
