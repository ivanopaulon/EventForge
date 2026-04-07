using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of tenant seeding service.
/// </summary>
public class TenantSeeder(
    PrymDbContext dbContext,
    ILogger<TenantSeeder> logger) : ITenantSeeder
{

    public async Task<Tenant?> EnsureSystemTenantAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var systemTenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == Guid.Empty, cancellationToken);

            if (systemTenant is not null)
                return systemTenant;

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

            _ = dbContext.Tenants.Add(systemTenant);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return systemTenant;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring system tenant");
            return null;
        }
    }

    public async Task<Tenant?> CreateDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existingTenant = await dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Code == "default", cancellationToken);

            if (existingTenant is not null)
                return existingTenant;

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

            _ = dbContext.Tenants.Add(defaultTenant);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return defaultTenant;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating default tenant");
            return null;
        }
    }

    public async Task<bool> CreateAdminTenantRecordAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingRecord = await dbContext.AdminTenants
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId, cancellationToken);

            if (existingRecord is not null)
                return true;

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

            _ = dbContext.AdminTenants.Add(adminTenant);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating AdminTenant record");
            return false;
        }
    }

}
