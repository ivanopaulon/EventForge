namespace EventForge.UpdateAgent.Models;

// Mirror of the hub message types (kept local to avoid project reference)
public record RegisterInstallationMessage(
    string InstallationId,
    string InstallationName,
    string? VersionServer,
    string? VersionClient,
    InstallationComponentsDto Components);

public record HeartbeatMessage(
    string InstallationId,
    string? VersionServer,
    string? VersionClient,
    string Status,
    DateTime Timestamp);

public record UpdateProgressMessage(
    string InstallationId,
    Guid UpdateHistoryId,
    string Phase,
    bool IsCompleted,
    bool IsSuccess,
    string? ErrorMessage);

public record UpdateAvailableMessage(
    Guid PackageId,
    string Version,
    string Component,
    string DownloadUrl,
    string Checksum,
    string? ReleaseNotes);

public record StartUpdateCommand(
    Guid UpdateHistoryId,
    Guid PackageId,
    string Version,
    string Component,
    string DownloadUrl,
    string Checksum);

public record RequestStatusCommand(string Reason);
public record InstallationComponentsDto(bool Server, bool Client);

/// <summary>Hub → Agent: install a queued package immediately, bypassing the maintenance window.</summary>
public record InstallNowCommand(Guid PackageId);

/// <summary>Hub → Agent: unblock the install queue after a failed update (operator-confirmed).</summary>
public record UnblockQueueCommand(Guid PackageId, bool SkipAndRemove);
