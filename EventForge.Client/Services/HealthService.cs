using EventForge.DTOs.Health;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface IHealthService
    {
        Task<HealthStatusDto?> GetHealthAsync();
        Task<DetailedHealthStatusDto?> GetDetailedHealthAsync();
    }

    public class HealthService : IHealthService
    {
        private const string BaseUrl = "health";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HealthService> _logger;

        public HealthService(IHttpClientFactory httpClientFactory, ILogger<HealthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthStatusDto?> GetHealthAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync("api/v1/health");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Health check failed with status code {StatusCode}. Reason: {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }
                
                return await response.Content.ReadFromJsonAsync<HealthStatusDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling health endpoint: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling health endpoint: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<DetailedHealthStatusDto?> GetDetailedHealthAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync("api/v1/health/detailed");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Detailed health check failed with status code {StatusCode}. Reason: {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }
                
                return await response.Content.ReadFromJsonAsync<DetailedHealthStatusDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling detailed health endpoint: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling detailed health endpoint: {Message}", ex.Message);
                return null;
            }
        }
    }
}