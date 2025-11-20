using EventForge.DTOs.Health;
using System.Net.Http.Json;

namespace EventForge.Client.Services.Infrastructure
{
    public interface IHealthService
    {
        Task<HealthStatusDto?> GetHealthAsync();
        Task<DetailedHealthStatusDto?> GetDetailedHealthAsync();
    }

    public class HealthService : IHealthService
    {
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
                return await httpClient.GetFromJsonAsync<HealthStatusDto>("api/v1/health");
            }
            catch
            {
                return null;
            }
        }

        public async Task<DetailedHealthStatusDto?> GetDetailedHealthAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                return await httpClient.GetFromJsonAsync<DetailedHealthStatusDto>("api/v1/health/detailed");
            }
            catch
            {
                return null;
            }
        }
    }
}