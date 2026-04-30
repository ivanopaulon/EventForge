using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureShiftRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<CashierShift>()
            .HasOne(s => s.StoreUser)
            .WithMany()
            .HasForeignKey(s => s.StoreUserId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<CashierShift>()
            .HasIndex(s => s.StoreUserId)
            .HasDatabaseName("IX_CashierShift_StoreUserId");

        _ = modelBuilder.Entity<CashierShift>()
            .HasOne(s => s.Pos)
            .WithMany()
            .HasForeignKey(s => s.PosId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        _ = modelBuilder.Entity<CashierShift>()
            .HasIndex(s => s.PosId)
            .HasDatabaseName("IX_CashierShift_PosId");

        _ = modelBuilder.Entity<CashierShift>()
            .HasIndex(s => s.ShiftStart)
            .HasDatabaseName("IX_CashierShift_ShiftStart");

        _ = modelBuilder.Entity<CashierShift>()
            .HasIndex(s => s.ShiftEnd)
            .HasDatabaseName("IX_CashierShift_ShiftEnd");

        _ = modelBuilder.Entity<CashierShift>()
            .HasIndex(s => new { s.TenantId, s.ShiftStart, s.ShiftEnd })
            .HasDatabaseName("IX_CashierShift_TenantId_ShiftStart_ShiftEnd");
    }
}
