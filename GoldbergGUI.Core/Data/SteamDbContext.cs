using Microsoft.EntityFrameworkCore;

namespace GoldbergGUI.Core.Data;

/// <summary>
/// Entity Framework Core database context for Steam apps cache
/// </summary>
public sealed class SteamDbContext(DbContextOptions<SteamDbContext> options) : DbContext(options)
{
    public DbSet<Models.SteamApp> SteamApps => Set<Models.SteamApp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.SteamApp>(entity =>
        {
            entity.ToTable("steamapp");
            entity.HasKey(e => e.AppId);
            
            entity.Property(e => e.AppId)
                .HasColumnName("appid")
                .IsRequired();
                
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(e => e.ComparableName)
                .HasColumnName("comparable_name")
                .IsRequired()
                .HasMaxLength(500);
                
            entity.Property(e => e.AppType)
                .HasColumnName("type")
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified");
                
            entity.Property(e => e.PriceChangeNumber)
                .HasColumnName("price_change_number");

            // Indexes for performance
            entity.HasIndex(e => e.ComparableName);
            entity.HasIndex(e => e.AppType);
        });
    }
}
