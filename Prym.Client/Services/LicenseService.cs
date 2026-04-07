using Prym.DTOs.Licensing;

namespace Prym.Client.Services;

/// <summary>
/// Client-side service for license management operations.
/// </summary>
public class LicenseService(
    IHttpClientService httpClientService,
    ILogger<LicenseService> logger) : ILicenseService
{
    private const string BaseUrl = "api/v1/license";

    // License Management
    public async Task<IEnumerable<LicenseDto>> GetLicensesAsync()
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<LicenseDto>>(BaseUrl) ?? Enumerable.Empty<LicenseDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting licenses");
            throw;
        }
    }

    public async Task<LicenseDto?> GetLicenseAsync(Guid id)
    {
        try
        {
            return await httpClientService.GetAsync<LicenseDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting license {LicenseId}", id);
            throw;
        }
    }

    public async Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto createDto)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateLicenseDto, LicenseDto>(BaseUrl, createDto);
            return result ?? throw new InvalidOperationException("Failed to create license");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating license");
            throw;
        }
    }

    public async Task<LicenseDto> UpdateLicenseAsync(Guid id, LicenseDto updateDto)
    {
        try
        {
            var result = await httpClientService.PutAsync<LicenseDto, LicenseDto>($"{BaseUrl}/{id}", updateDto);
            return result ?? throw new InvalidOperationException("Failed to update license");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating license {LicenseId}", id);
            throw;
        }
    }

    public async Task DeleteLicenseAsync(Guid id)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting license {LicenseId}", id);
            throw;
        }
    }

    // Tenant License Management
    public async Task<IEnumerable<TenantLicenseDto>> GetTenantLicensesAsync()
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<TenantLicenseDto>>($"{BaseUrl}/tenant-licenses") ?? Enumerable.Empty<TenantLicenseDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tenant licenses");
            throw;
        }
    }

    public async Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId)
    {
        try
        {
            return await httpClientService.GetAsync<TenantLicenseDto>($"{BaseUrl}/tenant/{tenantId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting tenant license for {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<TenantLicenseDto> AssignLicenseAsync(AssignLicenseDto assignDto)
    {
        try
        {
            var result = await httpClientService.PostAsync<AssignLicenseDto, TenantLicenseDto>($"{BaseUrl}/assign", assignDto);
            return result ?? throw new InvalidOperationException("Failed to assign license");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning license to tenant {TenantId}", assignDto.TenantId);
            throw;
        }
    }

    public async Task RemoveTenantLicenseAsync(Guid tenantId)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/tenant/{tenantId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing license from tenant {TenantId}", tenantId);
            throw;
        }
    }

    // License Features
    public async Task<IEnumerable<LicenseFeatureDto>> GetLicenseFeaturesAsync(Guid licenseId)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<LicenseFeatureDto>>($"{BaseUrl}/{licenseId}/features") ?? Enumerable.Empty<LicenseFeatureDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting license features for {LicenseId}", licenseId);
            throw;
        }
    }

    public async Task<IEnumerable<AvailableFeatureDto>> GetAvailableFeaturesAsync()
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<AvailableFeatureDto>>($"{BaseUrl}/available-features") ?? Enumerable.Empty<AvailableFeatureDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting available features");
            throw;
        }
    }

    public async Task<LicenseDto> UpdateLicenseFeaturesAsync(Guid licenseId, UpdateLicenseFeaturesDto updateDto)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateLicenseFeaturesDto, LicenseDto>($"{BaseUrl}/{licenseId}/features", updateDto);
            return result ?? throw new InvalidOperationException("Failed to update license features");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating license features for {LicenseId}", licenseId);
            throw;
        }
    }

    // API Usage and Statistics
    public async Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId)
    {
        try
        {
            return await httpClientService.GetAsync<ApiUsageDto>($"{BaseUrl}/usage/{tenantId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API usage for tenant {TenantId}", tenantId);
            throw;
        }
    }

    // Feature Template Management (SuperAdmin)
    public async Task<IEnumerable<FeatureTemplateDto>> GetFeatureTemplatesAsync()
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<FeatureTemplateDto>>($"{BaseUrl}/feature-templates")
                ?? Enumerable.Empty<FeatureTemplateDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feature templates");
            throw;
        }
    }

    public async Task<FeatureTemplateDto?> GetFeatureTemplateAsync(Guid id)
    {
        try
        {
            return await httpClientService.GetAsync<FeatureTemplateDto>($"{BaseUrl}/feature-templates/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting feature template {Id}", id);
            throw;
        }
    }

    public async Task<FeatureTemplateDto> CreateFeatureTemplateAsync(CreateFeatureTemplateDto dto)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateFeatureTemplateDto, FeatureTemplateDto>($"{BaseUrl}/feature-templates", dto);
            return result ?? throw new InvalidOperationException("Failed to create feature template");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating feature template");
            throw;
        }
    }

    public async Task<FeatureTemplateDto> UpdateFeatureTemplateAsync(Guid id, UpdateFeatureTemplateDto dto)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateFeatureTemplateDto, FeatureTemplateDto>($"{BaseUrl}/feature-templates/{id}", dto);
            return result ?? throw new InvalidOperationException("Failed to update feature template");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating feature template {Id}", id);
            throw;
        }
    }

    public async Task DeleteFeatureTemplateAsync(Guid id)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/feature-templates/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting feature template {Id}", id);
            throw;
        }
    }

    public async Task<FeatureTemplateDto> ToggleFeatureTemplateAvailabilityAsync(Guid id)
    {
        try
        {
            var result = await httpClientService.PutAsync<object, FeatureTemplateDto>($"{BaseUrl}/feature-templates/{id}/toggle-availability", new { });
            return result ?? throw new InvalidOperationException("Failed to toggle feature template availability");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling feature template availability {Id}", id);
            throw;
        }
    }
    }