using Microsoft.EntityFrameworkCore;
namespace LocalCommerce.Infrastructure.Persistence;
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(CommerceDbContext db,CancellationToken ct=default)
    {
        await using var stream=typeof(DatabaseInitializer).Assembly.GetManifestResourceStream("LocalCommerce.Infrastructure.Persistence.Migrations.001_initial.sql")
            ?? throw new InvalidOperationException("Initial migration resource not found.");
        using var reader=new StreamReader(stream);
        var sql=await reader.ReadToEndAsync(ct);
        await db.Database.ExecuteSqlRawAsync(sql,ct);
    }
}
