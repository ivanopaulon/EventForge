using Prym.Server.Services.Auth.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Auth;

/// <summary>
/// Service for bootstrapping the application with initial admin user and permissions.
/// </summary>
public interface IBootstrapService
{
    /// <summary>
    /// Ensures the database has initial admin user and permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if bootstrap was successful</returns>
    Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds default roles and permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SeedDefaultRolesAndPermissionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Bootstrap configuration options.
/// </summary>
public class BootstrapOptions
{
    /// <summary>
    /// Default admin username.
    /// </summary>
    public string DefaultAdminUsername { get; set; } = "superadmin";

    /// <summary>
    /// Default admin email.
    /// </summary>
    public string DefaultAdminEmail { get; set; } = "superadmin@localhost";

    /// <summary>
    /// Default admin password.
    /// </summary>
    public string DefaultAdminPassword { get; set; } = "SuperAdmin#2025!";

    /// <summary>
    /// Auto-create admin on startup.
    /// </summary>
    public bool AutoCreateAdmin { get; set; } = true;
}

/// <summary>
/// Orchestrates the bootstrap process using specialized seeder services.
/// Coordinates tenant, user, license, and entity seeding operations.
/// </summary>
public class BootstrapService(
    PrymDbContext dbContext,
    IUserSeeder userSeeder,
    ITenantSeeder tenantSeeder,
    ILicenseSeeder licenseSeeder,
    IEntitySeeder entitySeeder,
    IStoreSeeder storeSeeder,
    ILogger<BootstrapService> logger) : IBootstrapService
{

    public async Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure system tenant exists first (required for system-level entities)
            var systemTenant = await tenantSeeder.EnsureSystemTenantAsync(cancellationToken);
            if (systemTenant is null)
            {
                logger.LogError("Failed to ensure system tenant");
                return false;
            }

            // Seed/update default roles and permissions using dedicated seeder
            if (!await RolePermissionSeeder.SeedAsync(dbContext, logger, cancellationToken))
            {
                logger.LogError("Failed to seed default roles and permissions");
                return false;
            }

            // Always ensure SuperAdmin license is up to date
            var superAdminLicense = await licenseSeeder.EnsureSuperAdminLicenseAsync(cancellationToken);
            if (superAdminLicense is null)
            {
                logger.LogError("Failed to ensure SuperAdmin license");
                return false;
            }

            // Ensure FeatureTemplates are seeded
            if (!await EnsureFeatureTemplatesSeededAsync(cancellationToken))
            {
                logger.LogError("Failed to seed feature templates");
                return false;
            }

            // Check if SuperAdmin user already exists
            var superAdminUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == "superadmin" || u.Email == "superadmin@localhost", cancellationToken);

            // Get existing non-system tenants
            var existingTenants = await dbContext.Tenants
                .Where(t => t.Id != Guid.Empty) // Skip system-level tenant
                .ToListAsync(cancellationToken);

            // Determine if we need to create default tenant and users
            Tenant? defaultTenant = null;

            if (superAdminUser is null)
            {
                // Check if a default tenant already exists
                defaultTenant = existingTenants.FirstOrDefault(t => t.Code == "default");

                if (defaultTenant is null)
                {
                    defaultTenant = await tenantSeeder.CreateDefaultTenantAsync(cancellationToken);
                    if (defaultTenant is null)
                    {
                        logger.LogError("Failed to create default tenant");
                        return false;
                    }

                    // Add to existingTenants list for later processing
                    existingTenants.Add(defaultTenant);
                }
                else
                {
                    // Default tenant already exists
                }

                // Assign SuperAdmin license to default tenant
                if (!await licenseSeeder.AssignLicenseToTenantAsync(defaultTenant.Id, superAdminLicense.Id, cancellationToken))
                {
                    logger.LogError("Failed to assign SuperAdmin license to default tenant");
                    return false;
                }

                // Create SuperAdmin user
                superAdminUser = await userSeeder.CreateSuperAdminUserAsync(defaultTenant.Id, cancellationToken);
                if (superAdminUser is null)
                {
                    logger.LogError("Failed to create SuperAdmin user");
                    return false;
                }

                // Create default Manager user
                var managerUser = await userSeeder.CreateDefaultManagerUserAsync(defaultTenant.Id, cancellationToken);
                if (managerUser is null)
                {
                    logger.LogWarning("Failed to create default Manager user");
                    // Not fatal
                }

                // Create AdminTenant record
                if (!await tenantSeeder.CreateAdminTenantRecordAsync(superAdminUser.Id, defaultTenant.Id, cancellationToken))
                {
                    logger.LogError("Failed to create AdminTenant record");
                    return false;
                }

                logger.LogWarning("SECURITY: Please change the SuperAdmin and Manager passwords immediately after first login!");
            }
            else
            {
                // SuperAdmin already exists
            }

