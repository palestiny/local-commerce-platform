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

public sealed class EfIdempotencyStore(CommerceDbContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> GetAsync(Guid customerId,string operation,string key,CancellationToken ct)
        => Read(customerId,operation,key,ct);

    public async Task<IdempotencyRecord?> ReserveAsync(Guid customerId,string operation,string key,string fingerprint,CancellationToken ct)
    {
        var affected=await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "IdempotencyRecords"
            ("Id","CustomerId","Operation","IdempotencyKey","RequestFingerprint","CreatedAt")
            VALUES ({Guid.NewGuid()},{customerId},{operation},{key},{fingerprint},{DateTimeOffset.UtcNow})
            ON CONFLICT ("CustomerId","Operation","IdempotencyKey") DO NOTHING
            """,ct);
        if(affected==1) return null;
        return await Read(customerId,operation,key,ct)
            ?? throw new InvalidOperationException("Idempotency record exists but is not completed.");
    }

    public async Task CompleteAsync(Guid customerId,string operation,string key,CreateOrderResult result,CancellationToken ct)
    {
        var affected=await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "IdempotencyRecords"
            SET "OrderId"={result.OrderId},"OrderNumber"={result.OrderNumber},"CompletedAt"={DateTimeOffset.UtcNow}
            WHERE "CustomerId"={customerId} AND "Operation"={operation} AND "IdempotencyKey"={key} AND "CompletedAt" IS NULL
            """,ct);
        if(affected!=1) throw new InvalidOperationException("Idempotency completion affected an unexpected number of records.");
    }

    private async Task<IdempotencyRecord?> Read(Guid customerId,string operation,string key,CancellationToken ct)
    {
        var row=await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(
            x=>x.CustomerId==customerId && x.Operation==operation && x.IdempotencyKey==key,ct);
        if(row?.OrderId is null || row.OrderNumber is null) return null;
        return new IdempotencyRecord(row.IdempotencyKey,row.RequestFingerprint,new CreateOrderResult(row.OrderId.Value,row.OrderNumber));
    }
}
