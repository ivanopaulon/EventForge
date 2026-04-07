using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Data;

public partial class PrymDbContext
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

        _ = modelBuilder.Entity<Printer>()
            .HasOne(p => p.Station)
            .WithMany(s => s.Printers)
            .HasForeignKey(p => p.StationId);
    }

}
