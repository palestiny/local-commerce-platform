using LocalCommerce.Application.Delivery;
using LocalCommerce.Application.Ordering;
using LocalCommerce.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Persistence;

public sealed class EfOrderForDeliveryRepository(CommerceDbContext db) :
    IOrderForDeliveryRepository,
    IOrderCancellationOrderRepository
{
    public Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken) =>
        LoadAsync(orderId, cancellationToken);

    public Task SaveAsync(Order order, CancellationToken cancellationToken) =>
        SaveInternalAsync(order, cancellationToken);

    private async Task<Order?> LoadAsync(Guid orderId, CancellationToken cancellationToken)
    {
        IQueryable<OrderEntity> query;

        if (db.Database.CurrentTransaction is not null)
        {
            query = db.Orders.FromSqlInterpolated($"""
                SELECT *
                FROM "Orders"
                WHERE "Id" = {orderId}
                FOR UPDATE
                """);
        }
        else
        {
            query = db.Orders.AsNoTracking().Where(x => x.Id == orderId);
        }

        var entity = await query.SingleOrDefaultAsync(cancellationToken);
        if (entity is null)
            return null;

        var items = await db.OrderItems.AsNoTracking()
            .Where(x => x.OrderId == entity.Id)
            .OrderBy(x => x.Id)
            .Select(x => new OrderItem(
                x.ProductId,
                x.StoreId,
                x.ProductName,
                x.VariantName,
                x.UnitPrice,
                x.Quantity,
                x.LineDiscount,
                x.LineTotal))
            .ToListAsync(cancellationToken);

        return Order.Restore(entity.Id, entity.StoreId, items, entity.Status);
    }

    private async Task SaveInternalAsync(Order order, CancellationToken cancellationToken)
    {
        var entity = await db.Orders.SingleAsync(x => x.Id == order.Id, cancellationToken);
        entity.Status = order.Status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfOrderDeliveryUnitOfWork(CommerceDbContext db) :
    IReadyForDeliveryUnitOfWork,
    ICancelOrderUnitOfWork
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
