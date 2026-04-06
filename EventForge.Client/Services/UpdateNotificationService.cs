using EventForge.Client.Services.Updates;

namespace EventForge.Client.Services;

/// <summary>
/// Singleton service that:
/// - listens for MaintenanceStarted / MaintenanceEnded / ClientUpdateDeployed events
///   pushed by the Server via the update-notifications SignalR hub;
/// - exposes SuperAdmin-only operations (GetPackages, GetInstallations, TriggerUpdate)
///   through the Server proxy (/api/v1/updatehub-proxy/*).
/// </summary>
public sealed class UpdateNotificationService : IUpdateNotificationService, IDisposable
{
    private readonly IHttpClientService _http;
    private readonly IRealtimeService _realtime;
    private readonly ILogger<UpdateNotificationService> _logger;

    // ── Maintenance state ────────────────────────────────────────────────────
    private bool _isServerMaintenance;
    private string? _maintenanceComponent;
    private string? _maintenanceVersion;
    private bool _hasPendingClientUpdate;
    private string? _clientUpdateVersion;
    private int _availableUpdatesCount;
    private UpdateProgressPayload? _currentProgress;

    public bool IsServerMaintenance => _isServerMaintenance;
    public bool IsActiveUpdate =>
        _currentProgress is not null &&
        !string.Equals(_currentProgress.Phase, "AwaitingMaintenanceWindow", StringComparison.OrdinalIgnoreCase);
    public string? MaintenanceComponent => _maintenanceComponent;
    public string? MaintenanceVersion => _maintenanceVersion;
    public bool HasPendingClientUpdate => _hasPendingClientUpdate;
    public string? ClientUpdateVersion => _clientUpdateVersion;
    public int AvailableUpdatesCount => _availableUpdatesCount;
    public UpdateProgressPayload? CurrentProgress => _currentProgress;

    public event Action? StateChanged;

    public UpdateNotificationService(
        IHttpClientService http,
        IRealtimeService realtime,
        ILogger<UpdateNotificationService> logger)
    {
        _http = http;
        _realtime = realtime;
        _logger = logger;

        _realtime.ServerMaintenanceStarted += OnMaintenanceStarted;
        _realtime.ServerMaintenanceEnded += OnMaintenanceEnded;
        _realtime.ClientUpdateDeployed += OnClientUpdateDeployed;
        _realtime.UpdateProgressReceived += OnUpdateProgress;
    }

    // ── SignalR event handlers ───────────────────────────────────────────────

    private void OnMaintenanceStarted(MaintenanceStartedPayload payload)
    {
        _isServerMaintenance = true;
        _maintenanceComponent = payload.Component;
        _maintenanceVersion = payload.Version;
        _logger.LogInformation("Maintenance started: {Component} v{Version}", payload.Component, payload.Version);
        StateChanged?.Invoke();
    }

    private void OnMaintenanceEnded(MaintenanceEndedPayload payload)
    {
        _isServerMaintenance = false;
        _maintenanceComponent = null;
        _maintenanceVersion = null;
        _currentProgress = null;
        _logger.LogInformation("Maintenance ended: {Component} v{Version}", payload.Component, payload.Version);
        StateChanged?.Invoke();
    }

    private void OnClientUpdateDeployed(ClientUpdateDeployedPayload payload)
    {
        _hasPendingClientUpdate = true;
        _clientUpdateVersion = payload.Version;
        _currentProgress = null;
        _logger.LogInformation("Client update deployed: v{Version}", payload.Version);
        StateChanged?.Invoke();
    }

    private void OnUpdateProgress(UpdateProgressPayload payload)
    {
        _currentProgress = payload;
        StateChanged?.Invoke();
    }

    // ── SuperAdmin proxy operations ──────────────────────────────────────────

    public async Task<IReadOnlyList<PackageSummaryClientDto>> GetPackagesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetAsync<List<PackageSummaryClientDto>>(
                "api/v1/updatehub-proxy/packages", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch packages from UpdateHub proxy");
            return [];
        }
    }

    public async Task<IReadOnlyList<InstallationSummaryClientDto>> GetInstallationsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetAsync<List<InstallationSummaryClientDto>>(
                "api/v1/updatehub-proxy/installations", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch installations from UpdateHub proxy");
            return [];
        }
    }

    public async Task TriggerUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
        => await _http.PostAsync<object>(
            $"api/v1/updatehub-proxy/installations/{installationId}/update",
            new { PackageId = packageId },
            ct);

    public async Task RefreshAvailableUpdatesCountAsync()
    {
        try
        {
            var packages = await GetPackagesAsync();
            _availableUpdatesCount = packages.Count(p =>
                p.Status.Equals("ReadyToDeploy", StringComparison.OrdinalIgnoreCase));
            StateChanged?.Invoke();
        }
        catch
        {
            _availableUpdatesCount = 0;
        }
    }

    public void Dispose()
    {
        _realtime.ServerMaintenanceStarted -= OnMaintenanceStarted;
        _realtime.ServerMaintenanceEnded -= OnMaintenanceEnded;
        _realtime.ClientUpdateDeployed -= OnClientUpdateDeployed;
        _realtime.UpdateProgressReceived -= OnUpdateProgress;
    }
}
