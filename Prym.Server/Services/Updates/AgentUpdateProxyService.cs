using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Prym.Server.Services.Updates;

/// <summary>
/// Calls the UpdateAgent REST API directly at the configured <c>Agent:LocalUrl</c>.
/// Throws <see cref="AgentNotConfiguredException"/> when LocalUrl is empty.
/// </summary>
public sealed class AgentUpdateProxyService : IAgentUpdateProxyService
{
    private readonly HttpClient _http;
    private readonly ILogger<AgentUpdateProxyService> _logger;
    private readonly bool _configured;

    public AgentUpdateProxyService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AgentUpdateProxyService> logger)
    {
        _logger = logger;

        var agentUrl = (configuration["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        _configured = !string.IsNullOrWhiteSpace(agentUrl);

        _http = httpClientFactory.CreateClient("AgentUpdateProxy");
        if (_configured)
        {
            _http.BaseAddress = new Uri(agentUrl + "/");
            _http.Timeout = TimeSpan.FromSeconds(15);
        }
    }

    public async Task<IReadOnlyList<AgentPendingInstallDto>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        return await _http.GetFromJsonAsync<List<AgentPendingInstallDto>>("api/agent/pending-installs", ct) ?? [];
    }

    public async Task TriggerInstallNowAsync(Guid packageId, CancellationToken ct = default)
    {
        EnsureConfigured();
        var content = new StringContent(
            JsonSerializer.Serialize(new { PackageId = packageId }),
            Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("api/agent/install-now", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Agent InstallNow failed {Status}: {Body}", response.StatusCode, err);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task TriggerUnblockQueueAsync(Guid packageId, bool skipAndRemove, CancellationToken ct = default)
    {
        EnsureConfigured();
        var content = new StringContent(
            JsonSerializer.Serialize(new { PackageId = packageId, SkipAndRemove = skipAndRemove }),
            Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("api/agent/unblock-queue", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Agent UnblockQueue failed {Status}: {Body}", response.StatusCode, err);
            response.EnsureSuccessStatusCode();
        }
    }

    private void EnsureConfigured()
    {
        if (!_configured)
            throw new AgentNotConfiguredException(
                "Agent is not configured. Set Agent:LocalUrl in appsettings.json.");
    }
}
