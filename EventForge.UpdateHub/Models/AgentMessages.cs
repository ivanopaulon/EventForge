namespace EventForge.UpdateHub.Models;

// ── Messages sent FROM the Agent TO the Hub ──

/// <summary>Sent on connection to register/refresh installation state.</summary>
public record RegisterInstallationMessage(
    string InstallationId,
    string InstallationName,
    string? VersionServer,
    string? VersionClient,
    InstallationComponentsDto Components);

/// <summary>Sent periodically to confirm the agent is alive.</summary>
public record HeartbeatMessage(
    string InstallationId,
    string? VersionServer,
    string? VersionClient,
    string Status,
    DateTime Timestamp);

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
    string Checksum);

/// <summary>Asks the agent to send its current status.</summary>
public record RequestStatusCommand(string Reason);

public record InstallationComponentsDto(bool Server, bool Client);
