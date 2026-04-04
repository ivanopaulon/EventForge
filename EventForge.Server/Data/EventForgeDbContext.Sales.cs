using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureWarehouseAndSalesRelationships(ModelBuilder modelBuilder)
    {
        {
            _ = modelBuilder.Entity<StorageLocation>()
                .HasOne(l => l.Warehouse)
                .WithMany(f => f.Locations)
                .HasForeignKey(l => l.WarehouseId);

            // TransferOrder relationships: esplicite, nessun cascade per evitare percorsi multipli/cicli
            _ = modelBuilder.Entity<TransferOrder>()
                .HasOne(t => t.SourceWarehouse)
                .WithMany()
                .HasForeignKey(t => t.SourceWarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            _ = modelBuilder.Entity<TransferOrder>()
                .HasOne(t => t.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(t => t.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.NoAction);

            _ = modelBuilder.Entity<TransferOrder>().HasIndex(t => t.SourceWarehouseId).HasDatabaseName("IX_TransferOrders_SourceWarehouseId");
            _ = modelBuilder.Entity<TransferOrder>().HasIndex(t => t.DestinationWarehouseId).HasDatabaseName("IX_TransferOrders_DestinationWarehouseId");

            _ = modelBuilder.Entity<PromotionRule>()
                .HasOne(r => r.Promotion)
                .WithMany(p => p.Rules)
                .HasForeignKey(r => r.PromotionId);

            _ = modelBuilder.Entity<PromotionRuleProduct>()
                .HasOne(rp => rp.PromotionRule)
                .WithMany(r => r.Products)
                .HasForeignKey(rp => rp.PromotionRuleId);

            _ = modelBuilder.Entity<PromotionRuleProduct>()
                .HasOne(rp => rp.Product)
                .WithMany()
                .HasForeignKey(rp => rp.ProductId);


            // SaleSession parent-child self-referencing relationship for split/merge
            _ = modelBuilder.Entity<SaleSession>()
                .HasOne(s => s.ParentSession)
                .WithMany(s => s.ChildSessions)
                .HasForeignKey(s => s.ParentSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = modelBuilder.Entity<SaleSession>()
                .HasIndex(s => s.ParentSessionId);

            // SaleSession split/merge fields configuration
            _ = modelBuilder.Entity<SaleSession>()
                .Property(s => s.SplitType)
                .HasMaxLength(50)
                .IsRequired(false);

            _ = modelBuilder.Entity<SaleSession>()
                .Property(s => s.SplitPercentage)
                .HasPrecision(5, 2)
                .IsRequired(false);

            _ = modelBuilder.Entity<SaleSession>()
                .Property(s => s.MergeReason)
                .HasMaxLength(500)
                .IsRequired(false);
        }

    }

}
