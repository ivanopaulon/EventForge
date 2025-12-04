using EventForge.Server.Services.Auth.Seeders;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth;

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
public class BootstrapService : IBootstrapService
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IUserSeeder _userSeeder;
    private readonly ITenantSeeder _tenantSeeder;
    private readonly ILicenseSeeder _licenseSeeder;
    private readonly IEntitySeeder _entitySeeder;
    private readonly IStoreSeeder _storeSeeder;
    private readonly ILogger<BootstrapService> _logger;

    public BootstrapService(
        EventForgeDbContext dbContext,
        IUserSeeder userSeeder,
        ITenantSeeder tenantSeeder,
        ILicenseSeeder licenseSeeder,
        IEntitySeeder entitySeeder,
        IStoreSeeder storeSeeder,
        ILogger<BootstrapService> logger)
    {
        _dbContext = dbContext;
        _userSeeder = userSeeder;
        _tenantSeeder = tenantSeeder;
        _licenseSeeder = licenseSeeder;
        _entitySeeder = entitySeeder;
        _storeSeeder = storeSeeder;
        _logger = logger;
    }

    public async Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting bootstrap process...");

            // Ensure database is created
            _ = await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

            // Ensure system tenant exists first (required for system-level entities)
            var systemTenant = await _tenantSeeder.EnsureSystemTenantAsync(cancellationToken);
            if (systemTenant == null)
            {
                _logger.LogError("Failed to ensure system tenant");
                return false;
            }

            // Seed/update default roles and permissions using dedicated seeder
            if (!await RolePermissionSeeder.SeedAsync(_dbContext, _logger, cancellationToken))
            {
                _logger.LogError("Failed to seed default roles and permissions");
                return false;
            }

            // Always ensure SuperAdmin license is up to date
            var superAdminLicense = await _licenseSeeder.EnsureSuperAdminLicenseAsync(cancellationToken);
            if (superAdminLicense == null)
            {
                _logger.LogError("Failed to ensure SuperAdmin license");
                return false;
            }

            // Check if SuperAdmin user already exists
            var superAdminUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == "superadmin" || u.Email == "superadmin@localhost", cancellationToken);

            // Get existing non-system tenants
            var existingTenants = await _dbContext.Tenants
                .Where(t => t.Id != Guid.Empty) // Skip system-level tenant
                .ToListAsync(cancellationToken);

            // Determine if we need to create default tenant and users
            Tenant? defaultTenant = null;

            if (superAdminUser == null)
            {
                _logger.LogInformation("SuperAdmin user not found. Proceeding with initial bootstrap...");

                // Check if a default tenant already exists
                defaultTenant = existingTenants.FirstOrDefault(t => t.Code == "default");

                if (defaultTenant == null)
                {
                    // Create default tenant
                    _logger.LogInformation("Creating default tenant...");
                    defaultTenant = await _tenantSeeder.CreateDefaultTenantAsync(cancellationToken);
                    if (defaultTenant == null)
                    {
                        _logger.LogError("Failed to create default tenant");
                        return false;
                    }

                    // Add to existingTenants list for later processing
                    existingTenants.Add(defaultTenant);
                }
                else
                {
                    _logger.LogInformation("Default tenant already exists: {TenantName} (Code: {TenantCode})",
                        defaultTenant.Name, defaultTenant.Code);
                }

                // Assign SuperAdmin license to default tenant
                if (!await _licenseSeeder.AssignLicenseToTenantAsync(defaultTenant.Id, superAdminLicense.Id, cancellationToken))
                {
                    _logger.LogError("Failed to assign SuperAdmin license to default tenant");
                    return false;
                }

                // Create SuperAdmin user
                superAdminUser = await _userSeeder.CreateSuperAdminUserAsync(defaultTenant.Id, cancellationToken);
                if (superAdminUser == null)
                {
                    _logger.LogError("Failed to create SuperAdmin user");
                    return false;
                }

                // Create default Manager user
                var managerUser = await _userSeeder.CreateDefaultManagerUserAsync(defaultTenant.Id, cancellationToken);
                if (managerUser == null)
                {
                    _logger.LogWarning("Failed to create default Manager user");
                    // Not fatal
                }

                // Create AdminTenant record
                if (!await _tenantSeeder.CreateAdminTenantRecordAsync(superAdminUser.Id, defaultTenant.Id, cancellationToken))
                {
                    _logger.LogError("Failed to create AdminTenant record");
                    return false;
                }

                _logger.LogInformation("SuperAdmin user and default tenant setup completed successfully");
            }
            else
            {
                _logger.LogInformation("SuperAdmin user already exists: {Username} ({Email})",
                    superAdminUser.Username, superAdminUser.Email);
            }

            // Now ensure all tenants have base entities seeded
            if (existingTenants.Any())
            {
                _logger.LogInformation("Checking {TenantCount} tenants for base entities...", existingTenants.Count);

                // Get all tenant IDs for batch query
                var tenantIds = existingTenants.Select(t => t.Id).ToList();

                // Batch query to check which tenants have base entities (more efficient than individual queries)
                var tenantsWithVatNatures = await _dbContext.VatNatures
                    .Where(v => tenantIds.Contains(v.TenantId))
                    .Select(v => v.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithVatRates = await _dbContext.VatRates
                    .Where(v => tenantIds.Contains(v.TenantId))
                    .Select(v => v.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithUnitsMeasure = await _dbContext.UMs
                    .Where(u => tenantIds.Contains(u.TenantId))
                    .Select(u => u.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithWarehouses = await _dbContext.StorageFacilities
                    .Where(w => tenantIds.Contains(w.TenantId))
                    .Select(w => w.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var tenantsWithPaymentMethods = await _dbContext.PaymentMethods
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
                        _logger.LogWarning("Tenant {TenantId} ({TenantName}) is missing base entities (VatNatures:{HasVat}, VatRates:{HasRates}, UMs:{HasUM}, Warehouses:{HasWH}). Seeding now...",
                            tenant.Id, tenant.Name, hasVatNatures, hasVatRates, hasUnitsMeasure, hasWarehouses);

                        if (!await _entitySeeder.SeedTenantBaseEntitiesAsync(tenant.Id, cancellationToken))
                        {
                            _logger.LogError("Failed to seed base entities for tenant {TenantId}", tenant.Id);
                            // Continue with other tenants instead of failing completely
                        }
                        else
                        {
                            _logger.LogInformation("Successfully seeded base entities for tenant {TenantId} ({TenantName})",
                                tenant.Id, tenant.Name);

                            // Validate the seeded entities
                            var (validationResult, validationIssues) = await _entitySeeder.ValidateTenantBaseEntitiesAsync(tenant.Id, cancellationToken);
                            if (!validationResult)
                            {
                                _logger.LogWarning("Validation found issues for tenant {TenantId}: {Issues}",
                                    tenant.Id, string.Join("; ", validationIssues));
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Tenant {TenantId} ({TenantName}) already has base entities seeded",
                            tenant.Id, tenant.Name);
                    }

                    // Check if tenant needs store entities seeded
                    var hasPaymentMethods = tenantsWithPaymentMethods.Contains(tenant.Id);

                    if (!hasPaymentMethods)
                    {
                        _logger.LogInformation("Tenant {TenantId} ({TenantName}) is missing store entities. Seeding now...",
                            tenant.Id, tenant.Name);

                        if (!await _storeSeeder.SeedStoreBaseEntitiesAsync(tenant.Id, cancellationToken))
                        {
                            _logger.LogWarning("Failed to seed store base entities for tenant {TenantId}", tenant.Id);
                            // Not fatal, continue
                        }
                        else
                        {
                            _logger.LogInformation("Successfully seeded store base entities for tenant {TenantId} ({TenantName})",
                                tenant.Id, tenant.Name);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Tenant {TenantId} ({TenantName}) already has store entities seeded",
                            tenant.Id, tenant.Name);
                    }
                }
            }

            // Final validation for default tenant if it was just created
            if (defaultTenant != null)
            {
                var (isValid, issues) = await _entitySeeder.ValidateTenantBaseEntitiesAsync(defaultTenant.Id, cancellationToken);
                if (!isValid)
                {
                    _logger.LogWarning("Base entities validation found issues for default tenant: {Issues}", string.Join("; ", issues));
                }
            }

            _logger.LogInformation("=== BOOTSTRAP COMPLETED SUCCESSFULLY ===");

            // Log summary of what exists
            var allTenants = await _dbContext.Tenants.Where(t => t.Id != Guid.Empty).ToListAsync(cancellationToken);
            var allUsers = await _dbContext.Users.ToListAsync(cancellationToken);

            _logger.LogInformation("Tenants in system: {TenantCount}", allTenants.Count);
            foreach (var tenant in allTenants)
            {
                _logger.LogInformation("  - {TenantName} (Code: {TenantCode})", tenant.Name, tenant.Code);
            }

            _logger.LogInformation("Users in system: {UserCount}", allUsers.Count);
            foreach (var user in allUsers)
            {
                _logger.LogInformation("  - {Username} ({Email}) - Tenant: {TenantId}", user.Username, user.Email, user.TenantId);
            }

            if (defaultTenant != null)
            {
                _logger.LogInformation("Default tenant setup completed: {TenantName} (Code: {TenantCode})", defaultTenant.Name, defaultTenant.Code);
                _logger.LogWarning("SECURITY: Please change the SuperAdmin and Manager passwords immediately after first login!");
            }

            _logger.LogInformation("SuperAdmin license assigned with unlimited users and API calls, including all features");
            _logger.LogInformation("==========================================");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bootstrap process");
            return false;
        }
    }

    public async Task<bool> SeedDefaultRolesAndPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await RolePermissionSeeder.SeedAsync(_dbContext, _logger, cancellationToken);
    }
}
