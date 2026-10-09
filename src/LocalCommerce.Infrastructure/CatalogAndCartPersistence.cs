using LocalCommerce.Application.Ordering.CreateOrder;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure;

public sealed class EfCartReader(CommerceDbContext db) : ICartReader
{
    public async Task<CartSnapshot?> GetAsync(Guid cartId,CancellationToken ct)
    {
        var c=await db.Carts.AsNoTracking().Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==cartId,ct);
        return c is null ? null : new CartSnapshot(c.Id,c.CustomerId,c.StoreId,c.IsActive,
            c.Lines.Select(x=>new CartLine(x.ProductId,x.VariantName,x.Quantity)).ToArray());
    }
}

public sealed class EfCartCheckout(CommerceDbContext db) : ICartCheckout
{
    public async Task ConsumeAsync(Guid cartId,CancellationToken ct)
    {
        var affected=await db.Carts.Where(x=>x.Id==cartId && x.IsActive)
            .ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false),ct);
        if(affected!=1) throw new CreateOrderRejectedException("Cart was already consumed by another order attempt.");
    }
}

public sealed class EfStoreReader(CommerceDbContext db) : IStoreReader
{
    public async Task<StoreSnapshot?> GetAsync(Guid storeId,CancellationToken ct)
    {
        var s=await db.Stores.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==storeId,ct);
        return s is null ? null : new StoreSnapshot(s.Id,s.IsActive);
    }
}

public sealed class EfProductReader(CommerceDbContext db) : IProductReader
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> GetAsync(IReadOnlyCollection<Guid> ids,CancellationToken ct)
        => await db.Products.AsNoTracking().Where(x=>ids.Contains(x.Id))
            .Select(x=>new ProductSnapshot(x.Id,x.StoreId,x.Name,x.VariantName,x.UnitPrice,x.IsOrderable))
            .ToArrayAsync(ct);
}

public sealed class SequentialOrderNumberGenerator : IOrderNumberGenerator
{
    public string Next() => $"ORD-{Guid.NewGuid():N}";
}
