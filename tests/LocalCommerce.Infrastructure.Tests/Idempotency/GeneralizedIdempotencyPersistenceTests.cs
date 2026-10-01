using LocalCommerce.Application.Idempotency;
using LocalCommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalCommerce.Infrastructure.Tests.Idempotency;

public sealed class GeneralizedIdempotencyPersistenceTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("LOCAL_COMMERCE_POSTGRES")
        ?? "Host=localhost;Port=5432;Database=localcommerce;Username=postgres;Password=postgres";

    private static CommerceDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<CommerceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    private static async Task ResetAsync(CommerceDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""TRUNCATE TABLE "IdempotencyRecords" CASCADE""");
    }

    [Fact]
    public async Task Reserve_creates_a_resource_neutral_record()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);
        await ResetAsync(db);

        var store = new EfGeneralizedIdempotencyStore(db);
        var scopeId = Guid.NewGuid();

        var existing = await store.ReserveAsync(
            scopeId, "AssignDriver", "key-1", "fingerprint-1", default);

        Assert.Null(existing);

        var record = await store.GetAsync(scopeId, "AssignDriver", "key-1", default);

        Assert.NotNull(record);
        Assert.Equal(scopeId, record.ScopeId);
        Assert.Equal("AssignDriver", record.Operation);
        Assert.Equal("key-1", record.Key);
        Assert.Equal("fingerprint-1", record.Fingerprint);
        Assert.Equal(IdempotencyStatus.Reserved, record.Status);
        Assert.Null(record.ResourceType);
        Assert.Null(record.ResourceId);
        Assert.Null(record.ResultPayload);
    }

    [Fact]
    public async Task Reusing_a_key_with_a_different_fingerprint_returns_the_original_record()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);
        await ResetAsync(db);

        var store = new EfGeneralizedIdempotencyStore(db);
        var scopeId = Guid.NewGuid();

        Assert.Null(await store.ReserveAsync(
            scopeId, "AssignDriver", "fingerprint-key", "fingerprint-a", default));

        var existing = await store.ReserveAsync(
            scopeId, "AssignDriver", "fingerprint-key", "fingerprint-b", default);

        Assert.NotNull(existing);
        Assert.Equal("fingerprint-a", existing.Fingerprint);
        Assert.Equal(IdempotencyStatus.Reserved, existing.Status);
    }

    [Fact]
    public async Task Completion_stores_resource_reference_and_result_payload()
    {
        await using var db = CreateDb();
        await DatabaseInitializer.InitializeAsync(db);
        await ResetAsync(db);

        var store = new EfGeneralizedIdempotencyStore(db);
        var scopeId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        Assert.Null(await store.ReserveAsync(
            scopeId, "AssignDriver", "key-2", "fingerprint-2", default));

        await store.CompleteAsync(
            scopeId,
            "AssignDriver",
            "key-2",
            new IdempotencyCompletion("Delivery", resourceId, "{\"deliveryId\":\""
                + resourceId
                + "\"}"),
            default);

        await db.SaveChangesAsync();

        var record = await store.GetAsync(scopeId, "AssignDriver", "key-2", default);

        Assert.NotNull(record);
        Assert.Equal(IdempotencyStatus.Completed, record.Status);
        Assert.Equal("Delivery", record.ResourceType);
        Assert.Equal(resourceId, record.ResourceId);
        Assert.Contains(resourceId.ToString(), record.ResultPayload);
    }

    [Fact]
    public async Task Concurrent_reservation_allows_only_one_winner()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);
        await ResetAsync(setup);

        await using var db1 = CreateDb();
        await using var db2 = CreateDb();

        var scopeId = Guid.NewGuid();
        var a = new EfGeneralizedIdempotencyStore(db1);
        var b = new EfGeneralizedIdempotencyStore(db2);

        var results = await Task.WhenAll(
            a.ReserveAsync(scopeId, "CompleteDelivery", "race-key", "fingerprint", default),
            b.ReserveAsync(scopeId, "CompleteDelivery", "race-key", "fingerprint", default));

        Assert.Equal(1, results.Count(x => x is null));
        Assert.Equal(1, results.Count(x => x is not null));
    }

    [Fact]
    public async Task Reservation_and_completion_rollback_as_one_transaction()
    {
        await using var setup = CreateDb();
        await DatabaseInitializer.InitializeAsync(setup);
        await ResetAsync(setup);

        var scopeId = Guid.NewGuid();
        var key = "rollback-key";
        var fingerprint = "fingerprint";

        await using (var db = CreateDb())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var store = new EfGeneralizedIdempotencyStore(db);

            Assert.Null(await store.ReserveAsync(scopeId, "CancelOrder", key, fingerprint, default));

            await store.CompleteAsync(
                scopeId,
                "CancelOrder",
                key,
                new IdempotencyCompletion("Order", Guid.NewGuid(), null),
                default);

            await tx.RollbackAsync();
        }

        await using var verifyDb = CreateDb();
        var verify = new EfGeneralizedIdempotencyStore(verifyDb);

        Assert.Null(await verify.GetAsync(scopeId, "CancelOrder", key, default));
        Assert.Null(await verify.ReserveAsync(scopeId, "CancelOrder", key, fingerprint, default));
    }
}
