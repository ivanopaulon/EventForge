using Microsoft.EntityFrameworkCore;

namespace EventForge.UpdateHub.Data;

public class UpdateHubDbContext(DbContextOptions<UpdateHubDbContext> options) : DbContext(options)
{
    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<UpdatePackage> UpdatePackages => Set<UpdatePackage>();
    public DbSet<UpdateHistory> UpdateHistories => Set<UpdateHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Installation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ApiKey).IsUnique();
            e.HasMany(x => x.UpdateHistory).WithOne(x => x.Installation).HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<UpdatePackage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Version, x.Component }).IsUnique();
            e.HasMany(x => x.UpdateHistory).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
        });

        modelBuilder.Entity<UpdateHistory>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }
}
