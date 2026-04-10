namespace EventForge.Client.Services;

/// <summary>
/// Service for managing the dynamic server URL configuration.
/// Reads from localStorage first, then falls back to appsettings.json.
/// </summary>
public interface IServerConfigService
{
    /// <summary>
    /// Returns the current server base URL.
    /// Prefers localStorage["ef_server_url"], falls back to ApiSettings:BaseUrl from appsettings.json.
    /// </summary>
    Task<string> GetServerUrlAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists a new server URL to localStorage and updates the in-memory cache.
    /// </summary>
    Task SetServerUrlAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Returns true if a non-empty server URL is available (from localStorage or appsettings).
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);

    /// <summary>
    /// Tests connectivity to the given URL by calling GET {url}/api/v1/health with a 5-second timeout.
    /// Returns true if the server responds with a success status code.
    /// </summary>
    Task<bool> TestConnectionAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Removes the localStorage entry so the default appsettings URL is used again.
    /// </summary>
    Task ResetToDefaultAsync(CancellationToken ct = default);
}
