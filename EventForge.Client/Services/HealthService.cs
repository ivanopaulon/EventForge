using EventForge.Client.Services.Updates;
using EventForge.DTOs.Health;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface IHealthService
    {
        Task<HealthStatusDto?> GetHealthAsync();
        Task<DetailedHealthStatusDto?> GetDetailedHealthAsync();

        /// <summary>
        /// Fetches the status of the co-located UpdateAgent from the Server proxy.
        /// Returns null if unauthenticated or if the endpoint is unreachable.
        /// </summary>
        Task<AgentStatusClientDto?> GetAgentStatusAsync();

        /// <summary>
        /// Asks the Server to restart the co-located UpdateAgent Windows Service.
        /// Requires SuperAdmin. Returns (Success, Message).
        /// </summary>
        Task<(bool Success, string Message)> RestartAgentAsync();
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

        public async Task<AgentStatusClientDto?> GetAgentStatusAsync()
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync("api/v1/system/agent-status");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<AgentStatusClientDto>();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent status fetch failed: {Message}", ex.Message);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> RestartAgentAsync()
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsync("api/v1/system/agent-status/restart", null);
                var body = await response.Content.ReadFromJsonAsync<AgentRestartResultClientDto>();
                return (body?.Success == true, body?.Message ?? response.ReasonPhrase ?? "Risposta sconosciuta");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent restart request failed");
                return (false, ex.Message);
            }
        }
    }

    // Mirror of AgentRestartResultDto from the Server
    internal record AgentRestartResultClientDto(bool Success, string Message);
}