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

    // ── Phase name constants — must match UpdatePhase.ToString() in Prym.UpdateShared ──
    private const string PhasePackageReceived        = "PackageReceived";
    private const string PhaseAwaitingMaintenanceWindow = "AwaitingMaintenanceWindow";
    private const string PhaseCompleted              = "Completed";
    private const string PhaseRollback               = "Rollback";

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
        !string.Equals(_currentProgress.Phase, PhasePackageReceived, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(_currentProgress.Phase, PhaseAwaitingMaintenanceWindow, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when a package has been received and is about to be downloaded or is awaiting install.</summary>
    public bool HasDownloadNotification =>
        _currentProgress is not null &&
        (string.Equals(_currentProgress.Phase, PhasePackageReceived, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(_currentProgress.Phase, PhaseAwaitingMaintenanceWindow, StringComparison.OrdinalIgnoreCase));

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
            var isAwaiting = string.Equals(payload.Phase, PhaseAwaitingMaintenanceWindow, StringComparison.OrdinalIgnoreCase)
                          && payload.IsManualInstall == true;

            // Remove from pending once the install reaches a terminal phase (Completed or Rollback).
            // Note: there is no "Failed" phase — a failed install is always reported as Completed
            // (with IsSuccess=false) or as Rollback when the rollback path ran.
            var isTerminal = string.Equals(payload.Phase, PhaseCompleted, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(payload.Phase, PhaseRollback, StringComparison.OrdinalIgnoreCase);

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
        try
        {
            return await _http.GetAsync<List<PackageSummaryClientDto>>(
                "api/v1/updatehub-proxy/packages", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving update packages");
            throw;
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
            _logger.LogError(ex, "Error retrieving installations");
            throw;
        }
    }

    public async Task<IReadOnlyList<PendingInstallClientDto>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetAsync<List<PendingInstallClientDto>>("api/v1/agent-proxy/pending-installs", ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending installs");
            throw;
        }
    }

    public async Task TriggerUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
    {
        try
        {
            await _http.PostAsync($"api/v1/updatehub-proxy/installations/{installationId}/update", new { PackageId = packageId }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering update for installation {InstallationId}, package {PackageId}", installationId, packageId);
            throw;
        }
    }

    public async Task TriggerInstallNowAsync(Guid installationId, Guid packageId, CancellationToken ct = default)
    {
        try
        {
            await _http.PostAsync("api/v1/agent-proxy/install-now",
                new { InstallationId = installationId, PackageId = packageId }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering install now for installation {InstallationId}, package {PackageId}", installationId, packageId);
            throw;
        }
    }

    public async Task TriggerUnblockQueueAsync(Guid installationId, Guid packageId, bool skipAndRemove, CancellationToken ct = default)
    {
        try
        {
            await _http.PostAsync("api/v1/agent-proxy/unblock-queue", new { PackageId = packageId, SkipAndRemove = skipAndRemove }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering unblock queue for package {PackageId}", packageId);
            throw;
        }
    }

    public async Task RefreshAvailableUpdatesCountAsync(CancellationToken ct = default)
    {
        try
        {
            var packages = await GetPackagesAsync();
            _availableUpdatesCount = packages.Count(p =>
                p.Status.Equals("ReadyToDeploy", StringComparison.OrdinalIgnoreCase));
            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing available updates count");
            _availableUpdatesCount = 0;
        }
    }

    public async Task<string> GetHubUrlAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _http.GetAsync<HubUrlResponse>("api/v1/updatehub-proxy/hub-url", ct);
            return result?.HubUrl ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching Hub URL");
            return string.Empty;
        }
    }

    private record HubUrlResponse(string HubUrl);

    public void Dispose()
    {
        _realtime.ServerMaintenanceStarted -= OnMaintenanceStarted;
        _realtime.ServerMaintenanceEnded -= OnMaintenanceEnded;
        _realtime.ClientUpdateDeployed -= OnClientUpdateDeployed;
        _realtime.UpdateProgressReceived -= OnUpdateProgress;
        _realtime.UpdatesAvailableReceived -= OnUpdatesAvailable;
    }
}
