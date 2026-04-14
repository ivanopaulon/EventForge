namespace Prym.Agent.Services;

/// <summary>Detects the currently installed versions of Server and Client.</summary>
public class VersionDetectorService(AgentOptions options, ILogger<VersionDetectorService> logger)
{
    // ── Version cache ─────────────────────────────────────────────────────────
    // version.txt is read on every heartbeat, every dashboard page-load, and on
    // every UpdateAvailable check.  A short TTL avoids repeated disk I/O while
    // keeping values fresh enough for the automatic update channel.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
    private readonly Lock _cacheLock = new();
    // Async locks ensure only one thread reads from disk per cache miss (double-check
    // lock pattern for async code).
    private readonly SemaphoreSlim _serverReadLock = new(1, 1);
    private readonly SemaphoreSlim _clientReadLock = new(1, 1);
    private (string? Version, DateTime CachedAt) _serverCache;
    private (string? Version, DateTime CachedAt) _clientCache;

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<string?> GetServerVersionAsync()
    {
        lock (_cacheLock)
        {
            if (DateTime.UtcNow - _serverCache.CachedAt < CacheTtl)
                return _serverCache.Version;
        }
        // Async double-check: acquire the semaphore, then re-check before reading disk.
        // Only one thread reads at a time; subsequent waiters find a warm cache.
        await _serverReadLock.WaitAsync();
        try
        {
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _serverCache.CachedAt < CacheTtl)
                    return _serverCache.Version;
            }
            var version = await ReadComponentVersionAsync(
                options.Components.Server.Enabled,
                options.Components.Server.DeployPath,
                serverExeFallback: true);
            lock (_cacheLock) { _serverCache = (version, DateTime.UtcNow); }
            return version;
        }
        finally
        {
            _serverReadLock.Release();
        }
    }

    public async Task<string?> GetClientVersionAsync()
    {
        lock (_cacheLock)
        {
            if (DateTime.UtcNow - _clientCache.CachedAt < CacheTtl)
                return _clientCache.Version;
        }
        await _clientReadLock.WaitAsync();
        try
        {
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _clientCache.CachedAt < CacheTtl)
                    return _clientCache.Version;
            }
            var version = await ReadComponentVersionAsync(
                options.Components.Client.Enabled,
                options.Components.Client.DeployPath,
                serverExeFallback: false);
            lock (_cacheLock) { _clientCache = (version, DateTime.UtcNow); }
            return version;
        }
        finally
        {
            _clientReadLock.Release();
        }
    }

    /// <summary>
    /// Forces the next calls to <see cref="GetServerVersionAsync"/> and
    /// <see cref="GetClientVersionAsync"/> to re-read from disk, bypassing the cache.
    /// Call after a successful component deployment so the new version is reported
    /// to the Hub on the next heartbeat.
    /// </summary>
    public void InvalidateVersionCache()
    {
        lock (_cacheLock)
        {
            _serverCache = default;
            _clientCache = default;
        }
    }

    // Cached agent version — reflection is only needed once; the value never changes at runtime.
    private readonly Lazy<string> _agentVersion = new(ReadAgentVersion, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Returns the version of the running UpdateAgent process itself.</summary>
    public string GetAgentVersion() => _agentVersion.Value;

    private static string ReadAgentVersion()
    {
        try
        {
            var asm = typeof(VersionDetectorService).Assembly;
            var infoAttr = System.Reflection.CustomAttributeExtensions
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(asm);
            return infoAttr?.InformationalVersion
                   ?? asm.GetName().Version?.ToString()
                   ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Reads the installed version for a component:
    ///   1. Returns <see langword="null"/> immediately when the component is disabled or its deploy
    ///      path is absent.
    ///   2. Reads <c>version.txt</c> from the deploy directory (preferred).
    ///   3. Falls back to <c>FileVersionInfo</c> on <c>EventForge.Server.exe</c> when
    ///      <paramref name="serverExeFallback"/> is <see langword="true"/>.
    /// </summary>
    private async Task<string?> ReadComponentVersionAsync(
        bool enabled, string? deployPath, bool serverExeFallback)
    {
        if (!enabled) return null;

        if (string.IsNullOrWhiteSpace(deployPath) || !Directory.Exists(deployPath))
        {
            logger.LogDebug("Deploy path not found or empty: '{Path}'", deployPath);
            return null;
        }

        try
        {
            var versionFile = Path.Combine(deployPath, "version.txt");
            if (File.Exists(versionFile))
            {
                var v = (await File.ReadAllTextAsync(versionFile)).Trim();
                if (!string.IsNullOrEmpty(v)) return v;
                logger.LogWarning(
                    "version.txt at '{Path}' exists but is empty{Fallback}.",
                    versionFile,
                    serverExeFallback ? " — falling back to assembly FileVersionInfo" : string.Empty);
            }

            if (serverExeFallback)
            {
                var exePath = Directory.EnumerateFiles(deployPath, "EventForge.Server.exe",
                    SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (exePath is not null)
                {
                    var fileVersion = System.Diagnostics.FileVersionInfo
                        .GetVersionInfo(exePath).FileVersion;
                    if (!string.IsNullOrWhiteSpace(fileVersion)) return fileVersion;
                    logger.LogDebug("FileVersion is empty for {Exe}", exePath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect version from {Path}", deployPath);
        }

        return null;
    }
}
