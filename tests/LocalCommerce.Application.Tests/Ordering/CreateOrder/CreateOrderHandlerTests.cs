using LocalCommerce.Application.Ordering.CreateOrder;
using Xunit;

namespace LocalCommerce.Application.Tests.Ordering.CreateOrder;

public sealed class CreateOrderHandlerTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid CartId = Guid.NewGuid();
    private static readonly Guid StoreId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public async Task Customer_can_create_an_order_from_their_active_cart()
    {
        var fixture = Fixture.WithActiveCart();

        var result = await fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        Assert.NotEqual(Guid.Empty, result.OrderId);
        Assert.Equal("ORD-000001", result.OrderNumber);
        Assert.Equal(CartId, fixture.CartCheckout.ConsumedCartId);
        Assert.Single(fixture.OrderWriter.Orders);
    }

    [Fact]
    public async Task Empty_cart_is_rejected()
    {
        var fixture = Fixture.WithCart(lines: []);

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(act);
    }

    [Fact]
    public async Task Another_customers_cart_is_rejected()
    {
        var fixture = Fixture.WithActiveCart(customerId: Guid.NewGuid());

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(act);
    }

    [Fact]
    public async Task Inactive_store_is_rejected()
    {
        var fixture = Fixture.WithActiveCart(storeActive: false);

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(act);
    }

    [Fact]
    public async Task Unavailable_product_is_rejected()
    {
        var fixture = Fixture.WithActiveCart(productOrderable: false);

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(act);
    }

    [Fact]
    public async Task Server_side_price_is_snapshotted_into_the_order()
    {
        var fixture = Fixture.WithActiveCart(productPrice: 37.50m);

        await fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        var orderItem = fixture.OrderWriter.Orders.Single().Items.Single();

        Assert.Equal(37.50m, orderItem.UnitPrice);
        Assert.Equal(75.00m, orderItem.LineTotal);
    }

    [Fact]
    public async Task Order_uses_exactly_the_cart_store()
    {
        var fixture = Fixture.WithActiveCart();

        await fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        Assert.Equal(StoreId, fixture.OrderWriter.Orders.Single().StoreId);
    }

    [Fact]
    public async Task Same_idempotency_key_and_same_fingerprint_returns_original_result()
    {
        var fixture = Fixture.WithActiveCart();
        var command = new CreateOrderCommand(CustomerId, CartId, "idem-1");

        var first = await fixture.Handler.HandleAsync(command);
        var second = await fixture.Handler.HandleAsync(command);

        Assert.Equal(first, second);
        Assert.Single(fixture.OrderWriter.Orders);
    }

    [Fact]
    public async Task Same_idempotency_key_with_different_fingerprint_is_rejected()
    {
        var fixture = Fixture.WithActiveCart();
        await fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, Guid.NewGuid(), "idem-1"));

        await Assert.ThrowsAsync<CreateOrderRejectedException>(act);
    }

    [Fact]
    public async Task Failed_creation_does_not_consume_the_cart()
    {
        var fixture = Fixture.WithActiveCart();
        fixture.UnitOfWork.Fail = true;

        var act = () => fixture.Handler.HandleAsync(
            new CreateOrderCommand(CustomerId, CartId, "idem-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Null(fixture.CartCheckout.ConsumedCartId);
    }

    private sealed class Fixture
    {
        private Fixture(
            CartSnapshot cart,
            StoreSnapshot store,
            ProductSnapshot product)
        {
            CartReader = new FakeCartReader(cart);
            CartCheckout = new FakeCartCheckout();
            StoreReader = new FakeStoreReader(store);
            ProductReader = new FakeProductReader(product);
            OrderWriter = new FakeOrderWriter();
            IdempotencyStore = new FakeIdempotencyStore();
            UnitOfWork = new FakeUnitOfWork();
            OrderNumberGenerator = new FakeOrderNumberGenerator();

            Handler = new CreateOrderHandler(
                CartReader,
                CartCheckout,
                StoreReader,
                ProductReader,
                OrderWriter,
                IdempotencyStore,
                UnitOfWork,
                OrderNumberGenerator);
        }

        public CreateOrderHandler Handler { get; }
        public FakeCartCheckout CartCheckout { get; }
        public FakeOrderWriter OrderWriter { get; }
        public FakeIdempotencyStore IdempotencyStore { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public FakeOrderNumberGenerator OrderNumberGenerator { get; }

        public static Fixture WithActiveCart(
            Guid? customerId = null,
            bool storeActive = true,
            bool productOrderable = true,
            decimal productPrice = 30m) =>
            WithCart(
                customerId,
                storeActive,
                productOrderable,
                productPrice,
                [new CartLine(ProductId, "1L", 2)]);

        public static Fixture WithCart(
            Guid? customerId = null,
            bool storeActive = true,
            bool productOrderable = true,
            decimal productPrice = 30m,
            IReadOnlyCollection<CartLine>? lines = null) =>
            new(
                new CartSnapshot(
                    CartId,
                    customerId ?? CustomerId,
                    StoreId,
                    true,
                    lines ?? [new CartLine(ProductId, "1L", 2)]),
                new StoreSnapshot(StoreId, storeActive),
                new ProductSnapshot(
                    ProductId,
                    StoreId,
                    "Whole Milk",
                    "1L",
                    productPrice,
                    productOrderable));

        private sealed class FakeCartReader(CartSnapshot? cart) : ICartReader
        {
            public Task<CartSnapshot?> GetAsync(Guid cartId, CancellationToken cancellationToken) =>
                Task.FromResult(cart);
        }

        private sealed class FakeCartCheckout : ICartCheckout
        {
            public Guid? ConsumedCartId { get; private set; }

            public Task ConsumeAsync(Guid cartId, CancellationToken cancellationToken)
            {
                ConsumedCartId = cartId;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeStoreReader(StoreSnapshot store) : IStoreReader
        {
            public Task<StoreSnapshot?> GetAsync(Guid storeId, CancellationToken cancellationToken) =>
                Task.FromResult<StoreSnapshot?>(store);
        }

        private sealed class FakeProductReader(ProductSnapshot product) : IProductReader
        {
            public Task<IReadOnlyCollection<ProductSnapshot>> GetAsync(
                IReadOnlyCollection<Guid> productIds,
                CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyCollection<ProductSnapshot>>([product]);
        }

        private sealed class FakeOrderWriter : IOrderWriter
        {
            public List<Order> Orders { get; } = [];

            public Task AddAsync(
                LocalCommerce.Domain.Ordering.Order order,
                string orderNumber,
                CancellationToken cancellationToken)
            {
                Orders.Add(order);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeIdempotencyStore : IIdempotencyStore
        {
            private readonly Dictionary<string, IdempotencyRecord> records = [];

            public Task<IdempotencyRecord?> GetAsync(
                Guid customerId,
                string operation,
                string key,
                CancellationToken cancellationToken) =>
                Task.FromResult(
                    records.TryGetValue(key, out var record)
                        ? record
                        : null);

            public Task ReserveAsync(
                Guid customerId,
                string operation,
                string key,
                string fingerprint,
                CancellationToken cancellationToken) =>
                Task.CompletedTask;

            public Task CompleteAsync(
                Guid customerId,
                string operation,
                string key,
                CreateOrderResult result,
                CancellationToken cancellationToken)
            {
                records[key] = new IdempotencyRecord(key, "fingerprint", result);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeUnitOfWork : ICreateOrderUnitOfWork
        {
            public bool Fail { get; set; }

            public async Task ExecuteAsync(
                Func<CancellationToken, Task> operation,
                CancellationToken cancellationToken)
            {
                if (Fail)
                    throw new InvalidOperationException("Persistence failure.");

                await operation(cancellationToken);
            }
        }

        private sealed class FakeOrderNumberGenerator : IOrderNumberGenerator
        {
            public string Next() => "ORD-000001";
        }
    }
}
