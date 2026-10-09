namespace LocalCommerce.Application.Idempotency;

public enum IdempotencyStatus
{
    Reserved,
    Completed
}

public sealed record IdempotencyRecord(
    Guid ScopeId,
    string Operation,
    string Key,
    string Fingerprint,
    IdempotencyStatus Status,
    string? ResourceType,
    Guid? ResourceId,
    string? ResultPayload);

public sealed record IdempotencyCompletion(
    string ResourceType,
    Guid ResourceId,
    string? ResultPayload);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(
        Guid scopeId,
        string operation,
        string key,
        CancellationToken cancellationToken);

    Task<IdempotencyRecord?> ReserveAsync(
        Guid scopeId,
        string operation,
        string key,
        string fingerprint,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid scopeId,
        string operation,
        string key,
        IdempotencyCompletion completion,
        CancellationToken cancellationToken);
}
