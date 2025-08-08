using EventForge.DTOs.Licensing;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(HttpClient httpClient, ILogger<LicenseService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // License Management
        public async Task<IEnumerable<LicenseDto>> GetLicensesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<LicenseDto>>("api/license");
                return response ?? Enumerable.Empty<LicenseDto>();
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
                return await _httpClient.GetFromJsonAsync<LicenseDto>($"api/license/{id}");
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
                var response = await _httpClient.PostAsJsonAsync("api/license", createDto);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<LicenseDto>();
                return result!;
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
                var response = await _httpClient.PutAsJsonAsync($"api/license/{id}", updateDto);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<LicenseDto>();
                return result!;
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
                var response = await _httpClient.DeleteAsync($"api/license/{id}");
                response.EnsureSuccessStatusCode();
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
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<TenantLicenseDto>>("api/license/tenant-licenses");
                return response ?? Enumerable.Empty<TenantLicenseDto>();
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
                return await _httpClient.GetFromJsonAsync<TenantLicenseDto>($"api/license/tenant/{tenantId}");
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
                var response = await _httpClient.PostAsJsonAsync("api/license/assign", assignDto);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TenantLicenseDto>();
                return result!;
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
                var response = await _httpClient.DeleteAsync($"api/license/tenant/{tenantId}");
                response.EnsureSuccessStatusCode();
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
                var response = await _httpClient.GetFromJsonAsync<IEnumerable<LicenseFeatureDto>>($"api/license/{licenseId}/features");
                return response ?? Enumerable.Empty<LicenseFeatureDto>();
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
                return await _httpClient.GetFromJsonAsync<ApiUsageDto>($"api/license/usage/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API usage for tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}