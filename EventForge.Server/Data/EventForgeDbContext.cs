using EventForge.Server.Data.Entities;
using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public class EventForgeDbContext : DbContext
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

    // Common
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Reference> References { get; set; }
    public DbSet<ClassificationNode> ClassificationNodes { get; set; }
    public DbSet<Printer> Printers { get; set; }
    public DbSet<UM> UMs { get; set; }
    public DbSet<VatRate> VatRates { get; set; }

    // Business
    public DbSet<BusinessParty> BusinessParties { get; set; }
    public DbSet<BusinessPartyAccounting> BusinessPartyAccountings { get; set; }
    public DbSet<PaymentTerm> PaymentTerms { get; set; }

    // Documents
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<DocumentHeader> DocumentHeaders { get; set; }
    public DbSet<DocumentRow> DocumentRows { get; set; }
    public DbSet<DocumentSummaryLink> DocumentSummaryLinks { get; set; }
    public DbSet<DocumentAttachment> DocumentAttachments { get; set; }

    // Events & Teams
    public DbSet<Event> Events { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }

    // Products
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCode> ProductCodes { get; set; }
    public DbSet<ProductUnit> ProductUnits { get; set; }
    public DbSet<ProductBundleItem> ProductBundleItems { get; set; }

    // Price Lists
    public DbSet<PriceList> PriceLists { get; set; }
    public DbSet<PriceListEntry> PriceListEntries { get; set; }

    // Promotions
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<PromotionRule> PromotionRules { get; set; }
    public DbSet<PromotionRuleProduct> PromotionRuleProducts { get; set; }

    // Warehouse
    public DbSet<StorageFacility> StorageFacilities { get; set; }
    public DbSet<StorageLocation> StorageLocations { get; set; }

    // Station Monitor
    public DbSet<Station> Stations { get; set; }
    public DbSet<StationOrderQueueItem> StationOrderQueueItems { get; set; }

    // Store
    public DbSet<StorePos> StorePoses { get; set; }
    public DbSet<StoreUser> StoreUsers { get; set; }
    public DbSet<StoreUserGroup> StoreUserGroups { get; set; }
    public DbSet<StoreUserPrivilege> StoreUserPrivileges { get; set; }

    // Audit
    public DbSet<EntityChangeLog> EntityChangeLogs { get; set; }

    // Authentication & Authorization
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<LoginAudit> LoginAudits { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AdminTenant> AdminTenants { get; set; }
    public DbSet<AuditTrail> AuditTrails { get; set; }

    // Licensing System
    public DbSet<License> Licenses { get; set; }
    public DbSet<LicenseFeature> LicenseFeatures { get; set; }
    public DbSet<LicenseFeaturePermission> LicenseFeaturePermissions { get; set; }
    public DbSet<TenantLicense> TenantLicenses { get; set; }

    // Configuration & System Management
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<BackupOperation> BackupOperations { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationRecipient> NotificationRecipients { get; set; }

    // Chat
    public DbSet<ChatThread> ChatThreads { get; set; }
    public DbSet<ChatMember> ChatMembers { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }
    public DbSet<MessageReadReceipt> MessageReadReceipts { get; set; }

    // Logging
    public DbSet<LogEntry> LogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure global query filters for soft delete and tenant isolation
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Apply soft delete filter to all entities that inherit from AuditableEntity
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(EventForgeDbContext)
                    .GetMethod(nameof(GetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.MakeGenericMethod(entityType.ClrType);

                var filter = method?.Invoke(null, new object[] { });
                if (filter != null)
                {
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter((System.Linq.Expressions.LambdaExpression)filter);
                }

                // TODO: Add tenant filtering when ITenantContext is implemented
                // For now, we'll add tenant filtering manually in services/repositories
            }
        }

        // DocumentHeader - importi e prezzi
        modelBuilder.Entity<DocumentHeader>().Property(x => x.AmountPaid).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.BaseCurrencyAmount).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.ExchangeRate).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscount).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalDiscountAmount).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalGrossAmount).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.TotalNetAmount).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentHeader>().Property(x => x.VatAmount).HasPrecision(18, 6);

        // DocumentRow - prezzi e quantità
        modelBuilder.Entity<DocumentRow>().Property(x => x.UnitPrice).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentRow>().Property(x => x.Quantity).HasPrecision(18, 6);
        modelBuilder.Entity<DocumentRow>().Property(x => x.LineDiscount).HasPrecision(5, 2); // percentuale
        modelBuilder.Entity<DocumentRow>().Property(x => x.VatRate).HasPrecision(5, 2); // percentuale

        // PriceListEntry - prezzi
        modelBuilder.Entity<PriceListEntry>().Property(x => x.Price).HasPrecision(18, 6);

        // Product - prezzi
        modelBuilder.Entity<Product>().Property(x => x.DefaultPrice).HasPrecision(18, 6);

        // Promotion - importi
        modelBuilder.Entity<Promotion>().Property(x => x.MinOrderAmount).HasPrecision(18, 6);

        // PromotionRule - importi e percentuali
        modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountAmount).HasPrecision(18, 6);
        modelBuilder.Entity<PromotionRule>().Property(x => x.DiscountPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<PromotionRule>().Property(x => x.FixedPrice).HasPrecision(18, 6);
        modelBuilder.Entity<PromotionRule>().Property(x => x.MinOrderAmount).HasPrecision(18, 6);

        // VatRate - percentuale
        modelBuilder.Entity<VatRate>().Property(x => x.Percentage).HasPrecision(5, 2);

        // DocumentHeader → BusinessParty
        modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessParty)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyId);

        // DocumentHeader → Address (BusinessPartyAddress)
        modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessPartyAddress)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyAddressId);

        // DocumentSummaryLink → DocumentHeader (Summary and Detailed)
        modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.SummaryDocument)
            .WithMany(h => h.SummaryDocuments)
            .HasForeignKey(l => l.SummaryDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.DetailedDocument)
            .WithMany(h => h.IncludedInSummaries)
            .HasForeignKey("DetailedDocumentId")
            .OnDelete(DeleteBehavior.Restrict);

        // Event → Team
        modelBuilder.Entity<Team>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Teams)
            .HasForeignKey(t => t.EventId);

        // Event → PriceList
        modelBuilder.Entity<PriceList>()
            .HasOne(p => p.Event)
            .WithMany(e => e.PriceLists)
            .HasForeignKey(p => p.EventId);

        // Team → TeamMember
        modelBuilder.Entity<TeamMember>()
            .HasOne(m => m.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(m => m.TeamId);

        // PriceList → PriceListEntry
        modelBuilder.Entity<PriceListEntry>()
            .HasOne(e => e.PriceList)
            .WithMany(l => l.ProductPrices)
            .HasForeignKey(e => e.PriceListId);

        // Product → ProductUnit
        modelBuilder.Entity<ProductUnit>()
            .HasOne(u => u.Product)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.ProductId);

        // Product → ProductCode
        modelBuilder.Entity<ProductCode>()
            .HasOne(c => c.Product)
            .WithMany(p => p.Codes)
            .HasForeignKey(c => c.ProductId);

        // ProductBundleItem (BundleProductId, ComponentProductId)
        modelBuilder.Entity<ProductBundleItem>()
            .HasOne(b => b.BundleProduct)
            .WithMany(p => p.BundleItems)
            .HasForeignKey(b => b.BundleProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductBundleItem>()
            .HasOne(b => b.ComponentProduct)
            .WithMany(p => p.IncludedInBundles)
            .HasForeignKey(b => b.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // StoreUserGroup ↔ StoreUserPrivilege (many-to-many)
        modelBuilder.Entity<StoreUserGroup>()
            .HasMany(g => g.Privileges)
            .WithMany(p => p.Groups);

        // StoreUserGroup → StoreUser
        modelBuilder.Entity<StoreUser>()
            .HasOne(u => u.CashierGroup)
            .WithMany(g => g.Cashiers)
            .HasForeignKey(u => u.CashierGroupId);

        // Station → Printer
        modelBuilder.Entity<Printer>()
            .HasOne(p => p.Station)
            .WithMany(s => s.Printers)
            .HasForeignKey(p => p.StationId);

        // StorageFacility → StorageLocation
        modelBuilder.Entity<StorageLocation>()
            .HasOne(l => l.Warehouse)
            .WithMany(f => f.Locations)
            .HasForeignKey(l => l.WarehouseId);

        // Promotion → PromotionRule
        modelBuilder.Entity<PromotionRule>()
            .HasOne(r => r.Promotion)
            .WithMany(p => p.Rules)
            .HasForeignKey(r => r.PromotionId);

        // PromotionRule → PromotionRuleProduct
        modelBuilder.Entity<PromotionRuleProduct>()
            .HasOne(rp => rp.PromotionRule)
            .WithMany(r => r.Products)
            .HasForeignKey(rp => rp.PromotionRuleId);

        // PromotionRuleProduct → Product
        modelBuilder.Entity<PromotionRuleProduct>()
            .HasOne(rp => rp.Product)
            .WithMany()
            .HasForeignKey(rp => rp.ProductId);

        // Reference → Contact (polymorphic, managed at application/query level)
        // Address, Contact, Reference: polimorphic, no classic FK

        // Bank → Address, Contact, Reference (polymorphic, managed at application/query level)
        // BusinessParty → Address, Contact, Reference (polymorphic, managed at application/query level)

        // Authentication & Authorization relationships

        // User constraints - updated for tenant-aware uniqueness
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Username, u.TenantId })
            .IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.Email, u.TenantId })
            .IsUnique();

        // User -> Tenant relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Tenant)
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Role constraints
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // Permission constraints
        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.Category, p.Resource, p.Action })
            .IsUnique();

        // Authentication & Authorization Relationships
        // Configured to prevent cascade delete cycles that are prohibited in SQL Server

        // UserRole relationships - using Cascade for UserRole deletion is safe
        // because UserRole is a junction table and doesn't create cycles
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // RolePermission relationships - using Cascade for RolePermission deletion is safe
        // because RolePermission is a junction table and doesn't create cycles
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // LoginAudit relationships - using SetNull to preserve audit history 
        // even if the user is deleted (which should be rare due to soft delete)
        modelBuilder.Entity<LoginAudit>()
            .HasOne(la => la.User)
            .WithMany(u => u.LoginAudits)
            .HasForeignKey(la => la.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Multi-tenancy entity configurations

        // Tenant constraints
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Code)
            .IsUnique();

        // AdminTenant relationships - manages which users can administer which tenants
        // Cascade delete on User to remove admin privileges when user is deleted
        // Restrict delete on Tenant to prevent accidental tenant deletion if admins exist
        modelBuilder.Entity<AdminTenant>()
            .HasOne(at => at.User)
            .WithMany(u => u.AdminTenants)
            .HasForeignKey(at => at.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AdminTenant>()
            .HasOne(at => at.ManagedTenant)
            .WithMany(t => t.AdminTenants)
            .HasForeignKey(at => at.ManagedTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // AdminTenant composite index for uniqueness
        modelBuilder.Entity<AdminTenant>()
            .HasIndex(at => new { at.UserId, at.ManagedTenantId })
            .IsUnique();

        // AuditTrail relationships - preserve audit history integrity
        // All audit relationships use Restrict to prevent loss of audit history
        modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.PerformedByUser)
            .WithMany(u => u.PerformedAuditTrails)
            .HasForeignKey(at => at.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.SourceTenant)
            .WithMany()
            .HasForeignKey(at => at.SourceTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.TargetTenant)
            .WithMany()
            .HasForeignKey(at => at.TargetTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditTrail>()
            .HasOne(at => at.TargetUser)
            .WithMany(u => u.TargetedAuditTrails)
            .HasForeignKey(at => at.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification relationships
        modelBuilder.Entity<NotificationRecipient>()
            .HasOne(nr => nr.Notification)
            .WithMany(n => n.Recipients)
            .HasForeignKey(nr => nr.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: NotificationRecipient.UserId references User but no FK is configured
        // This is intentional to avoid cascade delete cycles since User entities
        // should not be deleted when notifications are deleted
        modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(nr => nr.UserId)
            .HasDatabaseName("IX_NotificationRecipients_UserId");

        // Chat relationships
        modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Members)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: ChatMember.UserId references User but no FK is configured
        // This is intentional to avoid cascade delete cycles with User entity
        modelBuilder.Entity<ChatMember>()
            .HasIndex(cm => cm.UserId)
            .HasDatabaseName("IX_ChatMembers_UserId");

        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Messages)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Restrict);

        // Note: ChatMessage.SenderId references User but no FK is configured
        // This is intentional to avoid cascade delete cycles with User entity
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.SenderId)
            .HasDatabaseName("IX_ChatMessages_SenderId");

        // Self-referential relationship for message replies
        // Using SetNull to prevent cascade delete cycles when parent messages are deleted
        // This ensures that when a message is deleted, its replies remain but lose the reference
        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ReplyToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(cm => cm.ReplyToMessageId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<MessageAttachment>()
            .HasOne(ma => ma.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ma => ma.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MessageReadReceipt>()
            .HasOne(mrr => mrr.Message)
            .WithMany(m => m.ReadReceipts)
            .HasForeignKey(mrr => mrr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: MessageReadReceipt.UserId references User but no FK is configured
        // This is intentional to avoid cascade delete cycles with User entity
        modelBuilder.Entity<MessageReadReceipt>()
            .HasIndex(mrr => mrr.UserId)
            .HasDatabaseName("IX_MessageReadReceipts_UserId");

        // Configuration & System Management relationships
        // BackupOperation references User but doesn't use FK constraint to avoid cascade cycles
        // Using index only for performance while preserving referential flexibility
        modelBuilder.Entity<BackupOperation>()
            .HasIndex(bo => bo.StartedByUserId)
            .HasDatabaseName("IX_BackupOperations_StartedByUserId");

        // Unique constraints for chat and notification entities
        modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(nr => new { nr.NotificationId, nr.UserId })
            .IsUnique();

        modelBuilder.Entity<ChatMember>()
            .HasIndex(cm => new { cm.ChatThreadId, cm.UserId })
            .IsUnique();

        modelBuilder.Entity<MessageReadReceipt>()
            .HasIndex(mrr => new { mrr.MessageId, mrr.UserId })
            .IsUnique();

        // Licensing System relationships

        // License constraints
        modelBuilder.Entity<License>()
            .HasIndex(l => l.Name)
            .IsUnique();

        // LicenseFeature relationships
        modelBuilder.Entity<LicenseFeature>()
            .HasOne(lf => lf.License)
            .WithMany(l => l.LicenseFeatures)
            .HasForeignKey(lf => lf.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseFeature constraints
        modelBuilder.Entity<LicenseFeature>()
            .HasIndex(lf => new { lf.LicenseId, lf.Name })
            .IsUnique();

        // LicenseFeaturePermission relationships
        modelBuilder.Entity<LicenseFeaturePermission>()
            .HasOne(lfp => lfp.LicenseFeature)
            .WithMany(lf => lf.LicenseFeaturePermissions)
            .HasForeignKey(lfp => lfp.LicenseFeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LicenseFeaturePermission>()
            .HasOne(lfp => lfp.Permission)
            .WithMany(p => p.LicenseFeaturePermissions)
            .HasForeignKey(lfp => lfp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // LicenseFeaturePermission constraints
        modelBuilder.Entity<LicenseFeaturePermission>()
            .HasIndex(lfp => new { lfp.LicenseFeatureId, lfp.PermissionId })
            .IsUnique();

        // TenantLicense relationships
        modelBuilder.Entity<TenantLicense>()
            .HasOne(tl => tl.Tenant)
            .WithMany(t => t.TenantLicenses)
            .HasForeignKey(tl => tl.TargetTenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TenantLicense>()
            .HasOne(tl => tl.License)
            .WithMany(l => l.TenantLicenses)
            .HasForeignKey(tl => tl.LicenseId)
            .OnDelete(DeleteBehavior.Restrict);

        // TenantLicense constraints - only one active license per tenant
        modelBuilder.Entity<TenantLicense>()
            .HasIndex(tl => new { tl.TargetTenantId, tl.IsAssignmentActive })
            .IsUnique()
            .HasFilter("[IsAssignmentActive] = 1");

        // LogEntry configuration for Serilog logs table
        modelBuilder.Entity<LogEntry>(entity =>
        {
            entity.ToTable("Logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TimeStamp).IsRequired();
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Exception);
            entity.Property(e => e.MachineName).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(100);
        });
    }

    /// <summary>
    /// Creates a soft delete filter for entities that inherit from AuditableEntity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>Lambda expression for the filter</returns>
    private static System.Linq.Expressions.LambdaExpression GetSoftDeleteFilter<TEntity>()
        where TEntity : AuditableEntity
    {
        System.Linq.Expressions.Expression<Func<TEntity, bool>> filter = e => !e.IsDeleted;
        return filter;
    }

    /// <summary>
    /// Includes soft-deleted entities in queries. Use sparingly and only when necessary.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <returns>Queryable including soft-deleted entities</returns>
    public IQueryable<T> IncludeDeleted<T>() where T : AuditableEntity
    {
        return Set<T>().IgnoreQueryFilters();
    }

    /// <summary>
    /// Saves changes with automatic audit tracking for auditable entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities written to the database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var auditEntries = new List<EntityChangeLog>();

        // Process auditable entities before saving
        var auditableEntries = ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in auditableEntries)
        {
            var entity = entry.Entity;
            var entityName = entity.GetType().Name;
            var entityId = entity.Id;

            // Update audit fields
            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.CreatedBy = currentUser;
                    entity.IsActive = true;
                    // TODO: Set TenantId from ITenantContext when implemented
                    // For now, TenantId must be set explicitly by the service layer
                    break;

                case EntityState.Modified:
                    entity.ModifiedAt = DateTime.UtcNow;
                    entity.ModifiedBy = currentUser;
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entity.IsDeleted = true;
                    entity.DeletedAt = DateTime.UtcNow;
                    entity.DeletedBy = currentUser;
                    break;
            }

            // Generate audit log entries
            auditEntries.AddRange(CreateAuditEntries(entry, currentUser));
        }

        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);

        // Then save audit entries (to avoid self-referencing issues)
        if (auditEntries.Any())
        {
            EntityChangeLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Saves changes with automatic audit tracking for auditable entities (synchronous).
    /// </summary>
    /// <returns>Number of entities written to the database</returns>
    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the current user from the HTTP context or returns system user.
    /// </summary>
    /// <returns>Current user identifier</returns>
    private string GetCurrentUser()
    {
        // Try to get user from HTTP context
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
    /// <param name="entry">The entity entry</param>
    /// <param name="currentUser">Current user</param>
    /// <returns>List of audit log entries</returns>
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