using EventForge.DTOs.Licensing;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LicenseService> _logger;
        private const string BaseUrl = "api/v1/License";

        public LicenseService(IHttpClientFactory httpClientFactory, ILogger<LicenseService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // License Management
        public async Task<IEnumerable<LicenseDto>> GetLicensesAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.GetFromJsonAsync<IEnumerable<LicenseDto>>(BaseUrl);
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                return await httpClient.GetFromJsonAsync<LicenseDto>($"{BaseUrl}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting license {LicenseId}", id);
                throw;
            }
        }

        public async Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto createDto)
        {
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.GetFromJsonAsync<IEnumerable<TenantLicenseDto>>($"{BaseUrl}/tenant-licenses");
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                return await httpClient.GetFromJsonAsync<TenantLicenseDto>($"{BaseUrl}/tenant/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant license for {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<TenantLicenseDto> AssignLicenseAsync(AssignLicenseDto assignDto)
        {
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/assign", assignDto);
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.DeleteAsync($"{BaseUrl}/tenant/{tenantId}");
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                var response = await httpClient.GetFromJsonAsync<IEnumerable<LicenseFeatureDto>>($"{BaseUrl}/{licenseId}/features");
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
            var httpClient = _httpClientFactory.CreateClient("ApiClient");
            try
            {
                return await httpClient.GetFromJsonAsync<ApiUsageDto>($"{BaseUrl}/usage/{tenantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API usage for tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}