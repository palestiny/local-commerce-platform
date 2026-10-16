using LocalCommerce.Application.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Persistence;

public sealed class EfGeneralizedIdempotencyStore(CommerceDbContext db) : IIdempotencyStore
{
    public async Task<IdempotencyRecord?> GetAsync(
        Guid scopeId,
        string operation,
        string key,
        CancellationToken cancellationToken)
    {
        var row = await db.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ScopeId == scopeId
                    && x.Operation == operation
                    && x.IdempotencyKey == key,
                cancellationToken);

        return row is null ? null : ToRecord(row);
    }

    public async Task<IdempotencyRecord?> ReserveAsync(
        Guid scopeId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "IdempotencyRecords"
            ("Id","ScopeId","Operation","IdempotencyKey","RequestFingerprint","Status","CreatedAt")
            VALUES ({Guid.NewGuid()},{scopeId},{operation},{key},{fingerprint},'Reserved',{DateTimeOffset.UtcNow})
            ON CONFLICT ("ScopeId","Operation","IdempotencyKey") DO NOTHING
            """, cancellationToken);

        if (affected == 1)
            return null;

        var existing = await GetAsync(scopeId, operation, key, cancellationToken);
        return existing
            ?? throw new InvalidOperationException(
                "Idempotency record disappeared after a conflicting reservation.");
    }

    public async Task CompleteAsync(
        Guid scopeId,
        string operation,
        string key,
        IdempotencyCompletion completion,
        CancellationToken cancellationToken)
    {
        var row = await db.IdempotencyRecords.SingleOrDefaultAsync(
            x => x.ScopeId == scopeId
                && x.Operation == operation
                && x.IdempotencyKey == key,
            cancellationToken);

        if (row is null)
            throw new InvalidOperationException("Idempotency reservation was not found.");

        if (row.Status == IdempotencyStatus.Completed)
            throw new InvalidOperationException("Idempotency record is already completed.");

        row.Status = IdempotencyStatus.Completed;
        row.ResourceType = completion.ResourceType;
        row.ResourceId = completion.ResourceId;
        row.ResultPayload = completion.ResultPayload;
        row.CompletedAt = DateTimeOffset.UtcNow;
    }

    private static IdempotencyRecord ToRecord(GeneralizedIdempotencyEntity row) =>
        new(
            row.ScopeId,
            row.Operation,
            row.IdempotencyKey,
            row.RequestFingerprint,
            row.Status,
            row.ResourceType,
            row.ResourceId,
            row.ResultPayload);
}
