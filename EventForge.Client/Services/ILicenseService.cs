using EventForge.DTOs.Licensing;

namespace EventForge.Client.Services
{
    public interface ILicenseService
    {
        // License Management
        Task<IEnumerable<LicenseDto>> GetLicensesAsync(CancellationToken ct = default);
        Task<LicenseDto?> GetLicenseAsync(Guid id, CancellationToken ct = default);
        Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto createDto, CancellationToken ct = default);
        Task<LicenseDto> UpdateLicenseAsync(Guid id, LicenseDto updateDto, CancellationToken ct = default);
        Task DeleteLicenseAsync(Guid id, CancellationToken ct = default);

        // Tenant License Management
        Task<IEnumerable<TenantLicenseDto>> GetTenantLicensesAsync(CancellationToken ct = default);
        Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId, CancellationToken ct = default);
        Task<TenantLicenseDto> AssignLicenseAsync(AssignLicenseDto assignDto, CancellationToken ct = default);
        Task RemoveTenantLicenseAsync(Guid tenantId, CancellationToken ct = default);

        // License Features
        Task<IEnumerable<LicenseFeatureDto>> GetLicenseFeaturesAsync(Guid licenseId, CancellationToken ct = default);
        Task<IEnumerable<AvailableFeatureDto>> GetAvailableFeaturesAsync(CancellationToken ct = default);
        Task<LicenseDto> UpdateLicenseFeaturesAsync(Guid licenseId, UpdateLicenseFeaturesDto updateDto, CancellationToken ct = default);

        // API Usage and Statistics
        Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId, CancellationToken ct = default);

        // Feature Template Management (SuperAdmin)
        Task<IEnumerable<FeatureTemplateDto>> GetFeatureTemplatesAsync(CancellationToken ct = default);
        Task<FeatureTemplateDto?> GetFeatureTemplateAsync(Guid id, CancellationToken ct = default);
        Task<FeatureTemplateDto> CreateFeatureTemplateAsync(CreateFeatureTemplateDto dto, CancellationToken ct = default);
        Task<FeatureTemplateDto> UpdateFeatureTemplateAsync(Guid id, UpdateFeatureTemplateDto dto, CancellationToken ct = default);
        Task DeleteFeatureTemplateAsync(Guid id, CancellationToken ct = default);
        Task<FeatureTemplateDto> ToggleFeatureTemplateAvailabilityAsync(Guid id, CancellationToken ct = default);
    }
}