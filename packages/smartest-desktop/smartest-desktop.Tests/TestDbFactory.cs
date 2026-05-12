using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;

namespace smartest_desktop.Tests;

internal static class TestDbFactory
{
    public static LocalDbContext CreateInMemory(string? name = null)
    {
        var opt = new DbContextOptionsBuilder<LocalDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        var db = new LocalDbContext(opt);
        db.Database.EnsureCreated();
        return db;
    }
}
