namespace Prym.Agent.Models;

// Mirror of the hub message types (kept local to avoid project reference)
public record RegisterInstallationMessage(
    string InstallationId,
    string InstallationName,
    string? VersionServer,
    string? VersionClient,
    InstallationComponentsDto Components,
    string? InstallationCode  = null,
    string? Location          = null,
    IReadOnlyList<string>? Tags = null,
    string? MachineName       = null,
    string? OSVersion         = null,
    string? DotNetVersion     = null,
    string? AgentVersion      = null);

public record HeartbeatMessage(
    string InstallationId,
    string? VersionServer,
    string? VersionClient,
    string Status,
    DateTime Timestamp,
    string? AgentVersion           = null,
    string? Location               = null,
    IReadOnlyList<string>? Tags    = null);

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
    string Checksum,
    bool IsManualInstall = false);

public record RequestStatusCommand(string Reason);
public record InstallationComponentsDto(bool Server, bool Client);

public record InstallNowCommand(Guid PackageId);
public record UnblockQueueCommand(Guid PackageId, bool SkipAndRemove);
