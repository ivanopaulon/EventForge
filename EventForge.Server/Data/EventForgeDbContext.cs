using EventForge.Server.Data.Entities;
using EventForge.Server.Data.Entities.Calendar;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Data.Entities.Notifications;
using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Data.Entities.Store;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// HashSet of entity types that are excluded from automatic audit tracking.
    /// <list type="bullet">
    /// <item>Sales entities use manual audit logging in SaleSessionService for better control.</item>
    /// <item>Infrastructure/log entities (PerformanceLog, LoginAudit, AuditTrail, BackupOperation)
    ///   must not be audited: auditing them would generate recursive EntityChangeLog rows on every
    ///   write and produce a cascade of noisy DB inserts (visible as giant EF CommandExecuted log
    ///   entries).</item>
    /// </list>
    /// </summary>
    private static readonly HashSet<Type> ExcludedFromAutomaticAudit = new()
    {
        // Sales entities — use manual audit in SaleSessionService
        typeof(SaleSession),
        typeof(SaleItem),
        typeof(SalePayment),
        typeof(SessionNote),

        // Infrastructure / log entities — must not be auto-audited to prevent recursive inserts
        typeof(PerformanceLog),    // written every minute by PerformanceCollectorService
        typeof(LoginAudit),        // the login-audit record itself must not spawn more audit rows
        typeof(AuditTrail),        // same: the audit trail is the audit, not something to audit
        typeof(BackupOperation),   // internal operation tracking, no property-level audit needed
    };

    public EventForgeDbContext(DbContextOptions<EventForgeDbContext> options)
        : base(options)
    {
    }

    public EventForgeDbContext(DbContextOptions<EventForgeDbContext> options, IHttpContextAccessor? httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #region DbSets

    // Common Entities
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Reference> References { get; set; }
    public DbSet<ClassificationNode> ClassificationNodes { get; set; }
    public DbSet<Printer> Printers { get; set; }
    public DbSet<EventForge.Server.Data.Entities.FiscalPrinting.DailyClosureRecord> DailyClosureRecords { get; set; }
    public DbSet<UM> UMs { get; set; }
    public DbSet<VatRate> VatRates { get; set; }
    public DbSet<VatNature> VatNatures { get; set; }

    // Business Entities
    public DbSet<BusinessParty> BusinessParties { get; set; }
    public DbSet<BusinessPartyAccounting> BusinessPartyAccountings { get; set; }
    public DbSet<PaymentTerm> PaymentTerms { get; set; }
    public DbSet<BusinessPartyGroup> BusinessPartyGroups { get; set; }
    public DbSet<BusinessPartyGroupMember> BusinessPartyGroupMembers { get; set; }

    // Document Entities
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<DocumentCounter> DocumentCounters { get; set; }
    public DbSet<DocumentHeader> DocumentHeaders { get; set; }
    public DbSet<DocumentRow> DocumentRows { get; set; }
    public DbSet<DocumentSummaryLink> DocumentSummaryLinks { get; set; }
    public DbSet<DocumentAttachment> DocumentAttachments { get; set; }
    public DbSet<DocumentComment> DocumentComments { get; set; }
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public DbSet<DocumentWorkflow> DocumentWorkflows { get; set; }
    public DbSet<DocumentWorkflowExecution> DocumentWorkflowExecutions { get; set; }
    public DbSet<DocumentRecurrence> DocumentRecurrences { get; set; }
    public DbSet<DocumentAnalytics> DocumentAnalytics { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<DocumentReminder> DocumentReminders { get; set; }
    public DbSet<DocumentSchedule> DocumentSchedules { get; set; }
    public DbSet<DocumentRetentionPolicy> DocumentRetentionPolicies { get; set; }
    public DbSet<DocumentAccessLog> DocumentAccessLogs { get; set; }
    public DbSet<DocumentReference> DocumentReferences { get; set; }
    public DbSet<DocumentStatusHistory> DocumentStatusHistories { get; set; }

    // Event & Team Entities
    public DbSet<Event> Events { get; set; }
    public DbSet<EventTimeSlot> EventTimeSlots { get; set; }
    public DbSet<CalendarReminder> CalendarReminders { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<MembershipCard> MembershipCards { get; set; }
    public DbSet<InsurancePolicy> InsurancePolicies { get; set; }

    // Product Entities
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCode> ProductCodes { get; set; }
    public DbSet<ProductUnit> ProductUnits { get; set; }
    public DbSet<ProductBundleItem> ProductBundleItems { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Model> Models { get; set; }
    public DbSet<ProductSupplier> ProductSuppliers { get; set; }
    public DbSet<SupplierProductPriceHistory> SupplierProductPriceHistories { get; set; }

    // Price List & Promotion Entities
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListEntry> PriceListEntries { get; set; }
    public DbSet<PriceListBusinessParty> PriceListBusinessParties { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionRule> PromotionRules { get; set; }
    public DbSet<PromotionRuleProduct> PromotionRuleProducts { get; set; }

    // Warehouse & Stock Entities
    public DbSet<StorageFacility> StorageFacilities { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }
    public DbSet<Lot> Lots { get; set; }
    public DbSet<Serial> Serials { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<StockMovementPlan> StockMovementPlans { get; set; }
    public DbSet<StockAlert> StockAlerts { get; set; }
    public DbSet<QualityControl> QualityControls { get; set; }
    public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
    public DbSet<SustainabilityCertificate> SustainabilityCertificates { get; set; }
    public DbSet<WasteManagementRecord> WasteManagementRecords { get; set; }
    public DbSet<ProjectOrder> ProjectOrders { get; set; }
    public DbSet<ProjectMaterialAllocation> ProjectMaterialAllocations { get; set; }
    public DbSet<TransferOrder> TransferOrders { get; set; }
    public DbSet<TransferOrderRow> TransferOrderRows { get; set; }

    // Station Monitor Entities
    public DbSet<Station> Stations { get; set; }
    public DbSet<StationOrderQueueItem> StationOrderQueueItems { get; set; }

    // Store Entities
    public DbSet<StorePos> StorePoses { get; set; }
    public DbSet<StoreUser> StoreUsers { get; set; }
    public DbSet<StoreUserGroup> StoreUserGroups { get; set; }
    public DbSet<StoreUserPrivilege> StoreUserPrivileges { get; set; }

    // Fiscal Drawer Entities
    public DbSet<FiscalDrawer> FiscalDrawers { get; set; }
    public DbSet<FiscalDrawerSession> FiscalDrawerSessions { get; set; }
    public DbSet<FiscalDrawerTransaction> FiscalDrawerTransactions { get; set; }
    public DbSet<CashDenomination> CashDenominations { get; set; }
    public DbSet<EventForge.Server.Data.Entities.Store.PaymentTerminal> PaymentTerminals { get; set; }

    // Authentication & Authorization Entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<LoginAudit> LoginAudits { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AdminTenant> AdminTenants { get; set; }
    public DbSet<AuditTrail> AuditTrails { get; set; }

    // Licensing Entities
    public DbSet<License> Licenses { get; set; }
    public DbSet<LicenseFeature> LicenseFeatures { get; set; }
    public DbSet<LicenseFeaturePermission> LicenseFeaturePermissions { get; set; }
    public DbSet<TenantLicense> TenantLicenses { get; set; }
    public DbSet<FeatureTemplate> FeatureTemplates { get; set; }

    // System Configuration Entities
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<BackupOperation> BackupOperations { get; set; }
    public DbSet<JwtKeyHistory> JwtKeyHistories { get; set; }
    public DbSet<SystemOperationLog> SystemOperationLogs { get; set; }
    public DbSet<SetupHistory> SetupHistories { get; set; }
    public DbSet<SystemAlert> SystemAlerts { get; set; }
    public DbSet<PerformanceLog> PerformanceLogs { get; set; }

    // Notification Entities
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }

    // Chat Entities
    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ChatMember> ChatMembers { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }
    public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }

    // Sales Entities
    public DbSet<SaleSession> SaleSessions { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<SalePayment> SalePayments { get; set; }
    public DbSet<Entities.Sales.PaymentMethod> PaymentMethods { get; set; }
    public DbSet<SessionNote> SessionNotes { get; set; }
    public DbSet<NoteFlag> NoteFlags { get; set; }
    public DbSet<TableSession> TableSessions { get; set; }
    public DbSet<TableReservation> TableReservations { get; set; }

    // Audit & Logging Entities
    public DbSet<EntityChangeLog> EntityChangeLogs { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }

    // Code Generation Entities
    public DbSet<DailySequence> DailySequences { get; set; }

    // Dashboard Entities
    public DbSet<Entities.Dashboard.DashboardConfiguration> DashboardConfigurations { get; set; }
    public DbSet<Entities.Dashboard.DashboardMetricConfig> DashboardMetricConfigs { get; set; }

    // Alert Entities
    public DbSet<Entities.Alerts.SupplierPriceAlert> SupplierPriceAlerts { get; set; }
    public DbSet<Entities.Alerts.AlertConfiguration> AlertConfigurations { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTenantEntity(modelBuilder);
        ConfigureDecimalPrecision(modelBuilder);
        ConfigureGlobalQueryFilters(modelBuilder);
        ConfigureDocumentRelationships(modelBuilder);
        ConfigureProductAndPriceListRelationships(modelBuilder);
        ConfigureAlertRelationships(modelBuilder);
        ConfigureStoreAndTeamRelationships(modelBuilder);
        ConfigureWarehouseAndSalesRelationships(modelBuilder);
        ConfigureAuthRelationships(modelBuilder);
        ConfigureChatRelationships(modelBuilder);
        ConfigureDailySequence(modelBuilder);
        ConfigurePriceApplicationMode(modelBuilder);
        ConfigurePerformanceIndexes(modelBuilder);
        ConfigureCalendarReminderRelationships(modelBuilder);
        ConfigureEventTimeSlotRelationships(modelBuilder);
        ConfigureEntityChangeLog(modelBuilder);
        ConfigureFiscalPrintingRelationships(modelBuilder);
        ConfigureWhatsAppRelationships(modelBuilder);
        ConfigureReportRelationships(modelBuilder);
    }

    /// <summary>
    /// Creates a soft delete filter for entities that inherit from AuditableEntity.
    /// </summary>
    private static System.Linq.Expressions.LambdaExpression GetSoftDeleteFilter<TEntity>()
        where TEntity : AuditableEntity
    {
        System.Linq.Expressions.Expression<Func<TEntity, bool>> filter = e => !e.IsDeleted;
        return filter;
    }

    /// <summary>
    /// Includes soft-deleted entities in queries. Use sparingly and only when necessary.
    /// </summary>
    public IQueryable<T> IncludeDeleted<T>() where T : AuditableEntity
    {
        return Set<T>().IgnoreQueryFilters();
    }

    /// <summary>
    /// Saves changes with automatic audit tracking for auditable entities.
    /// Sales entities (SaleSession, SaleItem, SalePayment, SessionNote) are excluded from automatic audit tracking
    /// to prevent DbUpdateConcurrencyException caused by ChangeTracker conflicts during high-frequency operations.
    /// These entities use manual audit logging in SaleSessionService for better control and clarity.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var auditEntries = new List<EntityChangeLog>();

        var auditableEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Where(e => !ExcludedFromAutomaticAudit.Contains(e.Entity.GetType()))
            .ToList();

        foreach (var entry in auditableEntries)
        {
            var entity = entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.CreatedBy = currentUser;
                    entity.IsActive = true;
                    break;

                case EntityState.Modified:
                    entity.ModifiedAt = DateTime.UtcNow;
                    entity.ModifiedBy = currentUser;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.DeletedAt = DateTime.UtcNow;
                    entity.DeletedBy = currentUser;
                    break;
            }

            auditEntries.AddRange(CreateAuditEntries(entry, currentUser));
        }

        // Handle Sales entities CreatedBy/ModifiedBy without audit logs
        // Query once and reuse to avoid duplicate iteration
        var allModifiedEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        var salesEntries = allModifiedEntries
            .Where(e => ExcludedFromAutomaticAudit.Contains(e.Entity.GetType()))
            .ToList();

        var hasNonSalesChanges = allModifiedEntries.Any(e => !ExcludedFromAutomaticAudit.Contains(e.Entity.GetType()));

        // CRITICAL FIX: Only add audit logs when there are non-Sales entity changes
        // This prevents DbUpdateConcurrencyException when saving Sales entities
        if (auditEntries.Any() && hasNonSalesChanges)
        {
            EntityChangeLogs.AddRange(auditEntries);
        }

        foreach (var entry in salesEntries)
        {
            var entity = entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = currentUser;
                entity.IsActive = true;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.ModifiedAt = DateTime.UtcNow;
                entity.ModifiedBy = currentUser;
            }
        }

        // Single SaveChanges call saves all entities atomically
        // Audit logs are only created for non-Sales entities
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves changes with automatic audit tracking for auditable entities (synchronous).
    /// </summary>
    public override int SaveChanges()
    {
        return SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the current user from the HTTP context or returns system user.
    /// </summary>
    private string GetCurrentUser()
    {
        try
        {
            var httpContext = _httpContextAccessor?.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.FindFirst("username")?.Value ??
                       httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ??
                       "authenticated_user";
            }
        }
        catch
        {
            // Ignore errors when getting HTTP context (e.g., during migrations)
        }

        return "system";
    }

    /// <summary>
    /// Properties that are excluded from Insert audit logging because they either have a fixed
    /// default value on every insert (IsDeleted=false, IsActive=true), are null (ModifiedAt/By),
    /// or are infrastructure columns (RowVersion) whose binary representation adds noise.
    /// </summary>
    private static readonly HashSet<string> AuditInsertNoiseProperties = new(StringComparer.Ordinal)
    {
        "RowVersion",    // byte[] — binary, meaningless as a log value
        "IsDeleted",     // always false on insert — zero information
        "IsActive",      // always true on insert — zero information
        "DeletedAt",     // always null on insert
        "DeletedBy",     // always null on insert
        "ModifiedAt",    // always null on insert
        "ModifiedBy",    // always null on insert
    };

    private const int AuditValueMaxLength = 2000;

    /// <summary>
    /// Truncates a value string to the maximum allowed length for audit columns,
    /// appending an ellipsis marker when truncation occurs.
    /// </summary>
    private static string? TruncateAuditValue(string? value)
    {
        if (value == null || value.Length <= AuditValueMaxLength) return value;
        return string.Concat(value.AsSpan(0, AuditValueMaxLength - 3), "...");
    }

    /// <summary>
    /// Creates audit log entries for an entity change.
    /// Insert events exclude noise properties (see AuditInsertNoiseProperties).
    /// All values are truncated to 2000 characters.
    /// </summary>
    private List<EntityChangeLog> CreateAuditEntries(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry, string currentUser)
    {
        var auditEntries = new List<EntityChangeLog>();
        var entity = entry.Entity;
        var entityName = entity.GetType().Name;
        var entityId = entity.Id;
        var tenantId = entity.TenantId == Guid.Empty ? (Guid?)null : entity.TenantId;
        var changedAt = DateTime.UtcNow;

        string operationType = entry.State switch
        {
            EntityState.Added => "Insert",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Unknown"
        };

        if (entry.State == EntityState.Added)
        {
            // For new entities log only meaningful business properties (skip noise defaults).
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue == null) continue;
                if (AuditInsertNoiseProperties.Contains(property.Metadata.Name)) continue;

                auditEntries.Add(new EntityChangeLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    TenantId = tenantId,
                    PropertyName = property.Metadata.Name,
                    OperationType = operationType,
                    OldValue = null,
                    NewValue = TruncateAuditValue(property.CurrentValue.ToString()),
                    ChangedBy = currentUser,
                    ChangedAt = changedAt
                });
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            // For modified entities log only actually changed properties.
            foreach (var property in entry.Properties)
            {
                if (!property.IsModified) continue;

                auditEntries.Add(new EntityChangeLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    TenantId = tenantId,
                    PropertyName = property.Metadata.Name,
                    OperationType = operationType,
                    OldValue = TruncateAuditValue(property.OriginalValue?.ToString()),
                    NewValue = TruncateAuditValue(property.CurrentValue?.ToString()),
                    ChangedBy = currentUser,
                    ChangedAt = changedAt
                });
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            // For deleted entities log a single deletion record.
            auditEntries.Add(new EntityChangeLog
            {
                EntityName = entityName,
                EntityId = entityId,
                TenantId = tenantId,
                PropertyName = "Entity",
                OperationType = operationType,
                OldValue = "Active",
                NewValue = "Deleted",
                ChangedBy = currentUser,
                ChangedAt = changedAt
            });
        }

        return auditEntries;
    }

    // ── Fiscal printing configuration ─────────────────────────────────────────

    private static void ConfigureFiscalPrintingRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Data.Entities.FiscalPrinting.DailyClosureRecord>(entity =>
        {
            entity.ToTable("DailyClosureRecords");

            entity.HasIndex(e => e.PrinterId);
            entity.HasIndex(e => e.ClosedAt);
            entity.HasIndex(e => new { e.PrinterId, e.ClosedAt });

            entity.HasOne(e => e.Printer)
                .WithMany()
                .HasForeignKey(e => e.PrinterId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
