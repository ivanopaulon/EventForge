using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of IServerConfigService.
/// Uses IJSRuntime to read/write localStorage and IConfiguration as default fallback.
/// </summary>
public class ServerConfigService(
    IJSRuntime jsRuntime,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<ServerConfigService> logger) : IServerConfigService
{
    private const string LocalStorageKey = "ef_server_url";

    private bool _initialized = false;
    private string _url = "";

    public async Task<string> GetServerUrlAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_initialized)
            {
                await LoadFromStorageAsync();
            }

            return _url;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting server URL");
            throw;
        }
    }

    public async Task SetServerUrlAsync(string url, CancellationToken ct = default)
    {
        try
        {
            url = NormalizeUrl(url);
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalStorageKey, url);
            _url = url;
            _initialized = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting server URL to {Url}", url);
            throw;
        }
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
    {
        try
        {
            var url = await GetServerUrlAsync();
            return !string.IsNullOrWhiteSpace(url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if server is configured");
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        url = NormalizeUrl(url);

        try
        {
            // Use a short-lived client from the factory (avoids socket exhaustion from raw HttpClient)
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync($"{url}api/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Connection test failed for URL: {Url}", url);
            return false;
        }
    }

    public async Task ResetToDefaultAsync(CancellationToken ct = default)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalStorageKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove server URL from localStorage");
        }

        _initialized = false;
        _url = "";
    }

    private async Task LoadFromStorageAsync()
    {
        try
        {
            var stored = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", LocalStorageKey);
            _url = !string.IsNullOrWhiteSpace(stored)
                ? stored
                : (configuration["ApiSettings:BaseUrl"] ?? "");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load server URL from localStorage, using appsettings default");
            _url = configuration["ApiSettings:BaseUrl"] ?? "";
        }

        _initialized = true;
    }

    private static string NormalizeUrl(string url)
    {
        url = url.Trim();
        if (!string.IsNullOrWhiteSpace(url) && !url.EndsWith('/'))
            url += "/";
        return url;
    }
}
