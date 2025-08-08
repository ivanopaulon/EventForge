using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.DTOs.Licensing;

namespace EventForge.Server.Services.Licensing;

/// <summary>
/// Implementation of license management services.
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(EventForgeDbContext context, ILogger<LicenseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasFeatureAccessAsync(Guid tenantId, string featureName)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null || !tenantLicense.IsValid)
                return false;

            var feature = tenantLicense.License.LicenseFeatures
                .FirstOrDefault(lf => lf.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase) && 
                                     lf.IsEnabled);

            return feature != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for tenant {TenantId} and feature {FeatureName}", 
                tenantId, featureName);
            return false;
        }
    }

    public async Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null)
                return null;

            var currentUserCount = await _context.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);

            return new TenantLicenseDto
            {
                Id = tenantLicense.Id,
                TenantId = tenantLicense.TargetTenantId,
                TenantName = tenantLicense.Tenant.Name,
                LicenseId = tenantLicense.LicenseId,
                LicenseName = tenantLicense.License.Name,
                LicenseDisplayName = tenantLicense.License.DisplayName,
                StartsAt = tenantLicense.StartsAt,
                ExpiresAt = tenantLicense.ExpiresAt,
                IsActive = tenantLicense.IsLicenseActive,
                ApiCallsThisMonth = tenantLicense.ApiCallsThisMonth,
                MaxApiCallsPerMonth = tenantLicense.License.MaxApiCallsPerMonth,
                ApiCallsResetAt = tenantLicense.ApiCallsResetAt,
                IsValid = tenantLicense.IsValid,
                CreatedAt = tenantLicense.CreatedAt,
                CreatedBy = tenantLicense.CreatedBy ?? "system",
                ModifiedAt = tenantLicense.ModifiedAt,
                ModifiedBy = tenantLicense.ModifiedBy,
                TierLevel = tenantLicense.License.TierLevel,
                MaxUsers = tenantLicense.License.MaxUsers,
                CurrentUserCount = currentUserCount,
                AvailableFeatures = tenantLicense.License.LicenseFeatures.Select(lf => new LicenseFeatureDto
                {
                    Id = lf.Id,
                    Name = lf.Name,
                    DisplayName = lf.DisplayName,
                    Description = lf.Description,
                    Category = lf.Category,
                    IsEnabled = lf.IsEnabled,
                    LicenseId = lf.LicenseId,
                    LicenseName = tenantLicense.License.Name,
                    CreatedAt = lf.CreatedAt,
                    CreatedBy = lf.CreatedBy ?? "system",
                    ModifiedAt = lf.ModifiedAt,
                    ModifiedBy = lf.ModifiedBy,
                    RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant license for {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> IsWithinApiLimitsAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null || !tenantLicense.IsValid)
                return false;

            // Reset monthly API calls if needed
            await ResetApiCallsIfNeeded(tenantLicense);

            return tenantLicense.ApiCallsThisMonth < tenantLicense.License.MaxApiCallsPerMonth;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking API limits for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> IncrementApiCallAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null || !tenantLicense.IsValid)
                return false;

            // Reset monthly API calls if needed
            await ResetApiCallsIfNeeded(tenantLicense);

            // Check if incrementing would exceed limit
            if (tenantLicense.ApiCallsThisMonth >= tenantLicense.License.MaxApiCallsPerMonth)
                return false;

            tenantLicense.ApiCallsThisMonth++;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing API call for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null)
                return null;

            // Reset monthly API calls if needed
            await ResetApiCallsIfNeeded(tenantLicense);

            return new ApiUsageDto
            {
                TenantId = tenantId,
                ApiCallsThisMonth = tenantLicense.ApiCallsThisMonth,
                MaxApiCallsPerMonth = tenantLicense.License.MaxApiCallsPerMonth,
                ApiCallsResetAt = tenantLicense.ApiCallsResetAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API usage for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> CanAddUserAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null || !tenantLicense.IsValid)
                return false;

            var currentUserCount = await _context.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
            return currentUserCount < tenantLicense.License.MaxUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user can be added for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<List<string>> GetAvailablePermissionsAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense == null || !tenantLicense.IsValid)
                return new List<string>();

            var permissions = tenantLicense.License.LicenseFeatures
                .Where(lf => lf.IsEnabled)
                .SelectMany(lf => lf.LicenseFeaturePermissions)
                .Select(lfp => lfp.Permission.Name)
                .Distinct()
                .ToList();

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available permissions for tenant {TenantId}", tenantId);
            return new List<string>();
        }
    }

    private async Task<Data.Entities.Auth.TenantLicense?> GetActiveTenantLicense(Guid tenantId)
    {
        return await _context.TenantLicenses
            .Include(tl => tl.Tenant)
            .Include(tl => tl.License)
                .ThenInclude(l => l.LicenseFeatures)
                    .ThenInclude(lf => lf.LicenseFeaturePermissions)
                        .ThenInclude(lfp => lfp.Permission)
            .FirstOrDefaultAsync(tl => tl.TargetTenantId == tenantId && 
                                      tl.IsLicenseActive && !tl.IsDeleted);
    }

    private async Task ResetApiCallsIfNeeded(Data.Entities.Auth.TenantLicense tenantLicense)
    {
        var currentDate = DateTime.UtcNow;
        var resetDate = tenantLicense.ApiCallsResetAt;

        // Check if we need to reset (different month or year)
        if (resetDate.Month != currentDate.Month || resetDate.Year != currentDate.Year)
        {
            tenantLicense.ApiCallsThisMonth = 0;
            tenantLicense.ApiCallsResetAt = new DateTime(currentDate.Year, currentDate.Month, 1);
            await _context.SaveChangesAsync();
        }
    }
}