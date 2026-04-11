using EventForge.DTOs.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing branding configuration with multi-tenant support and caching.
/// </summary>
public class BrandingService(
    EventForgeDbContext context,
    IConfigurationService configurationService,
    IMemoryCache cache,
    ILogger<BrandingService> logger,
    IWebHostEnvironment environment) : IBrandingService
{

    private const string CACHE_KEY_PREFIX = "branding_";
    private const string GLOBAL_CACHE_KEY = "branding_global";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // Default values
    private const string DEFAULT_LOGO_URL = "/eventforgetitle.svg";
    private const int DEFAULT_LOGO_HEIGHT = 40;
    private const string DEFAULT_APPLICATION_NAME = "PRYM";
    private const string DEFAULT_FAVICON_URL = "/trace.svg";

    // File upload constants
    private static readonly string[] ALLOWED_EXTENSIONS = { ".svg", ".png", ".jpg", ".jpeg", ".webp" };
    private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string UPLOAD_FOLDER = "uploads/logos";

    public async Task<BrandingConfigurationDto> GetBrandingAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = tenantId.HasValue ? $"{CACHE_KEY_PREFIX}{tenantId}" : GLOBAL_CACHE_KEY;

            // Try to get from cache
            if (cache.TryGetValue(cacheKey, out BrandingConfigurationDto? cached) && cached is not null)
            {
                logger.LogDebug("Returning branding configuration from cache for {CacheKey}", cacheKey);
                return cached;
            }


            var branding = new BrandingConfigurationDto
            {
                LogoUrl = DEFAULT_LOGO_URL,
                LogoHeight = DEFAULT_LOGO_HEIGHT,
                ApplicationName = DEFAULT_APPLICATION_NAME,
                FaviconUrl = DEFAULT_FAVICON_URL,
                IsTenantOverride = false,
                TenantId = tenantId
            };

            // Get global configuration
            var logoUrl = await configurationService.GetValueAsync("Branding:LogoUrl", DEFAULT_LOGO_URL, ct);
            var logoHeight = await configurationService.GetValueAsync("Branding:LogoHeight", DEFAULT_LOGO_HEIGHT.ToString(), ct);
            var applicationName = await configurationService.GetValueAsync("Branding:ApplicationName", DEFAULT_APPLICATION_NAME, ct);
            var faviconUrl = await configurationService.GetValueAsync("Branding:FaviconUrl", DEFAULT_FAVICON_URL, ct);
            var allowTenantOverride = await configurationService.GetValueAsync("Branding:AllowTenantOverride", "true", ct);

            branding.LogoUrl = logoUrl;
            branding.LogoHeight = int.TryParse(logoHeight, out var height) ? height : DEFAULT_LOGO_HEIGHT;
            branding.ApplicationName = applicationName;
            branding.FaviconUrl = faviconUrl;

            // Check for tenant override if allowed and tenant specified
            if (tenantId.HasValue && bool.TryParse(allowTenantOverride, out var allowed) && allowed)
            {
                var tenant = await context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

                if (tenant is not null)
                {
                    var hasOverride = false;

                    if (!string.IsNullOrWhiteSpace(tenant.CustomLogoUrl))
                    {
                        branding.LogoUrl = tenant.CustomLogoUrl;
                        hasOverride = true;
                    }

                    if (!string.IsNullOrWhiteSpace(tenant.CustomApplicationName))
                    {
                        branding.ApplicationName = tenant.CustomApplicationName;
                        hasOverride = true;
                    }

                    if (!string.IsNullOrWhiteSpace(tenant.CustomFaviconUrl))
                    {
                        branding.FaviconUrl = tenant.CustomFaviconUrl;
                        hasOverride = true;
                    }

                    branding.IsTenantOverride = hasOverride;
                }
            }

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,
                Size = 1  // REQUIRED when SizeLimit is set in Program.cs
            };
            cache.Set(cacheKey, branding, cacheOptions);

            return branding;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GetBrandingAsync operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting branding configuration for TenantId: {TenantId}", tenantId);

            // Return default branding on error
            return new BrandingConfigurationDto
            {
                LogoUrl = DEFAULT_LOGO_URL,
                LogoHeight = DEFAULT_LOGO_HEIGHT,
                ApplicationName = DEFAULT_APPLICATION_NAME,
                FaviconUrl = DEFAULT_FAVICON_URL,
                IsTenantOverride = false,
                TenantId = tenantId
            };
        }
    }

    public async Task<BrandingConfigurationDto> UpdateGlobalBrandingAsync(UpdateBrandingDto updateDto, string username, CancellationToken ct = default)
    {
        try
        {

            // Update configuration values
            if (!string.IsNullOrWhiteSpace(updateDto.LogoUrl))
            {
                await configurationService.SetValueAsync("Branding:LogoUrl", updateDto.LogoUrl, $"Updated by {username}", ct);
            }

            if (updateDto.LogoHeight.HasValue)
            {
                await configurationService.SetValueAsync("Branding:LogoHeight", updateDto.LogoHeight.Value.ToString(), $"Updated by {username}", ct);
            }

            if (!string.IsNullOrWhiteSpace(updateDto.ApplicationName))
            {
                await configurationService.SetValueAsync("Branding:ApplicationName", updateDto.ApplicationName, $"Updated by {username}", ct);
            }

            if (!string.IsNullOrWhiteSpace(updateDto.FaviconUrl))
            {
                await configurationService.SetValueAsync("Branding:FaviconUrl", updateDto.FaviconUrl, $"Updated by {username}", ct);
            }

            // Invalidate cache
            cache.Remove(GLOBAL_CACHE_KEY);

            logger.LogInformation("Global branding configuration updated successfully by user: {Username}", username);

            return await GetBrandingAsync(null, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<BrandingConfigurationDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingDto updateDto, string username, CancellationToken ct = default)
    {
        try
        {

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant is null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Update tenant branding properties
            if (!string.IsNullOrWhiteSpace(updateDto.LogoUrl))
            {
                tenant.CustomLogoUrl = updateDto.LogoUrl;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.ApplicationName))
            {
                tenant.CustomApplicationName = updateDto.ApplicationName;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.FaviconUrl))
            {
                tenant.CustomFaviconUrl = updateDto.FaviconUrl;
            }

            tenant.ModifiedBy = username;
            tenant.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            // Invalidate cache
            cache.Remove($"{CACHE_KEY_PREFIX}{tenantId}");

            logger.LogInformation("Tenant branding updated successfully for TenantId: {TenantId}", tenantId);

            return await GetBrandingAsync(tenantId, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteTenantBrandingAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant is null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Clear tenant branding
            tenant.CustomLogoUrl = null;
            tenant.CustomApplicationName = null;
            tenant.CustomFaviconUrl = null;
            tenant.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            // Invalidate cache
            cache.Remove($"{CACHE_KEY_PREFIX}{tenantId}");

            logger.LogInformation("Tenant branding override deleted successfully for TenantId: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<string> UploadLogoAsync(IFormFile file, Guid? tenantId = null, CancellationToken ct = default)
    {
        try
        {
            if (file is null || file.Length == 0)
            {
                throw new ArgumentException("File is required.");
            }

            // Validate file size
            if (file.Length > MAX_FILE_SIZE)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {MAX_FILE_SIZE / 1024 / 1024}MB.");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ALLOWED_EXTENSIONS.Contains(extension))
            {
                throw new ArgumentException($"File type {extension} is not allowed. Allowed types: {string.Join(", ", ALLOWED_EXTENSIONS)}");
            }

            // Create upload directory if it doesn't exist
            var uploadPath = Path.Combine(environment.WebRootPath, UPLOAD_FOLDER);
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename
            var fileName = $"{(tenantId.HasValue ? $"tenant_{tenantId}_" : "global_")}{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var logoUrl = $"/{UPLOAD_FOLDER}/{fileName}";

            logger.LogInformation("Logo uploaded successfully: {LogoUrl} for TenantId: {TenantId}", logoUrl, tenantId);

            return logoUrl;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
