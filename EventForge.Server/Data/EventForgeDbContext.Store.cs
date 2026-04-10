using EventForge.Server.Data.Entities.Store;
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

        // FiscalDrawer relationships
        _ = modelBuilder.Entity<FiscalDrawer>()
            .HasOne(d => d.Pos)
            .WithMany()
            .HasForeignKey(d => d.PosId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        _ = modelBuilder.Entity<FiscalDrawer>().HasIndex(d => d.PosId).HasDatabaseName("IX_FiscalDrawer_PosId");

        _ = modelBuilder.Entity<FiscalDrawer>()
            .HasOne(d => d.Operator)
            .WithMany()
            .HasForeignKey(d => d.OperatorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        _ = modelBuilder.Entity<FiscalDrawer>().HasIndex(d => d.OperatorId).HasDatabaseName("IX_FiscalDrawer_OperatorId");

        _ = modelBuilder.Entity<FiscalDrawerSession>()
            .HasOne(s => s.FiscalDrawer)
            .WithMany(d => d.Sessions)
            .HasForeignKey(s => s.FiscalDrawerId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = modelBuilder.Entity<FiscalDrawerSession>().HasIndex(s => s.FiscalDrawerId).HasDatabaseName("IX_FiscalDrawerSession_FiscalDrawerId");
        _ = modelBuilder.Entity<FiscalDrawerSession>().HasIndex(s => s.SessionDate).HasDatabaseName("IX_FiscalDrawerSession_SessionDate");

        _ = modelBuilder.Entity<FiscalDrawerSession>()
            .HasOne(s => s.OpenedByOperator)
            .WithMany()
            .HasForeignKey(s => s.OpenedByOperatorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        _ = modelBuilder.Entity<FiscalDrawerSession>()
            .HasOne(s => s.ClosedByOperator)
            .WithMany()
            .HasForeignKey(s => s.ClosedByOperatorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        _ = modelBuilder.Entity<FiscalDrawerTransaction>()
            .HasOne(t => t.FiscalDrawer)
            .WithMany(d => d.Transactions)
            .HasForeignKey(t => t.FiscalDrawerId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = modelBuilder.Entity<FiscalDrawerTransaction>().HasIndex(t => t.FiscalDrawerId).HasDatabaseName("IX_FiscalDrawerTransaction_FiscalDrawerId");
        _ = modelBuilder.Entity<FiscalDrawerTransaction>().HasIndex(t => t.TransactionAt).HasDatabaseName("IX_FiscalDrawerTransaction_TransactionAt");

        _ = modelBuilder.Entity<FiscalDrawerTransaction>()
            .HasOne(t => t.FiscalDrawerSession)
            .WithMany(s => s.Transactions)
            .HasForeignKey(t => t.FiscalDrawerSessionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        _ = modelBuilder.Entity<CashDenomination>()
            .HasOne(d => d.FiscalDrawer)
            .WithMany(f => f.CashDenominations)
            .HasForeignKey(d => d.FiscalDrawerId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = modelBuilder.Entity<CashDenomination>().HasIndex(d => d.FiscalDrawerId).HasDatabaseName("IX_CashDenomination_FiscalDrawerId");

        _ = modelBuilder.Entity<StorePos>()
            .HasOne(p => p.DefaultPaymentTerminal)
            .WithMany()
            .HasForeignKey(p => p.DefaultPaymentTerminalId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        _ = modelBuilder.Entity<StorePos>().HasIndex(p => p.DefaultPaymentTerminalId).HasDatabaseName("IX_StorePos_DefaultPaymentTerminalId");
    }

}
