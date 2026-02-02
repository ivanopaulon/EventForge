namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service interface for managing branding configuration.
/// Provides methods to get, update, and manage branding settings with multi-tenant support.
/// </summary>
public interface IBrandingService
{
    /// <summary>
    /// Gets branding configuration with fallback chain:
    /// 1. Tenant override (if tenantId specified and overrides exist)
    /// 2. Global configuration
    /// 3. Default hardcoded values
    /// </summary>
    /// <param name="tenantId">Optional tenant ID for tenant-specific branding</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Branding configuration</returns>
    Task<BrandingConfigurationDto> GetBrandingAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Updates global branding configuration (requires SuperAdmin).
    /// </summary>
    /// <param name="updateDto">Branding update data</param>
    /// <param name="username">Username of the user making the change</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated branding configuration</returns>
    Task<BrandingConfigurationDto> UpdateGlobalBrandingAsync(UpdateBrandingDto updateDto, string username, CancellationToken ct = default);

    /// <summary>
    /// Updates tenant-specific branding override (requires Admin).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="updateDto">Branding update data</param>
    /// <param name="username">Username of the user making the change</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated branding configuration</returns>
    Task<BrandingConfigurationDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingDto updateDto, string username, CancellationToken ct = default);

    /// <summary>
    /// Deletes tenant branding override, reverting to global settings.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteTenantBrandingAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Uploads a logo file and returns the URL.
    /// Supports .svg, .png, .jpg, .jpeg, .webp formats with max 5MB size.
    /// </summary>
    /// <param name="file">Logo file to upload</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific logo</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Uploaded logo URL</returns>
    Task<string> UploadLogoAsync(IFormFile file, Guid? tenantId = null, CancellationToken ct = default);
}
