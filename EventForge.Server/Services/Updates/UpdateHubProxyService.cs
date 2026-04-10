using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventForge.Server.Services.Updates;

/// <summary>
/// Calls the UpdateHub REST API using the configured AdminApiKey.
/// Throws <see cref="UpdateHubNotConfiguredException"/> when BaseUrl or AdminApiKey is empty.
/// </summary>
public sealed class UpdateHubProxyService : IUpdateHubProxyService
{
    private readonly HttpClient _http;
    private readonly ILogger<UpdateHubProxyService> _logger;
    private readonly bool _configured;

    /// <summary>
    /// Options used when deserialising UpdateHub API responses.
    /// <para>
    /// PropertyNameCaseInsensitive ensures that camelCase JSON properties (e.g. "component")
    /// are matched to PascalCase DTO properties (e.g. "Component").
    /// JsonStringEnumConverter ensures enum-valued fields serialised as strings by the Hub are
    /// accepted — but since the DTOs use <see langword="string"/> properties the converter also
    /// avoids a hard failure when an older Hub binary emits integer enum values.
    /// </para>
    /// </summary>
    private static readonly JsonSerializerOptions _hubJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public UpdateHubProxyService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<UpdateHubProxyService> logger)
    {
        _logger = logger;

        var baseUrl = (configuration["UpdateHub:BaseUrl"] ?? string.Empty).TrimEnd('/');
        var adminKey = configuration["UpdateHub:AdminApiKey"] ?? string.Empty;

        _configured = !string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(adminKey);

        _http = httpClientFactory.CreateClient("UpdateHubProxy");
        if (_configured)
        {
            _http.BaseAddress = new Uri(baseUrl + "/");
            _http.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);
            _http.Timeout = TimeSpan.FromSeconds(15);
        }
    }

    public async Task<IReadOnlyList<PackageSummaryDto>> GetPackagesAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        var list = await _http.GetFromJsonAsync<List<PackageSummaryDto>>("api/v1/packages", _hubJsonOptions, ct) ?? [];
        return [.. list.OrderByDescending(p => p.UploadedAt)];
    }

    public async Task<IReadOnlyList<InstallationSummaryDto>> GetInstallationsAsync(CancellationToken ct = default)
    {
        EnsureConfigured();
        return await _http.GetFromJsonAsync<List<InstallationSummaryDto>>("api/v1/installations", _hubJsonOptions, ct) ?? [];
    }

    public async Task SendUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
    {
        try
        {
            EnsureConfigured();
            using var content = new StringContent(
                JsonSerializer.Serialize(new { PackageId = packageId }),
                Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"api/v1/installations/{installationId}/update", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "SendUpdate failed for installation {InstallationId} package {PackageId}: HTTP {Status} — {Body}",
                    installationId, packageId, response.StatusCode, err);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            // For HttpRequestException from EnsureSuccessStatusCode, status/body are already logged above.
            // For all other exceptions (network, timeout, etc.), log with installation context.
            if (ex is not HttpRequestException)
                _logger.LogError(ex, "Error in SendUpdateAsync for installation {InstallationId} and package {PackageId}: {ErrorMessage}",
                    installationId, packageId, ex.Message);
            throw;
        }
    }

    private void EnsureConfigured()
    {
        if (!_configured)
            throw new UpdateHubNotConfiguredException(
                "UpdateHub is not configured. Set UpdateHub:BaseUrl and UpdateHub:AdminApiKey in appsettings.json.");
    }
}
