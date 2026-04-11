using Microsoft.EntityFrameworkCore;

namespace Prym.ManagementHub.Data;

public class ManagementHubDbContext(DbContextOptions<ManagementHubDbContext> options) : DbContext(options)
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
            e.HasIndex(x => x.InstallationCode).IsUnique().HasFilter("[InstallationCode] IS NOT NULL");
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

    /// <summary>
    /// Applies any schema additions that EnsureCreated would miss on an existing database.
    /// Idempotent — safe to call every startup.
    /// </summary>
    public void EnsureSchemaUpToDate()
    {
        var conn = Database.GetDbConnection();
        conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            // Add new columns to Installations if they don't exist (SQLite ALTER TABLE)
            foreach (var (col, def) in new[]
            {
                ("InstallationCode", "TEXT NULL"),
                ("IsRevoked",        "INTEGER NOT NULL DEFAULT 0"),
                ("RevokedAt",        "TEXT NULL"),
                ("RevokedReason",    "TEXT NULL"),
                ("MachineName",      "TEXT NULL"),
                ("OSVersion",        "TEXT NULL"),
                ("DotNetVersion",    "TEXT NULL"),
                ("AgentVersion",     "TEXT NULL"),
                ("IpAddress",        "TEXT NULL"),
                ("Tags",             "TEXT NULL"),
                ("UpdateMode",       "INTEGER NOT NULL DEFAULT 0")
            })
            {
                cmd.CommandText = $"ALTER TABLE Installations ADD COLUMN {col} {def}";
                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { /* Column already exists — expected on re-run */ }
            }

            // Add new columns to UpdatePackages if they don't exist
            foreach (var (col, def) in new[]
            {
                ("IsManualInstall", "INTEGER NOT NULL DEFAULT 0")
            })
            {
                cmd.CommandText = $"ALTER TABLE UpdatePackages ADD COLUMN {col} {def}";
                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) when (ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { /* Column already exists — expected on re-run */ }
            }

            // Unique index on InstallationCode (partial — only non-NULL rows)
            cmd.CommandText = """
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Installations_InstallationCode
                ON Installations(InstallationCode)
                WHERE InstallationCode IS NOT NULL
                """;
            try { cmd.ExecuteNonQuery(); } catch (Exception) { /* IF NOT EXISTS covers most cases */ }
        }
        finally
        {
            conn.Close();
        }
    }
}
