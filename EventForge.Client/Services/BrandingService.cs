using EventForge.DTOs.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing branding configuration from the client.
/// </summary>
public interface IBrandingService
{
    /// <summary>
    /// Gets branding configuration for the current or specified tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID</param>
    /// <returns>Branding configuration</returns>
    Task<BrandingConfigurationDto> GetBrandingAsync(Guid? tenantId = null);

    /// <summary>
    /// Updates global branding configuration (requires SuperAdmin).
    /// </summary>
    /// <param name="updateDto">Branding update data</param>
    /// <returns>Updated branding configuration</returns>
    Task<BrandingConfigurationDto> UpdateGlobalBrandingAsync(UpdateBrandingDto updateDto);

    /// <summary>
    /// Updates tenant-specific branding override (requires Manager).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="updateDto">Branding update data</param>
    /// <returns>Updated branding configuration</returns>
    Task<BrandingConfigurationDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingDto updateDto);

    /// <summary>
    /// Deletes tenant branding override, reverting to global settings.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    Task DeleteTenantBrandingAsync(Guid tenantId);

    /// <summary>
    /// Uploads a logo file.
    /// </summary>
    /// <param name="content">File content</param>
    /// <param name="fileName">File name</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific logo</param>
    /// <returns>Uploaded logo URL</returns>
    Task<string> UploadLogoAsync(byte[] content, string fileName, Guid? tenantId = null);
}

/// <summary>
/// Service for managing branding configuration from the client with caching.
/// </summary>
public class BrandingService : IBrandingService
{
    private const string BaseUrl = "api/v1/branding";
    private const string CacheKeyPrefix = "branding_client_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IHttpClientService _httpClientService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BrandingService> _logger;

    public BrandingService(
        IHttpClientService httpClientService,
        IMemoryCache cache,
        ILogger<BrandingService> logger)
    {
        _httpClientService = httpClientService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<BrandingConfigurationDto> GetBrandingAsync(Guid? tenantId = null)
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}{tenantId?.ToString() ?? "global"}";

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out BrandingConfigurationDto? cached) && cached != null)
            {
                _logger.LogDebug("Returning branding configuration from cache for {CacheKey}", cacheKey);
                return cached;
            }

            var url = tenantId.HasValue ? $"{BaseUrl}?tenantId={tenantId}" : BaseUrl;
            var branding = await _httpClientService.GetAsync<BrandingConfigurationDto>(url);

            if (branding != null)
            {
                // Cache the result
                _cache.Set(cacheKey, branding, CacheDuration);
                return branding;
            }

            // Return default branding if API call fails
            return GetDefaultBranding(tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving branding configuration, using defaults");
            // Return default branding on error
            return GetDefaultBranding(tenantId);
        }
    }

    public async Task<BrandingConfigurationDto> UpdateGlobalBrandingAsync(UpdateBrandingDto updateDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateBrandingDto, BrandingConfigurationDto>(
                $"{BaseUrl}/global", updateDto);

            // Invalidate cache
            _cache.Remove($"{CacheKeyPrefix}global");

            return result ?? throw new InvalidOperationException("Failed to update global branding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global branding");
            throw;
        }
    }

    public async Task<BrandingConfigurationDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingDto updateDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateBrandingDto, BrandingConfigurationDto>(
                $"{BaseUrl}/tenant/{tenantId}", updateDto);

            // Invalidate cache
            _cache.Remove($"{CacheKeyPrefix}{tenantId}");

            return result ?? throw new InvalidOperationException("Failed to update tenant branding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant branding for TenantId: {TenantId}", tenantId);
            throw;
        }
    }

    public async Task DeleteTenantBrandingAsync(Guid tenantId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/tenant/{tenantId}");

            // Invalidate cache
            _cache.Remove($"{CacheKeyPrefix}{tenantId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant branding for TenantId: {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<string> UploadLogoAsync(byte[] content, string fileName, Guid? tenantId = null)
    {
        try
        {
            using var formContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(content);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            formContent.Add(fileContent, "file", fileName);

            var url = tenantId.HasValue ? $"{BaseUrl}/upload?tenantId={tenantId}" : $"{BaseUrl}/upload";

            var result = await _httpClientService.PostAsync<MultipartFormDataContent, dynamic>(url, formContent);

            if (result == null)
            {
                throw new InvalidOperationException("Failed to upload logo");
            }

            // Extract logoUrl from result
            string logoUrl = result.logoUrl?.ToString() ?? throw new InvalidOperationException("Logo URL not returned");

            // Invalidate cache
            var cacheKey = $"{CacheKeyPrefix}{tenantId?.ToString() ?? "global"}";
            _cache.Remove(cacheKey);

            return logoUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo");
            throw;
        }
    }

    private static BrandingConfigurationDto GetDefaultBranding(Guid? tenantId)
    {
        return new BrandingConfigurationDto
        {
            LogoUrl = "/eventforgetitle.svg",
            LogoHeight = 40,
            ApplicationName = "EventForge",
            FaviconUrl = "/trace.svg",
            IsTenantOverride = false,
            TenantId = tenantId
        };
    }
}