            // Ensure all tenants have base entities seeded
            if (existingTenants.Any())
            {

                // Get all tenant IDs for batch query
                var tenantIds = existingTenants.Select(t => t.Id).ToList();

                // Batch query to check which tenants have base entities (more efficient than individual queries)
                var tenantsWithVatNatures = await dbContext.VatNatures
                    .Where(v => tenantIds.Contains(v.TenantId))
                    .Select(v => v.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithVatRates = await dbContext.VatRates
                    .Where(v => tenantIds.Contains(v.TenantId))
                    .Select(v => v.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithUnitsMeasure = await dbContext.UMs
                    .Where(u => tenantIds.Contains(u.TenantId))
                    .Select(u => u.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithWarehouses = await dbContext.StorageFacilities
                    .Where(w => tenantIds.Contains(w.TenantId))
                    .Select(w => w.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithPaymentMethods = await dbContext.PaymentMethods
                    .Where(p => tenantIds.Contains(p.TenantId))
                    .Select(p => p.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Check each tenant to see if it needs base entities seeded
                foreach (var tenant in existingTenants)
                {
                    // Check if this tenant has base entities (using batch query results)
                    var hasVatNatures = tenantsWithVatNatures.Contains(tenant.Id);
                    var hasVatRates = tenantsWithVatRates.Contains(tenant.Id);
                    var hasUnitsMeasure = tenantsWithUnitsMeasure.Contains(tenant.Id);
                    var hasWarehouses = tenantsWithWarehouses.Contains(tenant.Id);

                    // If any base entities are missing, seed them
                    if (!hasVatNatures || !hasVatRates || !hasUnitsMeasure || !hasWarehouses)
                    {
                        logger.LogWarning("Tenant {TenantId} ({TenantName}) is missing base entities (VatNatures:{HasVat}, VatRates:{HasRates}, UMs:{HasUM}, Warehouses:{HasWH}). Seeding now...",
                            tenant.Id, tenant.Name, hasVatNatures, hasVatRates, hasUnitsMeasure, hasWarehouses);

                        if (!await entitySeeder.SeedTenantBaseEntitiesAsync(tenant.Id, cancellationToken))
                        {
                            logger.LogError("Failed to seed base entities for tenant {TenantId}", tenant.Id);
                        }
                        else
                        {
                            var (validationResult, validationIssues) = await entitySeeder.ValidateTenantBaseEntitiesAsync(tenant.Id, cancellationToken);
                            if (!validationResult)
                                logger.LogWarning("Validation issues for tenant {TenantId}: {Issues}", tenant.Id, string.Join("; ", validationIssues));
                        }
                    }

                    if (!tenantsWithPaymentMethods.Contains(tenant.Id))
                    {
                        if (!await storeSeeder.SeedStoreBaseEntitiesAsync(tenant.Id, cancellationToken))
                            logger.LogWarning("Failed to seed store base entities for tenant {TenantId}", tenant.Id);
                    }
                }
            }

            // Final validation for default tenant if it was just created
            if (defaultTenant is not null)
            {
                var (isValid, issues) = await entitySeeder.ValidateTenantBaseEntitiesAsync(defaultTenant.Id, cancellationToken);
                if (!isValid)
                {
                    logger.LogWarning("Base entities validation found issues for default tenant: {Issues}", string.Join("; ", issues));
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bootstrap process");
            return false;
        }
    }

    public async Task<bool> SeedDefaultRolesAndPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await RolePermissionSeeder.SeedAsync(dbContext, logger, cancellationToken);
    }

    /// <summary>
    /// Ensures FeatureTemplates catalog is seeded with default features.
    /// </summary>
    private async Task<bool> EnsureFeatureTemplatesSeededAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var featureTemplates = new[]
            {
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "BasicEventManagement",
                    DisplayName = "Basic Event Management",
                    Description = "Create and manage basic events",
                    Category = "Events",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 1,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "BasicTeamManagement",
                    DisplayName = "Basic Team Management",
                    Description = "Create and manage basic teams",
                    Category = "Teams",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 2,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "ProductManagement",
                    DisplayName = "Product Management",
                    Description = "Manage products, inventory, and pricing",
                    Category = "Products",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 3,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "DocumentManagement",
                    DisplayName = "Document Management",
                    Description = "Create and manage documents",
                    Category = "Documents",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 4,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "FinancialManagement",
                    DisplayName = "Financial Management",
                    Description = "Manage financial transactions and accounting",
                    Category = "Finance",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 5,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "AdvancedAnalytics",
                    DisplayName = "Advanced Analytics",
                    Description = "Access to advanced analytics and insights",
                    Category = "Analytics",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 6,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "ReportGeneration",
                    DisplayName = "Report Generation",
                    Description = "Generate custom reports",
                    Category = "Reports",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 7,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "SystemConfiguration",
                    DisplayName = "System Configuration",
                    Description = "Configure system settings",
                    Category = "System",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 8,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "UserManagement",
                    DisplayName = "User Management",
                    Description = "Manage users and their permissions",
                    Category = "Administration",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 9,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "TenantManagement",
                    DisplayName = "Tenant Management",
                    Description = "Manage tenants (SuperAdmin only)",
                    Category = "Administration",
                    MinimumTierLevel = 3,
                    IsAvailable = true,
                    SortOrder = 10,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "RetailManagement",
                    DisplayName = "Retail Management",
                    Description = "Manage retail operations",
                    Category = "Retail",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 11,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "StoreManagement",
                    DisplayName = "Store Management",
                    Description = "Manage multiple stores",
                    Category = "Retail",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 12,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "POSManagement",
                    DisplayName = "POS Management",
                    Description = "Point of Sale management",
                    Category = "Retail",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 13,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "PaymentProcessing",
                    DisplayName = "Payment Processing",
                    Description = "Process payments and transactions",
                    Category = "Finance",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 14,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "PrintingManagement",
                    DisplayName = "Printing Management",
                    Description = "Manage printing and receipt generation",
                    Category = "System",
                    MinimumTierLevel = 1,
                    IsAvailable = true,
                    SortOrder = 15,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new FeatureTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "APIIntegration",
                    DisplayName = "API Integration",
                    Description = "Access to API for integrations",
                    Category = "Integration",
                    MinimumTierLevel = 2,
                    IsAvailable = true,
                    SortOrder = 16,
                    TenantId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Add only missing features by name so new templates propagate to existing installs
            var existingNames = await dbContext.FeatureTemplates
                .Select(ft => ft.Name)
                .ToListAsync(cancellationToken);
            var existingNamesSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var templatesToAdd = featureTemplates.Where(ft => !existingNamesSet.Contains(ft.Name)).ToList();

            if (templatesToAdd.Count > 0)
            {
                logger.LogWarning("Seeding {Count} missing FeatureTemplates", templatesToAdd.Count);
                await dbContext.FeatureTemplates.AddRangeAsync(templatesToAdd, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding FeatureTemplates");
            return false;
        }
    }

}
