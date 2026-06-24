using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Updates;

/// <summary>
/// Calls the UpdateAgent REST API directly at the configured <c>Agent:LocalUrl</c>.
/// Throws <see cref="AgentNotConfiguredException"/> when LocalUrl is empty.
///
/// <para>
/// Authentication: when <c>Agent:InternalApiToken</c> is configured the service adds an
/// <c>X-Agent-Internal-Token</c> header to every request so the Agent's
/// <c>BasicAuthMiddleware</c> can validate internal calls. When the token is absent the
/// legacy unauthenticated localhost-trust model applies (Agent must bind to localhost).
/// </para>
/// </summary>
public sealed class AgentUpdateProxyService : IAgentUpdateProxyService
{
    private const string InternalTokenHeader = "X-Agent-Internal-Token";

    private readonly HttpClient _http;
    private readonly ILogger<AgentUpdateProxyService> _logger;
    private readonly bool _configured;
    private readonly string _internalApiToken;

    public AgentUpdateProxyService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AgentUpdateProxyService> logger)
    {
        _logger = logger;
        _internalApiToken = configuration["Agent:InternalApiToken"] ?? string.Empty;

        var agentUrl = (configuration["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        _configured = !string.IsNullOrWhiteSpace(agentUrl);

        _http = httpClientFactory.CreateClient("AgentUpdateProxy");
        if (_configured)
        {
            _http.BaseAddress = new Uri(agentUrl + "/");
            _http.Timeout = TimeSpan.FromSeconds(15);
            // Add the token as a default header so every request includes it automatically.
            if (!string.IsNullOrWhiteSpace(_internalApiToken))
                _http.DefaultRequestHeaders.Add(InternalTokenHeader, _internalApiToken);
        }
    }

    public async Task<IReadOnlyList<AgentPendingInstallDto>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();
            return await _http.GetFromJsonAsync<List<AgentPendingInstallDto>>("api/agent/pending-installs", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPendingInstallsAsync.");
            throw;
        }
    }

    public async Task TriggerInstallNowAsync(Guid packageId, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriggerInstallNowAsync for package {PackageId}.", packageId);
            throw;
        }
    }

    public async Task TriggerUnblockQueueAsync(Guid packageId, bool skipAndRemove, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriggerUnblockQueueAsync for package {PackageId}.", packageId);
            throw;
        }
    }

    private void EnsureConfigured()
    {
        if (!_configured)
            throw new AgentNotConfiguredException(
                "Agent is not configured. Set Agent:LocalUrl in appsettings.json.");
    }
}


    public async Task<IReadOnlyList<AgentPendingInstallDto>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();
            return await _http.GetFromJsonAsync<List<AgentPendingInstallDto>>("api/agent/pending-installs", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPendingInstallsAsync.");
            throw;
        }
    }

    public async Task TriggerInstallNowAsync(Guid packageId, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriggerInstallNowAsync for package {PackageId}.", packageId);
            throw;
        }
    }

    public async Task TriggerUnblockQueueAsync(Guid packageId, bool skipAndRemove, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriggerUnblockQueueAsync for package {PackageId}.", packageId);
            throw;
        }
    }

    private void EnsureConfigured()
    {
        if (!_configured)
            throw new AgentNotConfiguredException(
                "Agent is not configured. Set Agent:LocalUrl in appsettings.json.");
    }
}
