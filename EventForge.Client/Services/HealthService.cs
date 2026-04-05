using EventForge.DTOs.Health;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface IHealthService
    {
        Task<HealthStatusDto?> GetHealthAsync();
        Task<DetailedHealthStatusDto?> GetDetailedHealthAsync();
    }

    public class HealthService(
        IHttpClientFactory httpClientFactory,
        ILogger<HealthService> logger) : IHealthService
    {
        private const string BaseUrl = "health";

        public async Task<HealthStatusDto?> GetHealthAsync()
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync("api/v1/health");

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Health check failed with status code {StatusCode}. Reason: {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<HealthStatusDto>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error calling health endpoint: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error calling health endpoint: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<DetailedHealthStatusDto?> GetDetailedHealthAsync()
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync("api/v1/health/detailed");

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Detailed health check failed with status code {StatusCode}. Reason: {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<DetailedHealthStatusDto>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error calling detailed health endpoint: {Message}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error calling detailed health endpoint: {Message}", ex.Message);
                return null;
            }
        }
    }
}