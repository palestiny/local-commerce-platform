using LocalCommerce.Application.Idempotency;
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

    public async Task<CreateOrderResult> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty)
            throw new CreateOrderRejectedException("Customer is required.");

        if (command.CartId == Guid.Empty)
            throw new CreateOrderRejectedException("Cart is required.");

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            throw new CreateOrderRejectedException("Idempotency key is required.");

        var fingerprint = BuildFingerprint(command);

        var existing = await IdempotencyStore.GetAsync(
            command.CustomerId,
            Operation,
            command.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ValidateExisting(existing, fingerprint);

        var cart = await CartReader.GetAsync(command.CartId, cancellationToken);

        if (cart is null)
            throw new CreateOrderRejectedException("Cart was not found.");

        if (cart.CustomerId != command.CustomerId)
            throw new CreateOrderRejectedException("Cart does not belong to the customer.");

        if (!cart.IsActive)
            throw new CreateOrderRejectedException("Cart is not active.");

        if (cart.Lines.Count == 0)
            throw new CreateOrderRejectedException("Cart is empty.");

        if (cart.Lines.Any(line => line.Quantity <= 0))
            throw new CreateOrderRejectedException("Cart contains an invalid quantity.");

        var store = await StoreReader.GetAsync(cart.StoreId, cancellationToken);

        if (store is null || store.StoreId != cart.StoreId || !store.IsActive)
            throw new CreateOrderRejectedException("Store is not active.");

        var productIds = cart.Lines.Select(line => line.ProductId).Distinct().ToArray();
        var products = await ProductReader.GetAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.ProductId);

        if (productsById.Count != productIds.Length)
            throw new CreateOrderRejectedException("One or more products were not found.");

        var items = new List<OrderItem>(cart.Lines.Count);

        foreach (var line in cart.Lines)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
                throw new CreateOrderRejectedException("One or more products were not found.");

            if (product.StoreId != cart.StoreId)
                throw new CreateOrderRejectedException("Cart contains a product from another store.");

            if (!product.IsOrderable)
                throw new CreateOrderRejectedException("One or more products are not orderable.");

            if (line.VariantName != product.VariantName)
                throw new CreateOrderRejectedException("Cart contains a product variant that no longer matches.");

            var lineTotal = product.UnitPrice * line.Quantity;

            items.Add(new OrderItem(
                product.ProductId,
                product.StoreId,
                product.ProductName,
                product.VariantName,
                product.UnitPrice,
                line.Quantity,
                0m,
                lineTotal));
        }

        var order = Order.Create(cart.StoreId, items);
        var orderNumber = OrderNumberGenerator.Next();
        CreateOrderResult? result = null;

        await UnitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            var reserved = await IdempotencyStore.ReserveAsync(
                command.CustomerId,
                Operation,
                command.IdempotencyKey,
                fingerprint,
                transactionCancellationToken);

            if (reserved is not null)
            {
                result = ValidateExisting(reserved, fingerprint);
                return;
            }

            await OrderWriter.AddAsync(
                order,
                orderNumber,
                transactionCancellationToken);

            await CartCheckout.ConsumeAsync(
                cart.CartId,
                transactionCancellationToken);

            result = new CreateOrderResult(order.Id, orderNumber);

            await IdempotencyStore.CompleteAsync(
                command.CustomerId,
                Operation,
                command.IdempotencyKey,
                new IdempotencyCompletion(
                    "Order",
                    result.OrderId,
                    System.Text.Json.JsonSerializer.Serialize(result)),
                transactionCancellationToken);
        }, cancellationToken);

        return result ?? throw new InvalidOperationException("Create Order completed without a result.");
    }

    private static string BuildFingerprint(CreateOrderCommand command) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"{command.CustomerId:N}|{command.CartId:N}")));

    private static CreateOrderResult ValidateExisting(
        IdempotencyRecord existing,
        string fingerprint)
    {
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new CreateOrderRejectedException(
                "The idempotency key was already used with a different request.");

        return existing.Result;
    }
}
