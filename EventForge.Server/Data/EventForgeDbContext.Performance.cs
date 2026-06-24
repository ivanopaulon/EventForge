using EventForge.Server.Data.Entities.Notifications;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // ── DocumentHeaders ─────────────────────────────────────────────────
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasIndex(dh => new { dh.TenantId, dh.Date })
            .HasDatabaseName("IX_DocumentHeaders_TenantId_Date");

        _ = modelBuilder.Entity<DocumentHeader>()
            .HasIndex(dh => dh.BusinessPartyId)
            .HasDatabaseName("IX_DocumentHeaders_BusinessPartyId");

        // ── DocumentRows ─────────────────────────────────────────────────────
        _ = modelBuilder.Entity<DocumentRow>()
            .HasIndex(dr => new { dr.TenantId, dr.DocumentHeaderId })
            .HasDatabaseName("IX_DocumentRows_TenantId_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentRow>()
            .HasIndex(dr => new { dr.TenantId, dr.ProductId })
            .HasDatabaseName("IX_DocumentRows_TenantId_ProductId");

        _ = modelBuilder.Entity<DocumentRow>()
            .HasIndex(dr => dr.IsPriceManual)
            .HasDatabaseName("IX_DocumentRows_IsPriceManual");

        _ = modelBuilder.Entity<DocumentRow>()
            .HasIndex(dr => new { dr.TenantId, dr.DocumentHeaderId })
            .HasDatabaseName("IX_DocumentRows_AppliedPromotionsJSON_NotNull")
            .HasFilter("[AppliedPromotionsJSON] IS NOT NULL");

        // ── PriceListEntry ───────────────────────────────────────────────────
        _ = modelBuilder.Entity<PriceListEntry>()
            .HasIndex(e => e.PriceListId)
            .HasDatabaseName("IX_PriceListEntries_PriceListId");

        // ── Promotions ───────────────────────────────────────────────────────
        _ = modelBuilder.Entity<Promotion>()
            .HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("IX_Promotions_TenantId_IsActive");

        _ = modelBuilder.Entity<Promotion>()
            .HasIndex(p => new { p.TenantId, p.StartDate, p.EndDate })
            .HasDatabaseName("IX_Promotions_TenantId_StartDate_EndDate");

        _ = modelBuilder.Entity<PromotionRule>()
            .HasIndex(r => r.PromotionId)
            .HasDatabaseName("IX_PromotionRules_PromotionId");

        _ = modelBuilder.Entity<PromotionRuleProduct>()
            .HasIndex(rp => rp.PromotionRuleId)
            .HasDatabaseName("IX_PromotionRuleProducts_PromotionRuleId");

        _ = modelBuilder.Entity<PromotionRuleProduct>()
            .HasIndex(rp => rp.ProductId)
            .HasDatabaseName("IX_PromotionRuleProducts_ProductId");

        // ── PriceLists ───────────────────────────────────────────────────────
        _ = modelBuilder.Entity<PriceList>()
            .HasIndex(pl => new { pl.TenantId, pl.Priority })
            .HasDatabaseName("IX_PriceLists_TenantId_Priority");

        // ── Auth ─────────────────────────────────────────────────────────────
        _ = modelBuilder.Entity<UserRole>()
            .HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        _ = modelBuilder.Entity<UserRole>()
            .HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");

        _ = modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => rp.RoleId)
            .HasDatabaseName("IX_RolePermissions_RoleId");

        _ = modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("IX_RolePermissions_PermissionId");

        _ = modelBuilder.Entity<LoginAudit>()
            .HasIndex(la => la.UserId)
            .HasDatabaseName("IX_LoginAudits_UserId");

        // ── Notifications ────────────────────────────────────────────────────
        _ = modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(nr => nr.NotificationId)
            .HasDatabaseName("IX_NotificationRecipients_NotificationId");

        // ── Sales ─────────────────────────────────────────────────────────────
        _ = modelBuilder.Entity<SaleItem>()
            .HasIndex(si => si.SaleSessionId)
            .HasDatabaseName("IX_SaleItems_SaleSessionId");

        _ = modelBuilder.Entity<SalePayment>()
            .HasIndex(sp => sp.SaleSessionId)
            .HasDatabaseName("IX_SalePayments_SaleSessionId");

        // ── Printers ──────────────────────────────────────────────────────────
        // Covers fiscal-printer polling queries from hosted services (IsFiscalPrinter=1, IsDeleted=0).
        // Without this index the query was performing a full-table scan (~5500 ms observed).
        _ = modelBuilder.Entity<Printer>()
            .HasIndex(p => new { p.IsDeleted, p.IsFiscalPrinter })
            .HasDatabaseName("IX_Printers_IsDeleted_IsFiscalPrinter");

        // ── BusinessParties ───────────────────────────────────────────────────
        // Accelerates the paginated list and text-search queries that always filter on
        // TenantId + IsDeleted before ordering/paging by Name.
        _ = modelBuilder.Entity<BusinessParty>()
            .HasIndex(bp => new { bp.TenantId, bp.IsDeleted, bp.Name })
            .HasDatabaseName("IX_BusinessParties_TenantId_IsDeleted_Name");

        // ── SystemOperationLogs ───────────────────────────────────────────────
        // Supports the monitoring-dashboard query: WHERE Severity IN (...) ORDER BY ExecutedAt DESC.
        _ = modelBuilder.Entity<SystemOperationLog>()
            .HasIndex(l => new { l.Severity, l.ExecutedAt })
            .HasDatabaseName("IX_SystemOperationLogs_Severity_ExecutedAt");

        // ── UserRoles (composite with ExpiresAt) ──────────────────────────────
        // Speeds up the filtered-include in RefreshTokenAsync:
        //   .Include(u => u.UserRoles.Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > now))
        _ = modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.ExpiresAt })
            .HasDatabaseName("IX_UserRoles_UserId_ExpiresAt");
    }
}
