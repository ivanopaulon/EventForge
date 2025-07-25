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
        private readonly HttpClient _httpClient;

        public HealthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HealthStatusDto?> GetHealthAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<HealthStatusDto>("api/v1/health");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<DetailedHealthStatusDto?> GetDetailedHealthAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DetailedHealthStatusDto>("api/v1/health/detailed");
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}