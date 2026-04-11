namespace Prym.ManagementHub.Models;

// ── Messages sent FROM the Agent TO the Hub ──

/// <summary>Sent on connection to register/refresh installation state.</summary>
public record RegisterInstallationMessage(
    string InstallationId,
    string InstallationName,
    string? VersionServer,
    string? VersionClient,
    InstallationComponentsDto Components,
    // Rich identity fields — sent on every connect so Hub stays up-to-date
    string? InstallationCode  = null,
    string? Location          = null,
    IReadOnlyList<string>? Tags = null,
    string? MachineName       = null,
    string? OSVersion         = null,
    string? DotNetVersion     = null,
    string? AgentVersion      = null);

/// <summary>Sent periodically to confirm the agent is alive and propagate mutable config (Location, Tags).</summary>
public record HeartbeatMessage(
    string InstallationId,
    string? VersionServer,
    string? VersionClient,
    string Status,
    DateTime Timestamp,
    string? AgentVersion           = null,
    string? Location               = null,
    IReadOnlyList<string>? Tags    = null);

/// <summary>Sent during or after an update to report progress/result.</summary>
public record UpdateProgressMessage(
    string InstallationId,
    Guid UpdateHistoryId,
    string Phase,
    bool IsCompleted,
    bool IsSuccess,
    string? ErrorMessage);

// ── Messages sent FROM the Hub TO the Agent ──

/// <summary>Informs the agent that a new package is available.</summary>
public record UpdateAvailableMessage(
    Guid PackageId,
    string Version,
    string Component,
    string DownloadUrl,
    string Checksum,
    string? ReleaseNotes);

/// <summary>Commands the agent to start updating now.</summary>
public record StartUpdateCommand(
    Guid UpdateHistoryId,
    Guid PackageId,
    string Version,
    string Component,
    string DownloadUrl,
    string Checksum,
    bool IsManualInstall = false);

/// <summary>Asks the agent to start installing a specific queued package immediately, bypassing the maintenance window.</summary>
public record InstallNowCommand(Guid PackageId);

/// <summary>Asks the agent to unblock its install queue. <see cref="SkipAndRemove"/> = true removes the head entry.</summary>
public record UnblockQueueCommand(Guid PackageId, bool SkipAndRemove);

/// <summary>Asks the agent to send its current status.</summary>
public record RequestStatusCommand(string Reason);

public record InstallationComponentsDto(bool Server, bool Client);
