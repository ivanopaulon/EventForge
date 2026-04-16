using Prym.Web.Services.Updates;
using Prym.DTOs.Health;
using System.Net.Http.Json;

namespace Prym.Web.Services
{
    public interface IHealthService
    {
        Task<HealthStatusDto?> GetHealthAsync(CancellationToken ct = default);
        Task<DetailedHealthStatusDto?> GetDetailedHealthAsync(CancellationToken ct = default);

        /// <summary>
        /// Fetches the status of the co-located UpdateAgent from the Server proxy.
        /// Returns null when the Agent is not configured or unreachable.
        /// </summary>
        Task<AgentStatusClientDto?> GetAgentStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Asks the Server to restart the co-located UpdateAgent Windows Service.
        /// Requires SuperAdmin. Returns (Success, Message).
        /// </summary>
        Task<(bool Success, string Message)> RestartAgentAsync(CancellationToken ct = default);
    }

    public class HealthService(
        IHttpClientFactory httpClientFactory,
        IHttpClientService httpClientService,
        ILogger<HealthService> logger) : IHealthService
    {
        public Task<HealthStatusDto?> GetHealthAsync(CancellationToken ct = default)
            => GetAnonymousAsync<HealthStatusDto>("api/v1/health");

        public Task<DetailedHealthStatusDto?> GetDetailedHealthAsync(CancellationToken ct = default)
            => GetAnonymousAsync<DetailedHealthStatusDto>("api/v1/health/detailed");

        /// <summary>
        /// Uses the authenticated <see cref="IHttpClientService"/> so the JWT Bearer token
        /// is included — required because <c>GET api/v1/system/agent-status</c> has [Authorize].
        /// </summary>
        public async Task<AgentStatusClientDto?> GetAgentStatusAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<AgentStatusClientDto>("api/v1/system/agent-status", ct);

            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent status request failed");
                return null;
            }
        }

        /// <summary>
        /// Uses the authenticated <see cref="IHttpClientService"/> so the JWT Bearer token
        /// is included — required because <c>POST api/v1/system/agent-status/restart</c> has [Authorize(Roles="SuperAdmin")].
        /// </summary>
        public async Task<(bool Success, string Message)> RestartAgentAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.PostAsync<object?, AgentRestartResultClientDto>(
                    "api/v1/system/agent-status/restart", null);
                return (result?.Success == true, result?.Message ?? "Risposta sconosciuta");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Agent restart request failed");
                return (false, ex.Message);
            }
        }

        // ── Anonymous helper — for public [AllowAnonymous] health endpoints ───────

        private async Task<T?> GetAnonymousAsync<T>(string relativeUrl) where T : class
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
