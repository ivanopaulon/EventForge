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
        /// Returns null when the Agent is not configured or unreachable.
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
        public Task<HealthStatusDto?> GetHealthAsync()
            => GetAsync<HealthStatusDto>("api/v1/health");

        public Task<DetailedHealthStatusDto?> GetDetailedHealthAsync()
            => GetAsync<DetailedHealthStatusDto>("api/v1/health/detailed");

        public Task<AgentStatusClientDto?> GetAgentStatusAsync()
            => GetAsync<AgentStatusClientDto>("api/v1/system/agent-status");

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

        // ── Helper ───────────────────────────────────────────────────────────

        private async Task<T?> GetAsync<T>(string relativeUrl) where T : class
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync(relativeUrl);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("GET {Url} failed: {StatusCode}", relativeUrl, response.StatusCode);
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error calling {Url}: {Message}", relativeUrl, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error calling {Url}: {Message}", relativeUrl, ex.Message);
                return null;
            }
        }
    }

    // Mirror of AgentRestartResultDto from the Server
    internal record AgentRestartResultClientDto(bool Success, string Message);
}
