namespace EventForge.Client.Services.Updates;

// ── Client-side mirror DTOs ───────────────────────────────────────────────────
// These mirror the records in EventForge.Server.Services.Updates.IUpdateHubProxyService
// and are deserialized from the Server's /api/v1/updatehub-proxy/* responses.

public record PackageSummaryClientDto(
    Guid Id,
    string Version,
    string Component,
    string? ReleaseNotes,
    string? Checksum,
    long FileSizeBytes,
    DateTime UploadedAt,
    string Status,
    bool IsManualInstall = false);

public record InstallationSummaryClientDto(
    Guid Id,
    string Name,
    string? Location,
    string? InstalledVersionServer,
    string? InstalledVersionClient,
    string Status,
    DateTime? LastSeen,
    bool IsConnected);

/// <summary>A package queued on an agent, waiting for operator approval to install.</summary>
public record PendingInstallClientDto(
    Guid InstallationId,
    string InstallationName,
    bool IsConnected,
    Guid HistoryId,
    Guid PackageId,
    string? Component,
    string? Version,
    bool IsManualInstall,
    DateTime QueuedAt);

public record MaintenanceStartedPayload(string? Component, string? Version, DateTime StartedAt);
public record MaintenanceEndedPayload(string? Component, string? Version, DateTime EndedAt);
public record ClientUpdateDeployedPayload(string? Component, string? Version, DateTime DeployedAt);

/// <summary>Real-time download/install progress forwarded by the Agent through the Server.</summary>
public record UpdateProgressPayload(
    string? Component,
    string? Version,
    string? Phase,
    int? PercentComplete,
    string? FormattedDownloaded,
    string? FormattedTotal,
    string? FormattedSpeed,
    string? Eta,
    DateTime SentAt,
    bool? IsManualInstall = null,
    Guid? PackageId = null,
    string? NextWindowAt = null,
    string? Detail = null);

/// <summary>Count of ReadyToDeploy packages pushed periodically to SuperAdmin clients.</summary>
public record UpdatesAvailablePayload(int Count);

/// <summary>Agent status as returned by GET /api/v1/system/agent-status.</summary>
public record AgentStatusClientDto(
    bool Reachable,
    string Status,
    string? InstallationName,
    string? AgentVersion,
    string? ServerVersion,
    string? ClientVersion,
    string? HubConnectionState,
    DateTime? LastHeartbeatAt,
    DateTime ProbedAt,
    DateTime? UnreachableSinceUtc,
    int AutoRestartAfterMinutes);
