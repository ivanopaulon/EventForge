using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventForge.Server.Filters;

/// <summary>
/// Authorization filter that verifies tenant license and feature access.
/// </summary>
public class RequireLicenseFeatureAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _featureName;
    private readonly bool _checkApiLimits;

    /// <summary>
    /// Initializes the license feature requirement filter.
    /// </summary>
    /// <param name="featureName">Required feature name</param>
    /// <param name="checkApiLimits">Whether to check API call limits</param>
    public RequireLicenseFeatureAttribute(string featureName, bool checkApiLimits = true)
    {
        _featureName = featureName;
        _checkApiLimits = checkApiLimits;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Skip authorization if user is not authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        // Skip license checks for SuperAdmins - they have full access to all features
        var isSuperAdmin = context.HttpContext.User.IsInRole("SuperAdmin") ||
                          context.HttpContext.User.HasClaim("permission", "System.Admin.FullAccess");

        if (isSuperAdmin)
        {
            return; // SuperAdmins bypass all license restrictions
        }

        // Get tenant ID from claims
        var tenantIdClaim = context.HttpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Result = new ObjectResult("Tenant information not found")
            {
                StatusCode = 403
            };
            return;
        }

        // Get DbContext from DI
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<EventForgeDbContext>();

        try
        {
            // Get active tenant license with features
            var tenantLicense = await dbContext.TenantLicenses
                .Include(tl => tl.License)
                    .ThenInclude(l => l.LicenseFeatures)
                        .ThenInclude(lf => lf.LicenseFeaturePermissions)
                            .ThenInclude(lfp => lfp.Permission)
                .FirstOrDefaultAsync(tl => tl.TargetTenantId == tenantId &&
                                          tl.IsLicenseActive && !tl.IsDeleted);

            if (tenantLicense == null)
            {
                context.Result = new ObjectResult("No active license found for tenant")
                {
                    StatusCode = 403
                };
                return;
            }

            // Check if license is valid (not expired)
            if (!tenantLicense.IsValid)
            {
                context.Result = new ObjectResult("License has expired or is not yet active")
                {
                    StatusCode = 403
                };
                return;
            }

            // Check if the required feature is available in the license
            var requiredFeature = tenantLicense.License.LicenseFeatures
                .FirstOrDefault(lf => lf.Name.Equals(_featureName, StringComparison.OrdinalIgnoreCase) &&
                                     lf.IsEnabled);

            if (requiredFeature == null)
            {
                context.Result = new ObjectResult($"Feature '{_featureName}' is not available in current license")
                {
                    StatusCode = 403
                };
                return;
            }

            // Check API call limits if enabled
            if (_checkApiLimits)
            {
                // Reset monthly API calls if needed
                if (tenantLicense.ApiCallsResetAt.Month != DateTime.UtcNow.Month ||
                    tenantLicense.ApiCallsResetAt.Year != DateTime.UtcNow.Year)
                {
                    tenantLicense.ApiCallsThisMonth = 0;
                    tenantLicense.ApiCallsResetAt = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                }

                // Check if API limit is exceeded
                if (tenantLicense.ApiCallsThisMonth >= tenantLicense.License.MaxApiCallsPerMonth)
                {
                    context.Result = new ObjectResult("API call limit exceeded for this month")
                    {
                        StatusCode = 429 // Too Many Requests
                    };
                    return;
                }

                // Increment API call counter
                tenantLicense.ApiCallsThisMonth++;
                await dbContext.SaveChangesAsync();
            }

            // Check if user has required permissions for the feature
            var userPermissions = await GetUserPermissionsAsync(dbContext, context.HttpContext.User);
            var requiredPermissions = requiredFeature.LicenseFeaturePermissions
                .Select(lfp => lfp.Permission.Name)
                .ToList();

            var hasRequiredPermissions = requiredPermissions.All(rp =>
                userPermissions.Contains(rp, StringComparer.OrdinalIgnoreCase));

            if (!hasRequiredPermissions)
            {
                var missingPermissions = requiredPermissions.Except(userPermissions, StringComparer.OrdinalIgnoreCase);
                context.Result = new ObjectResult($"Missing required permissions: {string.Join(", ", missingPermissions)}")
                {
                    StatusCode = 403
                };
                return;
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireLicenseFeatureAttribute>>();
            logger.LogError(ex, "Error checking license feature authorization for feature '{FeatureName}' and tenant '{TenantId}'",
                _featureName, tenantId);

            context.Result = new ObjectResult("Error validating license")
            {
                StatusCode = 500
            };
        }
    }

    private async Task<List<string>> GetUserPermissionsAsync(EventForgeDbContext dbContext, ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return new List<string>();
        }

        var permissions = await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }
}