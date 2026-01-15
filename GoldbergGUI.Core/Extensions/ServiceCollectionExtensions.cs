using GoldbergGUI.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace GoldbergGUI.Core.Extensions;

/// <summary>
/// Extension methods for service configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Steam database context with dependency injection
    /// </summary>
    public static IServiceCollection AddSteamDatabase(this IServiceCollection services, string databasePath = "steamapps.db")
    {
        services.AddDbContextFactory<SteamDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath}");
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        return services;
    }

    /// <summary>
    /// Registers memory cache with configured size limits
    /// </summary>
    public static IServiceCollection AddAppCache(this IServiceCollection services)
    {
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000; // Limit to 1000 entries
            options.CompactionPercentage = 0.25; // Remove 25% when limit reached
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        return services;
    }
}

