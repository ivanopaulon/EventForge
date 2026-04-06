using EventForge.Client.Services.Updates;

namespace EventForge.Client.Services;

/// <summary>
/// Singleton service that tracks update/maintenance state received via SignalR
/// and exposes SuperAdmin update management operations through the Server proxy.
/// </summary>
public interface IUpdateNotificationService
{
    // ── Maintenance state ────────────────────────────────────────────────────

    /// <summary>True while the Server component is going through a planned update.</summary>
    bool IsServerMaintenance { get; }

    /// <summary>Latest download/install progress received from the Agent, or null when idle.</summary>
    UpdateProgressPayload? CurrentProgress { get; }

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
    /// Fired whenever <see cref="IsServerMaintenance"/>, <see cref="HasPendingClientUpdate"/>
    /// or <see cref="AvailableUpdatesCount"/> changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>Fetches all packages from the UpdateHub via the Server proxy.</summary>
    Task<IReadOnlyList<PackageSummaryClientDto>> GetPackagesAsync(CancellationToken ct = default);

    /// <summary>Fetches all installations from the UpdateHub via the Server proxy.</summary>
    Task<IReadOnlyList<InstallationSummaryClientDto>> GetInstallationsAsync(CancellationToken ct = default);

    /// <summary>Sends an update command to an installation via the Server proxy.</summary>
    Task TriggerUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default);

    /// <summary>Refreshes <see cref="AvailableUpdatesCount"/> from the UpdateHub (SuperAdmin only).</summary>
    Task RefreshAvailableUpdatesCountAsync();
}
