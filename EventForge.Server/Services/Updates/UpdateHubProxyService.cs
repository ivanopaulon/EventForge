using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services.Updates;

/// <summary>
/// Calls the UpdateHub REST API using the configured AdminApiKey.
/// Throws <see cref="UpdateHubNotConfiguredException"/> when BaseUrl or AdminApiKey is empty.
/// </summary>
public sealed class UpdateHubProxyService : IUpdateHubProxyService
{
    private readonly HttpClient _http;
    private readonly ILogger<UpdateHubProxyService> _logger;
    private readonly string _adminKey;

    public UpdateHubProxyService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<UpdateHubProxyService> logger)
    {
        _logger = logger;
        _adminKey = configuration["UpdateHub:AdminApiKey"] ?? string.Empty;

        var baseUrl = configuration["UpdateHub:BaseUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(_adminKey))
        {
            // Create a placeholder client; actual calls will throw before reaching the network.
            _http = httpClientFactory.CreateClient();
        }
        else
        {
            _http = httpClientFactory.CreateClient("UpdateHubProxy");
            _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            _http.DefaultRequestHeaders.Add("X-Admin-Key", _adminKey);
            _http.Timeout = TimeSpan.FromSeconds(15);
        }
    }

    public async Task<IReadOnlyList<PackageSummaryDto>> GetPackagesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var list = await _http.GetFromJsonAsync<List<PackageSummaryDto>>("api/v1/packages", ct)
                   ?? [];
        return list.OrderByDescending(p => p.UploadedAt).ToList();
    }

    public async Task<IReadOnlyList<InstallationSummaryDto>> GetInstallationsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var list = await _http.GetFromJsonAsync<List<InstallationSummaryDto>>("api/v1/installations", ct)
                   ?? [];
        return list;
    }

    public async Task SendUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
    {
        EnsureConfigured();
        var body = JsonSerializer.Serialize(new { PackageId = packageId });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"api/v1/installations/{installationId}/update", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("SendUpdate failed {Status}: {Body}", response.StatusCode, err);
            response.EnsureSuccessStatusCode();
        }
    }

    private void EnsureConfigured()
    {
        var baseUrl = _http.BaseAddress?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(_adminKey))
            throw new UpdateHubNotConfiguredException(
                "UpdateHub is not configured. Set UpdateHub:BaseUrl and UpdateHub:AdminApiKey in appsettings.json.");
    }
}
