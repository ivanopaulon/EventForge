using Prym.Web.Services.Updates;

namespace Prym.Web.Services;

/// <summary>
/// Singleton service that tracks update/maintenance state received via SignalR
/// and exposes SuperAdmin update management operations through the Server proxy.
/// </summary>
public interface IUpdateNotificationService
{
    // ── Maintenance state ────────────────────────────────────────────────────

    /// <summary>
    /// True while a download or install phase is actively running (i.e. <see cref="CurrentProgress"/>
    /// is set and the phase is not <c>PackageReceived</c> or <c>AwaitingMaintenanceWindow</c>,
    /// which are shown as a non-blocking snackbar instead).
    /// </summary>
    bool IsActiveUpdate { get; }

    /// <summary>True while the Server component is going through a planned update.</summary>
    bool IsServerMaintenance { get; }

    /// <summary>Latest download/install progress received from the Agent, or null when idle.</summary>
    UpdateProgressPayload? CurrentProgress { get; }

    /// <summary>
    /// True when a package notification snackbar should be shown
    /// (phase is PackageReceived or AwaitingMaintenanceWindow).
    /// </summary>
    bool HasDownloadNotification { get; }

    /// <summary>Component being updated (e.g. "Server").</summary>
    string? MaintenanceComponent { get; }

    /// <summary>Target version being deployed.</summary>
    string? MaintenanceVersion { get; }

    /// <summary>True when a new Client version has been deployed and the browser should reload.</summary>
    bool HasPendingClientUpdate { get; }

    /// <summary>Version of the newly deployed Client.</summary>
    string? ClientUpdateVersion { get; }

    // ── Update management (SuperAdmin) ───────────────────────────────────────

    /// <summary>Number of packages available in the UpdateHub (0 when not configured).</summary>
    int AvailableUpdatesCount { get; }

    /// <summary>
    /// Number of packages downloaded and awaiting operator approval to install (manual-install mode).
    /// Used to show the pending badge on the updates FAB.
    /// </summary>
    int PendingManualInstallsCount { get; }

    /// <summary>
    /// Fired whenever any tracked state changes (maintenance, progress, available/pending counts).
    /// </summary>
    event Action? StateChanged;

    /// <summary>Fetches all packages from the UpdateHub via the Server proxy.</summary>
    Task<IReadOnlyList<PackageSummaryClientDto>> GetPackagesAsync(CancellationToken ct = default);

    /// <summary>Fetches all installations from the UpdateHub via the Server proxy.</summary>
    Task<IReadOnlyList<InstallationSummaryClientDto>> GetInstallationsAsync(CancellationToken ct = default);

    /// <summary>Fetches all packages currently awaiting manual install approval via the Server proxy.</summary>
    Task<IReadOnlyList<PendingInstallClientDto>> GetPendingInstallsAsync(CancellationToken ct = default);

    /// <summary>Sends an update command to an installation via the Server proxy.</summary>
    Task TriggerUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default);

    /// <summary>Sends an InstallNow command to the agent for the specified queued package.</summary>
    Task TriggerInstallNowAsync(Guid installationId, Guid packageId, CancellationToken ct = default);

    /// <summary>Sends an UnblockQueue command to the agent.</summary>
    Task TriggerUnblockQueueAsync(Guid installationId, Guid packageId, bool skipAndRemove, CancellationToken ct = default);

    /// <summary>Refreshes <see cref="AvailableUpdatesCount"/> from the UpdateHub (SuperAdmin only).</summary>
    Task RefreshAvailableUpdatesCountAsync(CancellationToken ct = default);

    /// <summary>Returns the UpdateHub base URL configured on the server, or an empty string if not configured.</summary>
    Task<string> GetHubUrlAsync(CancellationToken ct = default);
}
