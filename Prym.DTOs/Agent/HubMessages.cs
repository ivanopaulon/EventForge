namespace Prym.DTOs.Agent;

// Hub message types shared between Prym.Agent and EventForge.Server (UpdateHub).
// Kept in Prym.DTOs so both projects reference a single authoritative definition.

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
    string? AgentVersion      = null,
    string? LocalIpAddress    = null,
    string? PublicIpAddress   = null);

public record HeartbeatMessage(
    string InstallationId,
    string? VersionServer,
    string? VersionClient,
    string Status,
    DateTime Timestamp,
    string? AgentVersion           = null,
    string? Location               = null,
    IReadOnlyList<string>? Tags    = null,
    string? PublicIpAddress        = null);

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
