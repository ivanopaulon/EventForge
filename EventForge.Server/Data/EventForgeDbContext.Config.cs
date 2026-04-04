using EventForge.Server.Data.Entities;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
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
                var method = typeof(EventForgeDbContext)
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

}
