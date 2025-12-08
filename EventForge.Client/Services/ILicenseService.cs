using EventForge.DTOs.Licensing;

namespace EventForge.Client.Services
{
    public interface ILicenseService
    {
        // License Management
        Task<IEnumerable<LicenseDto>> GetLicensesAsync();
        Task<LicenseDto?> GetLicenseAsync(Guid id);
        Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto createDto);
        Task<LicenseDto> UpdateLicenseAsync(Guid id, LicenseDto updateDto);
        Task DeleteLicenseAsync(Guid id);

        // Tenant License Management
        Task<IEnumerable<TenantLicenseDto>> GetTenantLicensesAsync();
        Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId);
        Task<TenantLicenseDto> AssignLicenseAsync(AssignLicenseDto assignDto);
        Task RemoveTenantLicenseAsync(Guid tenantId);

        // License Features
        Task<IEnumerable<LicenseFeatureDto>> GetLicenseFeaturesAsync(Guid licenseId);
        Task<IEnumerable<AvailableFeatureDto>> GetAvailableFeaturesAsync();
        Task<LicenseDto> UpdateLicenseFeaturesAsync(Guid licenseId, UpdateLicenseFeaturesDto updateDto);

        // API Usage and Statistics
        Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId);

        // Feature Template Management (SuperAdmin)
        Task<IEnumerable<FeatureTemplateDto>> GetFeatureTemplatesAsync();
        Task<FeatureTemplateDto?> GetFeatureTemplateAsync(Guid id);
        Task<FeatureTemplateDto> CreateFeatureTemplateAsync(CreateFeatureTemplateDto dto);
        Task<FeatureTemplateDto> UpdateFeatureTemplateAsync(Guid id, UpdateFeatureTemplateDto dto);
        Task DeleteFeatureTemplateAsync(Guid id);
        Task<FeatureTemplateDto> ToggleFeatureTemplateAvailabilityAsync(Guid id);
    }
}