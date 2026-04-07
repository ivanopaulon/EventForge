using EventForge.Client.Services.Updates;

namespace EventForge.Client.Services;

/// <summary>
/// Singleton service that:
/// - listens for MaintenanceStarted / MaintenanceEnded / ClientUpdateDeployed / UpdateProgress /
///   UpdatesAvailable events pushed by the Server via the update-notifications SignalR hub;
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
    // Tracks PackageIds we have received AwaitingMaintenanceWindow for (persists across phase changes)
    private readonly HashSet<Guid> _pendingManualInstallIds = [];

    public bool IsServerMaintenance => _isServerMaintenance;

    /// <summary>
    /// True during an active download/install phase (overlay blocks UI).
    /// False for PackageReceived and AwaitingMaintenanceWindow (shown as snackbar instead).
    /// </summary>
    public bool IsActiveUpdate =>
        _currentProgress is not null &&
        !string.Equals(_currentProgress.Phase, "PackageReceived", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(_currentProgress.Phase, "AwaitingMaintenanceWindow", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when a package has been received and is about to be downloaded or is awaiting install.</summary>
    public bool HasDownloadNotification =>
        _currentProgress is not null &&
        (string.Equals(_currentProgress.Phase, "PackageReceived", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(_currentProgress.Phase, "AwaitingMaintenanceWindow", StringComparison.OrdinalIgnoreCase));

    public string? MaintenanceComponent => _maintenanceComponent;
    public string? MaintenanceVersion => _maintenanceVersion;
    public bool HasPendingClientUpdate => _hasPendingClientUpdate;
    public string? ClientUpdateVersion => _clientUpdateVersion;
    public int AvailableUpdatesCount => _availableUpdatesCount;
    public int PendingManualInstallsCount => _pendingManualInstallIds.Count;
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
        _realtime.UpdatesAvailableReceived += OnUpdatesAvailable;
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
        // A completed install removes it from pending (PackageId not in this payload, so clear all
        // if there are no other awaiting signals — conservative: keep the set as-is and let
        // OnUpdateProgress terminal phases clean up per-package).
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

        // Track packages awaiting manual install so PendingManualInstallsCount stays accurate.
        if (payload.PackageId.HasValue)
        {
            var isAwaiting = string.Equals(payload.Phase, "AwaitingMaintenanceWindow", StringComparison.OrdinalIgnoreCase)
                          && payload.IsManualInstall == true;

            // Remove from pending once the install completes (any terminal phase clears it).
            var isTerminal = string.Equals(payload.Phase, "Completed", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(payload.Phase, "Rollback", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(payload.Phase, "Failed", StringComparison.OrdinalIgnoreCase);

            if (isAwaiting)
                _pendingManualInstallIds.Add(payload.PackageId.Value);
            else if (isTerminal)
                _pendingManualInstallIds.Remove(payload.PackageId.Value);
        }

        StateChanged?.Invoke();
    }

    private void OnUpdatesAvailable(UpdatesAvailablePayload payload)
    {
        _availableUpdatesCount = payload.Count;
        _logger.LogDebug("UpdatesAvailable received: count={Count}", payload.Count);
        StateChanged?.Invoke();
    }

    // ── SuperAdmin proxy operations ──────────────────────────────────────────

    public async Task<IReadOnlyList<PackageSummaryClientDto>> GetPackagesAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<PackageSummaryClientDto>>(
            "api/v1/updatehub-proxy/packages", ct) ?? [];
    }

    public async Task<IReadOnlyList<InstallationSummaryClientDto>> GetInstallationsAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<InstallationSummaryClientDto>>(
            "api/v1/updatehub-proxy/installations", ct) ?? [];
    }

    public async Task<IReadOnlyList<PendingInstallClientDto>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        return await _http.GetAsync<List<PendingInstallClientDto>>("api/v1/agent-proxy/pending-installs", ct) ?? [];
    }

    public async Task TriggerUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
        => await _http.PostAsync($"api/v1/updatehub-proxy/installations/{installationId}/update", new { PackageId = packageId }, ct);

    public async Task TriggerInstallNowAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
        => await _http.PostAsync("api/v1/agent-proxy/install-now", new { PackageId = packageId }, ct);

    public async Task TriggerUnblockQueueAsync(Guid installationId, Guid packageId, bool skipAndRemove, CancellationToken ct = default)
        => await _http.PostAsync("api/v1/agent-proxy/unblock-queue", new { PackageId = packageId, SkipAndRemove = skipAndRemove }, ct);

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
        _realtime.UpdatesAvailableReceived -= OnUpdatesAvailable;
    }
}
