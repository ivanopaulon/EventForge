using EventForge.DTOs.Licensing;

namespace EventForge.Server.Services.Licensing;

/// <summary>
/// Interface for license management services.
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Check if a tenant has access to a specific feature.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="featureName">Feature name to check</param>
    /// <returns>True if tenant has access to the feature</returns>
    Task<bool> HasFeatureAccessAsync(Guid tenantId, string featureName);

    /// <summary>
    /// Get the active license for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant license information or null if no active license</returns>
    Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId);

    /// <summary>
    /// Check if a tenant is within their API call limits.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>True if within limits, false if exceeded</returns>
    Task<bool> IsWithinApiLimitsAsync(Guid tenantId);

    /// <summary>
    /// Increment API call count for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>True if increment was successful, false if limit would be exceeded</returns>
    Task<bool> IncrementApiCallAsync(Guid tenantId);

    /// <summary>
    /// Get API usage statistics for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>API usage information</returns>
    Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId);

    /// <summary>
    /// Check if a tenant can add more users based on their license.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>True if more users can be added</returns>
    Task<bool> CanAddUserAsync(Guid tenantId);

    /// <summary>
    /// Get the list of permissions available to a tenant based on their license.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of available permissions</returns>
    Task<List<string>> GetAvailablePermissionsAsync(Guid tenantId);
}