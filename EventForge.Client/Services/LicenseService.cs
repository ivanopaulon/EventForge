using EventForge.DTOs.Licensing;

namespace EventForge.Client.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<LicenseService> _logger;
        private const string BaseUrl = "api/v1/License";

        public LicenseService(IHttpClientService httpClientService, ILogger<LicenseService> logger)
        {
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // License Management
        public async Task<IEnumerable<LicenseDto>> GetLicensesAsync()
        {
            try
            {
                return await _httpClientService.GetAsync<IEnumerable<LicenseDto>>(BaseUrl) ?? Enumerable.Empty<LicenseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses");
                throw;
            }
        }

        public async Task<LicenseDto?> GetLicenseAsync(Guid id)
        {
            try
            {
                return await _httpClientService.GetAsync<LicenseDto>($"{BaseUrl}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license {LicenseId}", id);
                throw;
            }
        }

        public async Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto createDto)
        {
            try
            {
                var result = await _httpClientService.PostAsync<CreateLicenseDto, LicenseDto>(BaseUrl, createDto);
                return result ?? throw new InvalidOperationException("Failed to create license");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating license");
                throw;
            }
        }

        public async Task<LicenseDto> UpdateLicenseAsync(Guid id, LicenseDto updateDto)
        {
            try
            {
                var result = await _httpClientService.PutAsync<LicenseDto, LicenseDto>($"{BaseUrl}/{id}", updateDto);
                return result ?? throw new InvalidOperationException("Failed to update license");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating license {LicenseId}", id);
                throw;
            }
        }

        public async Task DeleteLicenseAsync(Guid id)
        {
            try
            {
                await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting license {LicenseId}", id);
                throw;
            }
        }

        // Tenant License Management
        public async Task<IEnumerable<TenantLicenseDto>> GetTenantLicensesAsync()
        {
            try
            {
                return await _httpClientService.GetAsync<IEnumerable<TenantLicenseDto>>($"{BaseUrl}/tenant-licenses") ?? Enumerable.Empty<TenantLicenseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant licenses");
                throw;
            }
        }

        public async Task<TenantLicenseDto?> GetTenantLicenseAsync(Guid tenantId)
        {
            try
            {
                return await _httpClientService.GetAsync<TenantLicenseDto>($"{BaseUrl}/tenant/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant license for {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<TenantLicenseDto> AssignLicenseAsync(AssignLicenseDto assignDto)
        {
            try
            {
                var result = await _httpClientService.PostAsync<AssignLicenseDto, TenantLicenseDto>($"{BaseUrl}/assign", assignDto);
                return result ?? throw new InvalidOperationException("Failed to assign license");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning license to tenant {TenantId}", assignDto.TenantId);
                throw;
            }
        }

        public async Task RemoveTenantLicenseAsync(Guid tenantId)
        {
            try
            {
                await _httpClientService.DeleteAsync($"{BaseUrl}/tenant/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing license from tenant {TenantId}", tenantId);
                throw;
            }
        }

        // License Features
        public async Task<IEnumerable<LicenseFeatureDto>> GetLicenseFeaturesAsync(Guid licenseId)
        {
            try
            {
                return await _httpClientService.GetAsync<IEnumerable<LicenseFeatureDto>>($"{BaseUrl}/{licenseId}/features") ?? Enumerable.Empty<LicenseFeatureDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license features for {LicenseId}", licenseId);
                throw;
            }
        }

        // API Usage and Statistics
        public async Task<ApiUsageDto?> GetApiUsageAsync(Guid tenantId)
        {
            try
            {
                return await _httpClientService.GetAsync<ApiUsageDto>($"{BaseUrl}/usage/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API usage for tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}