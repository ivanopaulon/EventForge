using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of tenant seeding service.
/// </summary>
public class TenantSeeder : ITenantSeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly ILogger<TenantSeeder> _logger;

    public TenantSeeder(
        EventForgeDbContext dbContext,
        ILogger<TenantSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Tenant?> EnsureSystemTenantAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if system tenant already exists
            var systemTenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == Guid.Empty, cancellationToken);
            
            if (systemTenant != null)
            {
                _logger.LogInformation("System tenant already exists");
                return systemTenant;
            }

            // Create system tenant with Id = Guid.Empty
            systemTenant = new Tenant
            {
                Id = Guid.Empty,
                Name = "System",
                Code = "system",
                DisplayName = "System Tenant",
                Description = "System-level tenant for global entities",
                ContactEmail = "system@localhost",
                Domain = "system",
                MaxUsers = int.MaxValue,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // Self-referencing for the base property
            };

            // Add tenant and save changes
            // The Tenant entity is configured with ValueGeneratedNever() in DbContext
            // to allow explicit Id values including Guid.Empty for the System tenant
            _ = _dbContext.Tenants.Add(systemTenant);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("System tenant created with Id = Guid.Empty");
            return systemTenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring system tenant");
            return null;
        }
    }

    public async Task<Tenant?> CreateDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if default tenant already exists
            var existingTenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Code == "default", cancellationToken);
            
            if (existingTenant != null)
            {
                _logger.LogInformation("Default tenant already exists: {TenantName} (Code: {TenantCode})",
                    existingTenant.Name, existingTenant.Code);
                return existingTenant;
            }

            // Create default tenant
            var defaultTenant = new Tenant
            {
                Name = "DefaultTenant",
                Code = "default",
                DisplayName = "Default Tenant",
                Description = "Default tenant created during initial setup",
                ContactEmail = "superadmin@localhost",
                Domain = "localhost",
                MaxUsers = 10,
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _ = _dbContext.Tenants.Add(defaultTenant);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default tenant created: {TenantName} (Code: {TenantCode})",
                defaultTenant.Name, defaultTenant.Code);
            return defaultTenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default tenant");
            return null;
        }
    }

    public async Task<bool> CreateAdminTenantRecordAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if AdminTenant record already exists
            var existingRecord = await _dbContext.AdminTenants
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId, cancellationToken);
            
            if (existingRecord != null)
            {
                _logger.LogInformation("AdminTenant record already exists for user {UserId} managing tenant {TenantId}",
                    userId, tenantId);
                return true;
            }

            var adminTenant = new AdminTenant
            {
                UserId = userId,
                ManagedTenantId = tenantId,
                AccessLevel = AdminAccessLevel.FullAccess,
                GrantedAt = DateTime.UtcNow,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = tenantId // Admin assignment belongs to the managed tenant
            };

            _ = _dbContext.AdminTenants.Add(adminTenant);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("AdminTenant record created for user {UserId} to manage tenant {TenantId}",
                userId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AdminTenant record");
            return false;
        }
    }
}
