using EventForge.DTOs.Licensing;

namespace EventForge.Client.Services.Admin
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

        // API Usage and Statistics
        Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId);
    }
}