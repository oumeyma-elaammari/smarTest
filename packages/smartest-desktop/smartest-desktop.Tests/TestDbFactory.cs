using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;

namespace smartest_desktop.Tests;

internal static class TestDbFactory
{
    /// <summary>Contexte EF Core InMemory isolé (nom de base unique par défaut).</summary>
    public static LocalDbContext CreateInMemoryContext(string? name = null)
    {
        var opt = new DbContextOptionsBuilder<LocalDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        var db = new LocalDbContext(opt);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>Alias historique.</summary>
    public static LocalDbContext CreateInMemory(string? name = null) => CreateInMemoryContext(name);
}
