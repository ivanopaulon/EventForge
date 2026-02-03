using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using EventForge.DTOs.Configuration;
using EventForge.Server.Data;

namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing branding configuration with multi-tenant support and caching.
/// </summary>
public class BrandingService : IBrandingService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IConfigurationService _configurationService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BrandingService> _logger;
    private readonly IWebHostEnvironment _environment;

    private const string CACHE_KEY_PREFIX = "branding_";
    private const string GLOBAL_CACHE_KEY = "branding_global";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // Default values
    private const string DEFAULT_LOGO_URL = "/eventforgetitle.svg";
    private const int DEFAULT_LOGO_HEIGHT = 40;
    private const string DEFAULT_APPLICATION_NAME = "EventForge";
    private const string DEFAULT_FAVICON_URL = "/trace.svg";

    // File upload constants
    private static readonly string[] ALLOWED_EXTENSIONS = { ".svg", ".png", ".jpg", ".jpeg", ".webp" };
    private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string UPLOAD_FOLDER = "uploads/logos";

    public BrandingService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IConfigurationService configurationService,
        IMemoryCache cache,
        ILogger<BrandingService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _tenantContext = tenantContext;
        _configurationService = configurationService;
        _cache = cache;
        _logger = logger;
        _environment = environment;
    }

    public async Task<BrandingConfigurationDto> GetBrandingAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = tenantId.HasValue ? $"{CACHE_KEY_PREFIX}{tenantId}" : GLOBAL_CACHE_KEY;

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out BrandingConfigurationDto? cached) && cached != null)
            {
                _logger.LogDebug("Returning branding configuration from cache for {CacheKey}", cacheKey);
                return cached;
            }

            _logger.LogInformation("Loading branding configuration for TenantId: {TenantId}", tenantId);

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
            var logoUrl = await _configurationService.GetValueAsync("Branding:LogoUrl", DEFAULT_LOGO_URL, ct);
            var logoHeight = await _configurationService.GetValueAsync("Branding:LogoHeight", DEFAULT_LOGO_HEIGHT.ToString(), ct);
            var applicationName = await _configurationService.GetValueAsync("Branding:ApplicationName", DEFAULT_APPLICATION_NAME, ct);
            var faviconUrl = await _configurationService.GetValueAsync("Branding:FaviconUrl", DEFAULT_FAVICON_URL, ct);
            var allowTenantOverride = await _configurationService.GetValueAsync("Branding:AllowTenantOverride", "true", ct);

            branding.LogoUrl = logoUrl;
            branding.LogoHeight = int.TryParse(logoHeight, out var height) ? height : DEFAULT_LOGO_HEIGHT;
            branding.ApplicationName = applicationName;
            branding.FaviconUrl = faviconUrl;

            // Check for tenant override if allowed and tenant specified
            if (tenantId.HasValue && bool.TryParse(allowTenantOverride, out var allowed) && allowed)
            {
                var tenant = await _context.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

                if (tenant != null)
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
                Size = 1  // OBBLIGATORIO quando SizeLimit è impostato in Program.cs
            };
            _cache.Set(cacheKey, branding, cacheOptions);

            return branding;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetBrandingAsync operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branding configuration for TenantId: {TenantId}", tenantId);

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
            _logger.LogInformation("Updating global branding configuration by user: {Username}", username);

            // Update configuration values
            if (!string.IsNullOrWhiteSpace(updateDto.LogoUrl))
            {
                await _configurationService.SetValueAsync("Branding:LogoUrl", updateDto.LogoUrl, $"Updated by {username}", ct);
            }

            if (updateDto.LogoHeight.HasValue)
            {
                await _configurationService.SetValueAsync("Branding:LogoHeight", updateDto.LogoHeight.Value.ToString(), $"Updated by {username}", ct);
            }

            if (!string.IsNullOrWhiteSpace(updateDto.ApplicationName))
            {
                await _configurationService.SetValueAsync("Branding:ApplicationName", updateDto.ApplicationName, $"Updated by {username}", ct);
            }

            if (!string.IsNullOrWhiteSpace(updateDto.FaviconUrl))
            {
                await _configurationService.SetValueAsync("Branding:FaviconUrl", updateDto.FaviconUrl, $"Updated by {username}", ct);
            }

            // Invalidate cache
            _cache.Remove(GLOBAL_CACHE_KEY);

            _logger.LogInformation("Global branding configuration updated successfully by user: {Username}", username);

            return await GetBrandingAsync(null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global branding configuration");
            throw;
        }
    }

    public async Task<BrandingConfigurationDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingDto updateDto, string username, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating tenant branding for TenantId: {TenantId} by user: {Username}", tenantId, username);

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant == null)
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

            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            _cache.Remove($"{CACHE_KEY_PREFIX}{tenantId}");

            _logger.LogInformation("Tenant branding updated successfully for TenantId: {TenantId}", tenantId);

            return await GetBrandingAsync(tenantId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant branding for TenantId: {TenantId}", tenantId);
            throw;
        }
    }

    public async Task DeleteTenantBrandingAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Deleting tenant branding override for TenantId: {TenantId}", tenantId);

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant == null)
            {
                throw new InvalidOperationException($"Tenant with ID {tenantId} not found.");
            }

            // Clear tenant branding
            tenant.CustomLogoUrl = null;
            tenant.CustomApplicationName = null;
            tenant.CustomFaviconUrl = null;
            tenant.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            _cache.Remove($"{CACHE_KEY_PREFIX}{tenantId}");

            _logger.LogInformation("Tenant branding override deleted successfully for TenantId: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant branding for TenantId: {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<string> UploadLogoAsync(IFormFile file, Guid? tenantId = null, CancellationToken ct = default)
    {
        try
        {
            if (file == null || file.Length == 0)
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
            var uploadPath = Path.Combine(_environment.WebRootPath, UPLOAD_FOLDER);
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

            _logger.LogInformation("Logo uploaded successfully: {LogoUrl} for TenantId: {TenantId}", logoUrl, tenantId);

            return logoUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo for TenantId: {TenantId}", tenantId);
            throw;
        }
    }
}
