using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Data;

public partial class PrymDbContext
{
    private static void ConfigureProductAndPriceListRelationships(ModelBuilder modelBuilder)
    {
        // PriceList → PriceListEntry
        _ = modelBuilder.Entity<PriceListEntry>()
            .HasOne(e => e.PriceList)
            .WithMany(l => l.ProductPrices)
            .HasForeignKey(e => e.PriceListId);

        // Product → ProductUnit
        _ = modelBuilder.Entity<ProductUnit>()
            .HasOne(u => u.Product)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.ProductId);

        // Product → ProductCode
        _ = modelBuilder.Entity<ProductCode>()
            .HasOne(c => c.Product)
            .WithMany(p => p.Codes)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ProductCode>()
            .HasOne(c => c.ProductUnit)
            .WithMany()
            .HasForeignKey(c => c.ProductUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // ProductBundleItem
        _ = modelBuilder.Entity<ProductBundleItem>()
            .HasOne(b => b.BundleProduct)
            .WithMany(p => p.BundleItems)
            .HasForeignKey(b => b.BundleProductId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ProductBundleItem>()
            .HasOne(b => b.ComponentProduct)
            .WithMany(p => p.IncludedInBundles)
            .HasForeignKey(b => b.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Brand / Model / Product / ProductSupplier
        _ = modelBuilder.Entity<Model>()
            .HasOne(m => m.Brand)
            .WithMany(b => b.Models)
            .HasForeignKey(m => m.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Product>()
            .HasOne(p => p.Model)
            .WithMany(m => m.Products)
            .HasForeignKey(p => p.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ProductSupplier>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.Suppliers)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ProductSupplier>()
            .HasOne(ps => ps.Supplier)
            .WithMany()
            .HasForeignKey(ps => ps.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ProductSupplier>().HasIndex(ps => ps.ProductId).HasDatabaseName("IX_ProductSupplier_ProductId");
        _ = modelBuilder.Entity<ProductSupplier>().HasIndex(ps => ps.SupplierId).HasDatabaseName("IX_ProductSupplier_SupplierId");
        _ = modelBuilder.Entity<ProductSupplier>().HasIndex(ps => new { ps.ProductId, ps.Preferred }).HasDatabaseName("IX_ProductSupplier_ProductId_Preferred");

        _ = modelBuilder.Entity<ProductSupplier>().Property(ps => ps.UnitCost).HasPrecision(18, 6);
        _ = modelBuilder.Entity<ProductSupplier>().Property(ps => ps.LastPurchasePrice).HasPrecision(18, 6);

        // SupplierProductPriceHistory relationships
        _ = modelBuilder.Entity<SupplierProductPriceHistory>()
            .HasOne(h => h.ProductSupplier)
            .WithMany()
            .HasForeignKey(h => h.ProductSupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<SupplierProductPriceHistory>()
            .HasOne(h => h.Supplier)
            .WithMany()
            .HasForeignKey(h => h.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<SupplierProductPriceHistory>()
            .HasOne(h => h.Product)
            .WithMany()
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<SupplierProductPriceHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SupplierProductPriceHistory indexes for performance
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => h.SupplierId).HasDatabaseName("IX_SupplierProductPriceHistory_SupplierId");
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => h.ProductId).HasDatabaseName("IX_SupplierProductPriceHistory_ProductId");
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => h.ChangedAt).HasDatabaseName("IX_SupplierProductPriceHistory_ChangedAt");
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => h.ProductSupplierId).HasDatabaseName("IX_SupplierProductPriceHistory_ProductSupplierId");
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => new { h.SupplierId, h.ChangedAt }).HasDatabaseName("IX_SupplierProductPriceHistory_SupplierId_ChangedAt");
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().HasIndex(h => new { h.ProductId, h.ChangedAt }).HasDatabaseName("IX_SupplierProductPriceHistory_ProductId_ChangedAt");

        // SupplierProductPriceHistory decimal precision
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().Property(h => h.OldUnitCost).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().Property(h => h.NewUnitCost).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().Property(h => h.PriceChange).HasPrecision(18, 6);
        _ = modelBuilder.Entity<SupplierProductPriceHistory>().Property(h => h.PriceChangePercentage).HasPrecision(18, 6);

        _ = modelBuilder.Entity<Product>().HasIndex(p => p.BrandId).HasDatabaseName("IX_Product_BrandId");
        _ = modelBuilder.Entity<Product>().HasIndex(p => p.ModelId).HasDatabaseName("IX_Product_ModelId");
        _ = modelBuilder.Entity<Product>().HasIndex(p => p.PreferredSupplierId).HasDatabaseName("IX_Product_PreferredSupplierId");

        _ = modelBuilder.Entity<Product>().Property(p => p.ReorderPoint).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Product>().Property(p => p.SafetyStock).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Product>().Property(p => p.TargetStockLevel).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Product>().Property(p => p.AverageDailyDemand).HasPrecision(18, 6);

        _ = modelBuilder.Entity<Product>()
            .HasOne(p => p.ImageDocument)
            .WithMany()
            .HasForeignKey(p => p.ImageDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Product>().HasIndex(p => p.ImageDocumentId).HasDatabaseName("IX_Product_ImageDocumentId");

        // PriceListBusinessParty → PriceList
        _ = modelBuilder.Entity<PriceListBusinessParty>()
            .HasKey(plbp => new { plbp.PriceListId, plbp.BusinessPartyId });

        _ = modelBuilder.Entity<PriceListBusinessParty>()
            .HasOne(plbp => plbp.PriceList)
            .WithMany(pl => pl.BusinessParties)
            .HasForeignKey(plbp => plbp.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        // PriceListBusinessParty → BusinessParty
        _ = modelBuilder.Entity<PriceListBusinessParty>()
            .HasOne(plbp => plbp.BusinessParty)
            .WithMany()
            .HasForeignKey(plbp => plbp.BusinessPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        // PriceListBusinessParty indexes
        _ = modelBuilder.Entity<PriceListBusinessParty>().HasIndex(plbp => plbp.Status)
            .HasDatabaseName("IX_PriceListBusinessParties_Status");

        _ = modelBuilder.Entity<PriceListBusinessParty>().HasIndex(plbp => plbp.IsPrimary)
            .HasDatabaseName("IX_PriceListBusinessParties_IsPrimary");

        _ = modelBuilder.Entity<PriceListBusinessParty>().HasIndex(plbp => new { plbp.BusinessPartyId, plbp.Status })
            .HasDatabaseName("IX_PriceListBusinessParties_BusinessPartyId_Status");

        // PriceListBusinessParty decimal precision
        _ = modelBuilder.Entity<PriceListBusinessParty>().Property(plbp => plbp.GlobalDiscountPercentage)
            .HasPrecision(5, 2);

        // PriceList indexes
        _ = modelBuilder.Entity<PriceList>().HasIndex(pl => new { pl.Type, pl.Direction })
            .HasDatabaseName("IX_PriceLists_Type_Direction");

        _ = modelBuilder.Entity<PriceList>().HasIndex(pl => pl.EventId)
            .HasDatabaseName("IX_PriceLists_EventId");

        // PriceList property configurations for document generation (FASE 2C - PR #4)
        _ = modelBuilder.Entity<PriceList>()
            .Property(p => p.IsGeneratedFromDocuments)
            .IsRequired();

        _ = modelBuilder.Entity<PriceList>()
            .Property(p => p.GenerationMetadata)
            .HasMaxLength(4000);

        _ = modelBuilder.Entity<PriceList>()
            .Property(p => p.LastSyncedBy)
            .HasMaxLength(256);

        // PriceListEntry → UnitOfMeasure
        _ = modelBuilder.Entity<PriceListEntry>()
            .HasOne(e => e.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<PriceListEntry>().HasIndex(e => e.UnitOfMeasureId)
            .HasDatabaseName("IX_PriceListEntries_UnitOfMeasureId");
    }

    private static void ConfigureAlertRelationships(ModelBuilder modelBuilder)
    {
        // SupplierPriceAlert relationships
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>()
            .HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>()
            .HasOne(a => a.Supplier)
            .WithMany()
            .HasForeignKey(a => a.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // SupplierPriceAlert indexes for performance
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => a.TenantId).HasDatabaseName("IX_SupplierPriceAlert_TenantId");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => a.ProductId).HasDatabaseName("IX_SupplierPriceAlert_ProductId");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => a.SupplierId).HasDatabaseName("IX_SupplierPriceAlert_SupplierId");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => a.Status).HasDatabaseName("IX_SupplierPriceAlert_Status");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => a.CreatedAt).HasDatabaseName("IX_SupplierPriceAlert_CreatedAt");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => new { a.TenantId, a.Status }).HasDatabaseName("IX_SupplierPriceAlert_TenantId_Status");
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().HasIndex(a => new { a.TenantId, a.Status, a.CreatedAt }).HasDatabaseName("IX_SupplierPriceAlert_TenantId_Status_CreatedAt");

        // SupplierPriceAlert decimal precision
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().Property(a => a.OldPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().Property(a => a.NewPrice).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().Property(a => a.PriceChangePercentage).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Entities.Alerts.SupplierPriceAlert>().Property(a => a.PotentialSavings).HasPrecision(18, 6);

        // AlertConfiguration indexes
        _ = modelBuilder.Entity<Entities.Alerts.AlertConfiguration>().HasIndex(ac => ac.TenantId).HasDatabaseName("IX_AlertConfiguration_TenantId");
        _ = modelBuilder.Entity<Entities.Alerts.AlertConfiguration>().HasIndex(ac => new { ac.TenantId, ac.UserId }).HasDatabaseName("IX_AlertConfiguration_TenantId_UserId").IsUnique();

        // AlertConfiguration decimal precision
        _ = modelBuilder.Entity<Entities.Alerts.AlertConfiguration>().Property(ac => ac.PriceIncreaseThresholdPercentage).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Entities.Alerts.AlertConfiguration>().Property(ac => ac.PriceDecreaseThresholdPercentage).HasPrecision(18, 6);
        _ = modelBuilder.Entity<Entities.Alerts.AlertConfiguration>().Property(ac => ac.VolatilityThresholdPercentage).HasPrecision(18, 6);
    }

}
