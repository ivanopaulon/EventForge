using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureStoreAndTeamRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<StoreUserGroup>()
            .HasMany(g => g.Privileges)
            .WithMany(p => p.Groups);

        _ = modelBuilder.Entity<StoreUser>()
            .HasOne(u => u.CashierGroup)
            .WithMany(g => g.Cashiers)
            .HasForeignKey(u => u.CashierGroupId);

        _ = modelBuilder.Entity<StoreUser>()
            .HasOne(u => u.PhotoDocument)
            .WithMany()
            .HasForeignKey(u => u.PhotoDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<StoreUser>().HasIndex(u => u.PhotoDocumentId).HasDatabaseName("IX_StoreUser_PhotoDocumentId");

        _ = modelBuilder.Entity<StoreUserGroup>()
            .HasOne(g => g.LogoDocument)
            .WithMany()
            .HasForeignKey(g => g.LogoDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<StoreUserGroup>().HasIndex(g => g.LogoDocumentId).HasDatabaseName("IX_StoreUserGroup_LogoDocumentId");

        _ = modelBuilder.Entity<StorePos>()
            .HasOne(p => p.ImageDocument)
            .WithMany()
            .HasForeignKey(p => p.ImageDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<StorePos>().HasIndex(p => p.ImageDocumentId).HasDatabaseName("IX_StorePos_ImageDocumentId");

        _ = modelBuilder.Entity<StorePos>()
            .HasOne(p => p.DefaultFiscalPrinter)
            .WithMany()
            .HasForeignKey(p => p.DefaultFiscalPrinterId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        _ = modelBuilder.Entity<StorePos>().HasIndex(p => p.DefaultFiscalPrinterId).HasDatabaseName("IX_StorePos_DefaultFiscalPrinterId");

        _ = modelBuilder.Entity<StorePos>()
            .HasOne(p => p.CashierGroup)
            .WithMany()
            .HasForeignKey(p => p.CashierGroupId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        _ = modelBuilder.Entity<StorePos>().HasIndex(p => p.CashierGroupId).HasDatabaseName("IX_StorePos_CashierGroupId");

        _ = modelBuilder.Entity<Printer>()
            .HasOne(p => p.Station)
            .WithMany(s => s.Printers)
            .HasForeignKey(p => p.StationId);

        _ = modelBuilder.Entity<Station>()
            .HasOne(s => s.AssignedPrinter)
            .WithMany()
            .HasForeignKey(s => s.AssignedPrinterId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }

}
