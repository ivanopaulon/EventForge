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
            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = "Informazioni sul tenant non trovate. Effettua nuovamente l'accesso.",
                Instance = context.HttpContext.Request.Path
            };
            context.Result = new ObjectResult(problemDetails)
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
                                          tl.IsAssignmentActive && !tl.IsDeleted);

            if (tenantLicense == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "Nessuna licenza attiva trovata per il tenant. Contatta l'amministratore.",
                    Instance = context.HttpContext.Request.Path
                };
                context.Result = new ObjectResult(problemDetails)
                {
                    StatusCode = 403
                };
                return;
            }

            // Check if license is valid (not expired)
            if (!tenantLicense.IsValid)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "La licenza è scaduta o non ancora attiva. Contatta l'amministratore per rinnovarla.",
                    Instance = context.HttpContext.Request.Path
                };
                context.Result = new ObjectResult(problemDetails)
                {
                    StatusCode = 403
                };
                return;
            }

            // Check if the required feature is available in the license
            var requiredFeature = tenantLicense.License.LicenseFeatures
                .FirstOrDefault(lf => lf.Name.Equals(_featureName, StringComparison.OrdinalIgnoreCase) &&
                                     lf.IsActive);

            if (requiredFeature == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = $"La funzionalità '{_featureName}' non è disponibile nella licenza corrente. Aggiorna la licenza per accedere a questa funzionalità.",
                    Instance = context.HttpContext.Request.Path
                };
                context.Result = new ObjectResult(problemDetails)
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
                    var problemDetails = new ProblemDetails
                    {
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Title = "Too Many Requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = $"Limite mensile di chiamate API superato ({tenantLicense.License.MaxApiCallsPerMonth}). Attendi il prossimo mese o aggiorna la licenza.",
                        Instance = context.HttpContext.Request.Path
                    };
                    problemDetails.Extensions["currentUsage"] = tenantLicense.ApiCallsThisMonth;
                    problemDetails.Extensions["limit"] = tenantLicense.License.MaxApiCallsPerMonth;
                    problemDetails.Extensions["resetDate"] = tenantLicense.ApiCallsResetAt.AddMonths(1).ToString("yyyy-MM-dd");

                    context.Result = new ObjectResult(problemDetails)
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
                var problemDetails = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = $"Permessi mancanti per la funzionalità '{_featureName}': {string.Join(", ", missingPermissions)}. Contatta l'amministratore per ottenere i permessi necessari.",
                    Instance = context.HttpContext.Request.Path
                };
                problemDetails.Extensions["missingPermissions"] = missingPermissions.ToArray();
                problemDetails.Extensions["featureName"] = _featureName;

                context.Result = new ObjectResult(problemDetails)
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

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Errore durante la validazione della licenza. Riprova più tardi.",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new ObjectResult(problemDetails)
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