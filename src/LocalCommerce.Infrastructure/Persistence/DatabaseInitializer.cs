using Microsoft.EntityFrameworkCore;

namespace LocalCommerce.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(CommerceDbContext db, CancellationToken ct = default)
    {
        var assembly = typeof(DatabaseInitializer).Assembly;
        var migrations = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("LocalCommerce.Infrastructure.Persistence.Migrations.", StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (migrations.Length == 0)
            throw new InvalidOperationException("No database migration resources were found.");

        foreach (var migration in migrations)
        {
            await using var stream = assembly.GetManifestResourceStream(migration)
                ?? throw new InvalidOperationException($"Database migration resource '{migration}' was not found.");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(ct);
            await db.Database.ExecuteSqlRawAsync(sql, ct);
        }
    }
}
