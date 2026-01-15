using GoldbergGUI.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoldbergGUI.Core.Services;

/// <summary>
/// Service for managing database migrations
/// </summary>
public interface IDatabaseMigrationService
{
    Task MigrateAsync();
    Task<bool> HasPendingMigrationsAsync();
}

/// <summary>
/// Implementation of database migration service
/// </summary>
public sealed class DatabaseMigrationService(
    IDbContextFactory<SteamDbContext> contextFactory,
    ILogger<DatabaseMigrationService> logger) : IDatabaseMigrationService
{
    /// <summary>
    /// Applies all pending migrations to the database
    /// </summary>
    public async Task MigrateAsync()
    {
        logger.LogInformation("Starting database migration...");
        
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        
        try
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
            var migrations = pendingMigrations.ToList();
            
            if (migrations.Count > 0)
            {
                logger.LogInformation("Applying {Count} pending migration(s): {Migrations}", 
                    migrations.Count, 
                    string.Join(", ", migrations));
                    
                await context.Database.MigrateAsync().ConfigureAwait(false);
                
                logger.LogInformation("Database migration completed successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    /// <summary>
    /// Checks if there are any pending migrations
    /// </summary>
    public async Task<bool> HasPendingMigrationsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
        return pendingMigrations.Any();
    }
}
