using Prym.DTOs.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Licensing;

/// <summary>
/// Implementation of license management services.
/// </summary>
public class LicenseService(PrymDbContext context, ILogger<LicenseService> logger) : ILicenseService
{

    public async Task<bool> HasFeatureAccessAsync(Guid tenantId, string featureName)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null || !tenantLicense.IsValid)
                return false;

            var feature = tenantLicense.License.LicenseFeatures
                .FirstOrDefault(lf => lf.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase) &&
                                     lf.IsActive);

            return feature is not null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking feature access for tenant {TenantId} and feature {FeatureName}",
                tenantId, featureName);
            return false;
        }
    }

    public async Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null)
                return null;

            var currentUserCount = await context.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);

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
                IsActive = tenantLicense.IsAssignmentActive,
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
                    IsActive = lf.IsActive,
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
            logger.LogError(ex, "Error retrieving tenant license for {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> IsWithinApiLimitsAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null || !tenantLicense.IsValid)
                return false;

            // Reset monthly API calls if needed
            await ResetApiCallsIfNeeded(tenantLicense);

            return tenantLicense.ApiCallsThisMonth < tenantLicense.License.MaxApiCallsPerMonth;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking API limits for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> IncrementApiCallAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null || !tenantLicense.IsValid)
                return false;

            // Reset monthly API calls if needed
            await ResetApiCallsIfNeeded(tenantLicense);

            // Check if incrementing would exceed limit
            if (tenantLicense.ApiCallsThisMonth >= tenantLicense.License.MaxApiCallsPerMonth)
                return false;

            tenantLicense.ApiCallsThisMonth++;
            _ = await context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing API call for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null)
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
            logger.LogError(ex, "Error retrieving API usage for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> CanAddUserAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null || !tenantLicense.IsValid)
                return false;

            var currentUserCount = await context.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
            return currentUserCount < tenantLicense.License.MaxUsers;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user can be added for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<List<string>> GetAvailablePermissionsAsync(Guid tenantId)
    {
        try
        {
            var tenantLicense = await GetActiveTenantLicense(tenantId);
            if (tenantLicense is null || !tenantLicense.IsValid)
                return [];

            var permissions = tenantLicense.License.LicenseFeatures
                .Where(lf => lf.IsActive)
                .SelectMany(lf => lf.LicenseFeaturePermissions)
                .Select(lfp => lfp.Permission.Name)
                .Distinct()
                .ToList();

            return permissions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available permissions for tenant {TenantId}", tenantId);
            return [];
        }
    }

    private async Task<Data.Entities.Auth.TenantLicense?> GetActiveTenantLicense(Guid tenantId)
    {
        // Se ci sono pi� licenze attive, prende quella con TierLevel pi� alto
        return await context.TenantLicenses
            .Include(tl => tl.Tenant)
            .Include(tl => tl.License)
                .ThenInclude(l => l.LicenseFeatures)
                    .ThenInclude(lf => lf.LicenseFeaturePermissions)
                        .ThenInclude(lfp => lfp.Permission)
            .Where(tl => tl.TargetTenantId == tenantId &&
                         tl.IsAssignmentActive && !tl.IsDeleted)
            .OrderByDescending(tl => tl.License.TierLevel)
            .FirstOrDefaultAsync();
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
            _ = await context.SaveChangesAsync();
        }
    }

}
