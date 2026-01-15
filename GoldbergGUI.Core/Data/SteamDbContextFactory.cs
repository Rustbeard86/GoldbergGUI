using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GoldbergGUI.Core.Data;

/// <summary>
/// Design-time factory for creating SteamDbContext instances for EF Core tools
/// </summary>
public sealed class SteamDbContextFactory : IDesignTimeDbContextFactory<SteamDbContext>
{
    public SteamDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SteamDbContext>();
        optionsBuilder.UseSqlite("Data Source=steamapps.db");

        return new SteamDbContext(optionsBuilder.Options);
    }
}
