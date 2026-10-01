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
    private EfGeneralizedIdempotencyStore Generalized => new(db);
    public async Task<IdempotencyRecord?> GetAsync(
        Guid customerId,
        string operation,
        string key,
        CancellationToken cancellationToken)
    {
        var record = await Generalized.GetAsync(customerId, operation, key, cancellationToken);

        if (record is null || record.Status != LocalCommerce.Application.Idempotency.IdempotencyStatus.Completed)
            return null;

        return ToCreateOrderRecord(record);
    }

    public async Task<IdempotencyRecord?> ReserveAsync(
        Guid customerId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var record = await Generalized.ReserveAsync(
            customerId, operation, key, fingerprint, cancellationToken);

        if (record is null)
            return null;

        if (record.Status != LocalCommerce.Application.Idempotency.IdempotencyStatus.Completed)
            throw new InvalidOperationException(
                "Idempotency record exists but is not completed.");

        return ToCreateOrderRecord(record);
    }

    public Task CompleteAsync(
        Guid customerId,
        string operation,
        string key,
        CreateOrderResult result,
        CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(result);

        return Generalized.CompleteAsync(
            customerId,
            operation,
            key,
            new LocalCommerce.Application.Idempotency.IdempotencyCompletion(
                "Order",
                result.OrderId,
                payload),
            cancellationToken);
    }

    private static IdempotencyRecord ToCreateOrderRecord(
        LocalCommerce.Application.Idempotency.IdempotencyRecord record)
    {
        if (record.ResourceType != "Order"
            || record.ResourceId is null
            || string.IsNullOrWhiteSpace(record.ResultPayload))
            throw new InvalidOperationException(
                "Completed Create Order idempotency record is missing its Order result.");

        var result = System.Text.Json.JsonSerializer.Deserialize<CreateOrderResult>(
            record.ResultPayload)
            ?? throw new InvalidOperationException(
                "Completed Create Order idempotency result is invalid.");

        return new IdempotencyRecord(
            record.Key,
            record.Fingerprint,
            result);
    }
}
