using EventForge.Server.Data.Entities.Reports;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    // ── DbSets ──────────────────────────────────────────────────────────────

    /// <summary>Bold Reports report definitions (RDLC layout + metadata).</summary>
    public DbSet<ReportDefinition> ReportDefinitions { get; set; }

    /// <summary>Data source declarations for report definitions.</summary>
    public DbSet<ReportDataSource> ReportDataSources { get; set; }

    // ── Model configuration ─────────────────────────────────────────────────

    private static void ConfigureReportRelationships(ModelBuilder modelBuilder)
    {
        // ReportDataSource → ReportDefinition
        _ = modelBuilder.Entity<ReportDataSource>()
            .HasOne(rds => rds.ReportDefinition)
            .WithMany(rd => rd.DataSources)
            .HasForeignKey(rds => rds.ReportDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique: data source name must be unique within a report definition
        _ = modelBuilder.Entity<ReportDataSource>()
            .HasIndex(rds => new { rds.ReportDefinitionId, rds.DataSourceName })
            .IsUnique()
            .HasDatabaseName("IX_ReportDataSources_ReportDefinitionId_DataSourceName");

        // Performance index: list reports by tenant + category
        _ = modelBuilder.Entity<ReportDefinition>()
            .HasIndex(rd => new { rd.TenantId, rd.Category, rd.IsDeleted })
            .HasDatabaseName("IX_ReportDefinitions_TenantId_Category_IsDeleted");

        // Performance index: report definitions by tenant + active
        _ = modelBuilder.Entity<ReportDefinition>()
            .HasIndex(rd => new { rd.TenantId, rd.IsActive, rd.IsDeleted })
            .HasDatabaseName("IX_ReportDefinitions_TenantId_IsActive_IsDeleted");
    }
}
