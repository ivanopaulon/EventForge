using Prym.Server.Data.Entities;
using Prym.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Data;

public partial class PrymDbContext
{
    private static void ConfigureTenantEntity(ModelBuilder modelBuilder)
    {
        // Configure Tenant entity to never generate Id values automatically
        // This allows us to explicitly set Id = Guid.Empty for the System tenant
        _ = modelBuilder.Entity<Tenant>()
            .Property(t => t.Id)
            .ValueGeneratedNever();
    }


    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        // RowVersion (optimistic concurrency) is configured via [Timestamp] on AuditableEntity — no
        // explicit .IsRowVersion() call needed here; EF Core recognises the [Timestamp] data annotation.

        // Default: set precision(18,6) for all decimal and nullable decimal properties
        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

        foreach (var prop in decimalProperties)
        {
            prop.SetPrecision(18);
            prop.SetScale(6);
        }

        // Override common percentage/ratio properties to precision(5,2)
        var percentagePropertyNames = new[]
        {
            "DiscountPercent",
            "DiscountPercentage",
            "VatRate",
            "TaxRate",
            "LineDiscount",
            "RecyclingRatePercentage",
            "Percentage"
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var p in entityType.GetProperties().Where(p => percentagePropertyNames.Contains(p.Name)))
            {
                p.SetPrecision(5);
                p.SetScale(2);
            }
        }

        // Specific known overrides for decimal fields that should have higher precision can be added here
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.AmountPaid).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.BaseCurrencyAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.ExchangeRate).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscountAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalGrossAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalNetAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.VatAmount).HasPrecision(18, 6);

        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.UnitPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.Quantity).HasPrecision(18, 4);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.BaseQuantity).HasPrecision(18, 4);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.BaseUnitPrice).HasPrecision(18, 4);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.LineDiscount).HasPrecision(5, 2);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.VatRate).HasPrecision(5, 2);

        _ = modelBuilder.Entity<PriceListEntry>().Property(x => x.Price).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Product>().Property(x => x.DefaultPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Promotion>().Property(x => x.MinOrderAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountPercentage).HasPrecision(5, 2);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.FixedPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.MinOrderAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<VatRate>().Property(x => x.Percentage).HasPrecision(5, 2);

        _ = modelBuilder.Entity<SaleSession>().Property(x => x.OriginalTotal).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.DiscountAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.FinalTotal).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.TaxAmount).HasPrecision(18, 6);

        _ = modelBuilder.Entity<SaleItem>().Property(x => x.UnitPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.Quantity).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.DiscountPercent).HasPrecision(5, 2);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TaxRate).HasPrecision(5, 2);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TaxAmount).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TotalAmount).HasPrecision(18, 6);

        _ = modelBuilder.Entity<SalePayment>().Property(x => x.Amount).HasPrecision(18, 6);

        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.Quantity).HasPrecision(18, 6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.WeightKg).HasPrecision(18, 6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.DisposalCost).HasPrecision(18, 6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.RecoveryValue).HasPrecision(18, 6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.RecyclingRatePercentage).HasPrecision(5, 2);
    }


    private static void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to all entities that inherit from AuditableEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(PrymDbContext)
                    .GetMethod(nameof(GetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType.ClrType);

                var filter = method?.Invoke(null, new object[] { });
                if (filter != null)
                {
                    _ = modelBuilder.Entity(entityType.ClrType).HasQueryFilter((System.Linq.Expressions.LambdaExpression)filter);
                }

                // Note: tenant filtering is applied in services/repositories for clarity
            }
        }
    }


    private static void ConfigureDailySequence(ModelBuilder modelBuilder)
    {
        // Configure DailySequence
        _ = modelBuilder.Entity<DailySequence>()
            .HasKey(ds => ds.Date);

        _ = modelBuilder.Entity<DailySequence>()
            .Property(ds => ds.Date)
            .HasColumnType("date");

        // Add unique constraint on Products.Code
        _ = modelBuilder.Entity<Product>()
            .HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("UQ_Products_Code");
    }



    /// <summary>
    /// Configures EntityChangeLog for optimal storage and query performance.
    /// Key decisions:
    /// - NEWSEQUENTIALID() PK default: eliminates B-tree page splits caused by random Guid values.
    ///   The table is append-only and high-volume; sequential keys keep the clustered index dense.
    /// - Non-clustered indexes on (TenantId, EntityId, ChangedAt) and (TenantId, ChangedAt):
    ///   the most common query patterns (audit history for an entity, audit history for a tenant).
    /// - OldValue/NewValue are NVARCHAR(2000) via [MaxLength(2000)] on the entity; values are
    ///   also truncated in CreateAuditEntries to guarantee the limit is never exceeded at runtime.
    /// </summary>
    private static void ConfigureEntityChangeLog(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<EntityChangeLog>()
            .Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .ValueGeneratedOnAdd();

        // Non-clustered index for per-entity audit queries: "show history of entity X in tenant T"
        _ = modelBuilder.Entity<EntityChangeLog>()
            .HasIndex(e => new { e.TenantId, e.EntityId, e.ChangedAt })
            .HasDatabaseName("IX_EntityChangeLogs_TenantId_EntityId_ChangedAt");

        // Non-clustered index for per-tenant audit timeline queries
        _ = modelBuilder.Entity<EntityChangeLog>()
            .HasIndex(e => new { e.TenantId, e.ChangedAt })
            .HasDatabaseName("IX_EntityChangeLogs_TenantId_ChangedAt");

        // Non-clustered index for per-entity-type queries ("who changed all Products today?")
        _ = modelBuilder.Entity<EntityChangeLog>()
            .HasIndex(e => new { e.EntityName, e.TenantId, e.ChangedAt })
            .HasDatabaseName("IX_EntityChangeLogs_EntityName_TenantId_ChangedAt");
    }
}
