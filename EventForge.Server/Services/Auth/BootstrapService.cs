using Microsoft.EntityFrameworkCore;
using EventForge.Server.Services.Auth.Seeders;

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
    private readonly ILogger<BootstrapService> _logger;

    public BootstrapService(
        EventForgeDbContext dbContext,
        IUserSeeder userSeeder,
        ITenantSeeder tenantSeeder,
        ILicenseSeeder licenseSeeder,
        IEntitySeeder entitySeeder,
        ILogger<BootstrapService> logger)
    {
        _dbContext = dbContext;
        _userSeeder = userSeeder;
        _tenantSeeder = tenantSeeder;
        _licenseSeeder = licenseSeeder;
        _entitySeeder = entitySeeder;
        _logger = logger;
    }

    public async Task<bool> EnsureAdminBootstrappedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting bootstrap process...");

            // Ensure database is created
            _ = await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

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

            // Check if any tenants exist
            var existingTenants = await _dbContext.Tenants.ToListAsync(cancellationToken);
            if (existingTenants.Any())
            {
                _logger.LogInformation("Tenants already exist. Checking if base entities need to be seeded...");

                // Check each tenant to see if it needs base entities seeded
                foreach (var tenant in existingTenants)
                {
                    // Skip system-level tenant (Guid.Empty is used for system entities)
                    if (tenant.Id == Guid.Empty)
                    {
                        continue;
                    }

                    // Check if this tenant has base entities
                    var hasVatNatures = await _dbContext.VatNatures
                        .AnyAsync(v => v.TenantId == tenant.Id, cancellationToken);
                    var hasVatRates = await _dbContext.VatRates
                        .AnyAsync(v => v.TenantId == tenant.Id, cancellationToken);
                    var hasUnitsMeasure = await _dbContext.UMs
                        .AnyAsync(u => u.TenantId == tenant.Id, cancellationToken);
                    var hasWarehouses = await _dbContext.StorageFacilities
                        .AnyAsync(w => w.TenantId == tenant.Id, cancellationToken);

                    // If any base entities are missing, seed them
                    if (!hasVatNatures || !hasVatRates || !hasUnitsMeasure || !hasWarehouses)
                    {
                        _logger.LogWarning("Tenant {TenantId} ({TenantName}) is missing base entities. Seeding now...",
                            tenant.Id, tenant.Name);

                        if (!await _entitySeeder.SeedTenantBaseEntitiesAsync(tenant.Id, cancellationToken))
                        {
                            _logger.LogError("Failed to seed base entities for tenant {TenantId}", tenant.Id);
                            // Continue with other tenants instead of failing completely
                        }
                        else
                        {
                            _logger.LogInformation("Successfully seeded base entities for tenant {TenantId} ({TenantName})",
                                tenant.Id, tenant.Name);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Tenant {TenantId} ({TenantName}) already has base entities seeded",
                            tenant.Id, tenant.Name);
                    }
                }

                _logger.LogInformation("Bootstrap data update completed.");
                return true;
            }

            _logger.LogInformation("No tenants found. Starting initial bootstrap...");

            // Create default tenant
            var defaultTenant = await _tenantSeeder.CreateDefaultTenantAsync(cancellationToken);
            if (defaultTenant == null)
            {
                _logger.LogError("Failed to create default tenant");
                return false;
            }

            // Assign SuperAdmin license to default tenant
            if (!await _licenseSeeder.AssignLicenseToTenantAsync(defaultTenant.Id, superAdminLicense.Id, cancellationToken))
            {
                _logger.LogError("Failed to assign SuperAdmin license to default tenant");
                return false;
            }

            // Create SuperAdmin user
            var superAdminUser = await _userSeeder.CreateSuperAdminUserAsync(defaultTenant.Id, cancellationToken);
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

            // Seed base entities for the tenant
            if (!await _entitySeeder.SeedTenantBaseEntitiesAsync(defaultTenant.Id, cancellationToken))
            {
                _logger.LogError("Failed to seed base entities for tenant");
                return false;
            }

            _logger.LogInformation("=== BOOTSTRAP COMPLETED SUCCESSFULLY ===");
            _logger.LogInformation("Default tenant created: {TenantName} (Code: {TenantCode})", defaultTenant.Name, defaultTenant.Code);
            _logger.LogInformation("SuperAdmin user created: {Username} ({Email})", superAdminUser.Username, superAdminUser.Email);
            _logger.LogInformation("SuperAdmin account created. (Password suppressed in logs for security)");
            if (managerUser != null)
            {
                _logger.LogInformation("Manager user created: {Username} ({Email})", managerUser.Username, managerUser.Email);
                _logger.LogInformation("Manager account created. (Password suppressed in logs for security)");
            }
            _logger.LogWarning("SECURITY: Please change the SuperAdmin and Manager passwords immediately after first login!");
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
