﻿using EventForge.Server.Data.Entities;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Data.Entities.Notifications;
using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Data.Entities.Warehouse;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

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
    public DbSet<UM> UMs { get; set; }
    public DbSet<VatRate> VatRates { get; set; }
    public DbSet<VatNature> VatNatures { get; set; }

    // Business Entities
    public DbSet<BusinessParty> BusinessParties { get; set; }
    public DbSet<BusinessPartyAccounting> BusinessPartyAccountings { get; set; }
    public DbSet<PaymentTerm> PaymentTerms { get; set; }

    // Document Entities
    public DbSet<DocumentType> DocumentTypes { get; set; }
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

    // Event & Team Entities
    public DbSet<Event> Events { get; set; }
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

    // Price List & Promotion Entities
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListEntry> PriceListEntries { get; set; }
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

    // Station Monitor Entities
    public DbSet<Station> Stations { get; set; }
    public DbSet<StationOrderQueueItem> StationOrderQueueItems { get; set; }

    // Store Entities
    public DbSet<StorePos> StorePoses { get; set; }
    public DbSet<StoreUser> StoreUsers { get; set; }
    public DbSet<StoreUserGroup> StoreUserGroups { get; set; }
    public DbSet<StoreUserPrivilege> StoreUserPrivileges { get; set; }

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

    // System Configuration Entities
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<BackupOperation> BackupOperations { get; set; }

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

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDecimalPrecision(modelBuilder);
        ConfigureGlobalQueryFilters(modelBuilder);
        ConfigureDocumentRelationships(modelBuilder);
        ConfigureProductAndPriceListRelationships(modelBuilder);
        ConfigureStoreAndTeamRelationships(modelBuilder);
        ConfigureSalesAndOtherRelationships(modelBuilder);
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
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.AmountPaid).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.BaseCurrencyAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.ExchangeRate).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscount).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscountAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalGrossAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalNetAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentHeader>().Property(x => x.VatAmount).HasPrecision(18,6);

        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.UnitPrice).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.Quantity).HasPrecision(18,6);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.LineDiscount).HasPrecision(5,2);
        _ = modelBuilder.Entity<DocumentRow>().Property(x => x.VatRate).HasPrecision(5,2);

        _ = modelBuilder.Entity<PriceListEntry>().Property(x => x.Price).HasPrecision(18,6);
        _ = modelBuilder.Entity<Product>().Property(x => x.DefaultPrice).HasPrecision(18,6);
        _ = modelBuilder.Entity<Promotion>().Property(x => x.MinOrderAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountPercentage).HasPrecision(5,2);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.FixedPrice).HasPrecision(18,6);
        _ = modelBuilder.Entity<PromotionRule>().Property(x => x.MinOrderAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<VatRate>().Property(x => x.Percentage).HasPrecision(5,2);

        _ = modelBuilder.Entity<SaleSession>().Property(x => x.OriginalTotal).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.DiscountAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.FinalTotal).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleSession>().Property(x => x.TaxAmount).HasPrecision(18,6);

        _ = modelBuilder.Entity<SaleItem>().Property(x => x.UnitPrice).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.Quantity).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.DiscountPercent).HasPrecision(5,2);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TaxRate).HasPrecision(5,2);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TaxAmount).HasPrecision(18,6);
        _ = modelBuilder.Entity<SaleItem>().Property(x => x.TotalAmount).HasPrecision(18,6);

        _ = modelBuilder.Entity<SalePayment>().Property(x => x.Amount).HasPrecision(18,6);

        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.Quantity).HasPrecision(18,6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.WeightKg).HasPrecision(18,6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.DisposalCost).HasPrecision(18,6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.RecoveryValue).HasPrecision(18,6);
        _ = modelBuilder.Entity<WasteManagementRecord>().Property(x => x.RecyclingRatePercentage).HasPrecision(5,2);
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

    private static void ConfigureDocumentRelationships(ModelBuilder modelBuilder)
    {
        // DocumentHeader → BusinessParty
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessParty)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyId);

        // DocumentHeader → Address (BusinessPartyAddress)
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessPartyAddress)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyAddressId);

        // DocumentSummaryLink → DocumentHeader (Summary and Detailed)
        _ = modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.SummaryDocument)
            .WithMany(h => h.SummaryDocuments)
            .HasForeignKey(l => l.SummaryDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.DetailedDocument)
            .WithMany(h => h.IncludedInSummaries)
            .HasForeignKey("DetailedDocumentId")
            .OnDelete(DeleteBehavior.Restrict);

        // Document header current workflow execution (optional navigation)
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(dh => dh.CurrentWorkflowExecution)
            .WithMany() // current workflow execution is a single reference, no inverse collection
            .HasForeignKey(dh => dh.CurrentWorkflowExecutionId)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentHeader>()
            .HasIndex(dh => dh.CurrentWorkflowExecutionId)
            .HasDatabaseName("IX_DocumentHeaders_CurrentWorkflowExecutionId");

        // DocumentWorkflowExecution → DocumentHeader (history)
        _ = modelBuilder.Entity<DocumentWorkflowExecution>()
            .HasOne(dwe => dwe.DocumentHeader)
            .WithMany(dh => dh.WorkflowExecutions)
            .HasForeignKey(dwe => dwe.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<DocumentWorkflowExecution>()
            .HasIndex(dwe => dwe.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentWorkflowExecutions_DocumentHeaderId");

        // DocumentVersion and reminders/schedules
        _ = modelBuilder.Entity<DocumentVersion>()
            .HasOne(dv => dv.DocumentHeader)
            .WithMany(dh => dh.Versions)
            .HasForeignKey(dv => dv.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<DocumentVersion>()
            .HasIndex(dv => dv.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentVersions_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentReminder>()
            .HasOne(dr => dr.DocumentHeader)
            .WithMany(dh => dh.Reminders)
            .HasForeignKey(dr => dr.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<DocumentReminder>()
            .HasIndex(dr => dr.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentReminders_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasOne(ds => ds.DocumentHeader)
            .WithMany(dh => dh.Schedules)
            .HasForeignKey(ds => ds.DocumentHeaderId)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasOne(ds => ds.DocumentType)
            .WithMany()
            .HasForeignKey(ds => ds.DocumentTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasIndex(ds => ds.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentSchedules_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasIndex(ds => ds.NextExecutionDate)
            .HasDatabaseName("IX_DocumentSchedules_NextExecutionDate");

        // DocumentAccessLog → DocumentHeader: optional to avoid global filter conflicts
        _ = modelBuilder.Entity<DocumentAccessLog>()
            .HasOne(dal => dal.DocumentHeader)
            .WithMany()
            .HasForeignKey(dal => dal.DocumentHeaderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentAccessLog>()
            .HasIndex(dal => dal.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentAccessLogs_DocumentHeaderId");

        // DocumentReference: ignore polymorphic navigation to keep model simple
        _ = modelBuilder.Entity<DocumentReference>()
            .Ignore(d => d.Team)
            .Ignore(d => d.TeamMember);

        // MembershipCard → DocumentReference
        _ = modelBuilder.Entity<MembershipCard>()
            .HasOne(mc => mc.DocumentReference)
            .WithMany()
            .HasForeignKey(mc => mc.DocumentReferenceId)
            .OnDelete(DeleteBehavior.SetNull);
    }

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

        _ = modelBuilder.Entity<ProductSupplier>().Property(ps => ps.UnitCost).HasPrecision(18,6);
        _ = modelBuilder.Entity<ProductSupplier>().Property(ps => ps.LastPurchasePrice).HasPrecision(18,6);

        _ = modelBuilder.Entity<Product>().HasIndex(p => p.BrandId).HasDatabaseName("IX_Product_BrandId");
        _ = modelBuilder.Entity<Product>().HasIndex(p => p.ModelId).HasDatabaseName("IX_Product_ModelId");
        _ = modelBuilder.Entity<Product>().HasIndex(p => p.PreferredSupplierId).HasDatabaseName("IX_Product_PreferredSupplierId");

        _ = modelBuilder.Entity<Product>().Property(p => p.ReorderPoint).HasPrecision(18,6);
        _ = modelBuilder.Entity<Product>().Property(p => p.SafetyStock).HasPrecision(18,6);
        _ = modelBuilder.Entity<Product>().Property(p => p.TargetStockLevel).HasPrecision(18,6);
        _ = modelBuilder.Entity<Product>().Property(p => p.AverageDailyDemand).HasPrecision(18,6);

        _ = modelBuilder.Entity<Product>()
            .HasOne(p => p.ImageDocument)
            .WithMany()
            .HasForeignKey(p => p.ImageDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<Product>().HasIndex(p => p.ImageDocumentId).HasDatabaseName("IX_Product_ImageDocumentId");
    }

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

    private static void ConfigureSalesAndOtherRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<StorageLocation>()
            .HasOne(l => l.Warehouse)
            .WithMany(f => f.Locations)
            .HasForeignKey(l => l.WarehouseId);

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

        _ = modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Username, u.TenantId })
            .IsUnique();

        _ = modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique();

        _ = modelBuilder.Entity<User>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<LoginAudit>()
            .HasOne(la => la.User)
            .WithMany(u => u.LoginAudits)
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<AdminTenant>()
            .HasOne(at => at.User)
            .WithMany(u => u.AdminTenants)
            .HasForeignKey(at => at.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<AdminTenant>()
            .HasOne(at => at.ManagedTenant)
            .WithMany(t => t.AdminTenants)
            .HasForeignKey(at => at.ManagedTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<AdminTenant>()
            .HasIndex(at => new { at.UserId, at.ManagedTenantId })
            .IsUnique();

        _ = modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.PerformedByUser)
            .WithMany(u => u.PerformedAuditTrails)
            .HasForeignKey(at => at.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.SourceTenant)
            .WithMany()
            .HasForeignKey(at => at.SourceTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.TargetTenant)
            .WithMany()
            .HasForeignKey(at => at.TargetTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.TargetUser)
            .WithMany(u => u.TargetedAuditTrails)
            .HasForeignKey(at => at.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<NotificationRecipient>()
            .HasOne(nr => nr.Notification)
            .WithMany(n => n.Recipients)
            .HasForeignKey(nr => nr.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(nr => nr.UserId)
            .HasDatabaseName("IX_NotificationRecipients_UserId");

        _ = modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Members)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ChatMember>()
            .HasIndex(cm => cm.UserId)
            .HasDatabaseName("IX_ChatMembers_UserId");

        _ = modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Messages)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.SenderId)
            .HasDatabaseName("IX_ChatMessages_SenderId");

        _ = modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ReplyToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(cm => cm.ReplyToMessageId)
            .OnDelete(DeleteBehavior.NoAction);

        _ = modelBuilder.Entity<MessageAttachment>()
            .HasOne(ma => ma.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ma => ma.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<MessageReadReceipt>()
            .HasOne(mrr => mrr.Message)
            .WithMany(m => m.ReadReceipts)
            .HasForeignKey(mrr => mrr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<MessageReadReceipt>()
            .HasIndex(mrr => new { mrr.MessageId, mrr.UserId })
            .IsUnique();
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
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var auditEntries = new List<EntityChangeLog>();

        var auditableEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
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
                    // TODO: Set TenantId from ITenantContext when implemented
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

        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Any())
        {
            EntityChangeLogs.AddRange(auditEntries);
            _ = await base.SaveChangesAsync(cancellationToken);
        }

        return result;
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
    /// Creates audit log entries for an entity change.
    /// </summary>
    private List<EntityChangeLog> CreateAuditEntries(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry, string currentUser)
    {
        var auditEntries = new List<EntityChangeLog>();
        var entity = entry.Entity;
        var entityName = entity.GetType().Name;
        var entityId = entity.Id;

        string operationType = entry.State switch
        {
            EntityState.Added => "Insert",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Unknown"
        };

        if (entry.State == EntityState.Added)
        {
            // For new entities, log all properties
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue != null)
                {
                    auditEntries.Add(new EntityChangeLog
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        PropertyName = property.Metadata.Name,
                        OperationType = operationType,
                        OldValue = null,
                        NewValue = property.CurrentValue.ToString(),
                        ChangedBy = currentUser,
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            // For modified entities, log only changed properties
            foreach (var property in entry.Properties)
            {
                if (property.IsModified)
                {
                    auditEntries.Add(new EntityChangeLog
                    {
                        EntityName = entityName,
                        EntityId = entityId,
                        PropertyName = property.Metadata.Name,
                        OperationType = operationType,
                        OldValue = property.OriginalValue?.ToString(),
                        NewValue = property.CurrentValue?.ToString(),
                        ChangedBy = currentUser,
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            // For deleted entities, log the deletion
            auditEntries.Add(new EntityChangeLog
            {
                EntityName = entityName,
                EntityId = entityId,
                PropertyName = "Entity",
                OperationType = operationType,
                OldValue = "Active",
                NewValue = "Deleted",
                ChangedBy = currentUser,
                ChangedAt = DateTime.UtcNow
            });
        }

        return auditEntries;
    }
}