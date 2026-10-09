using LocalCommerce.Application.Ordering.CreateOrder;
using LocalCommerce.Domain.Ordering;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Ordering;

public sealed class EfCreateOrderUnitOfWork(CommerceDbContext db) : ICreateOrderUnitOfWork
{
    public async Task ExecuteAsync(Func<CancellationToken,Task> operation,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        try { await operation(ct); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch { await tx.RollbackAsync(ct); throw; }
    }
}

public sealed class EfOrderWriter(CommerceDbContext db) : IOrderWriter
{
    public Task AddAsync(Order order,string orderNumber,CancellationToken ct)
    {
        db.Orders.Add(new OrderEntity {
            Id=order.Id, StoreId=order.StoreId, OrderNumber=orderNumber, Status=order.Status,
            CreatedAt=DateTimeOffset.UtcNow, UpdatedAt=DateTimeOffset.UtcNow,
            Items=order.Items.Select(i=>new OrderItemEntity {
                Id=Guid.NewGuid(), OrderId=order.Id, ProductId=i.ProductId, StoreId=i.StoreId,
                ProductName=i.ProductName, VariantName=i.VariantName, UnitPrice=i.UnitPrice,
                Quantity=i.Quantity, LineDiscount=i.LineDiscount, LineTotal=i.LineTotal
            }).ToList()
        });
        return Task.CompletedTask;
    }
}

