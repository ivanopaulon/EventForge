namespace EventForge.Server.Services.Updates;

/// <summary>
/// Raised when the UpdateHub BaseUrl or AdminApiKey is not configured.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public sealed class UpdateHubNotConfiguredException(string message) : Exception(message);

/// <summary>
/// Proxies REST requests to the UpdateHub on behalf of SuperAdmin users.
/// </summary>
public interface IUpdateHubProxyService
{
    /// <summary>Returns all packages from the UpdateHub, ordered by upload date descending.</summary>
    Task<IReadOnlyList<PackageSummaryDto>> GetPackagesAsync(CancellationToken ct = default);

    /// <summary>Returns all registered installations with their online/offline state.</summary>
    Task<IReadOnlyList<InstallationSummaryDto>> GetInstallationsAsync(CancellationToken ct = default);

    /// <summary>Sends the specified package to an installation as an update command.</summary>
    Task SendUpdateAsync(Guid installationId, Guid packageId, CancellationToken ct = default);
}

// ── Inline DTOs ──────────────────────────────────────────────────────────────

public record PackageSummaryDto(
    Guid Id,
    string Version,
    string Component,
    string? ReleaseNotes,
    string? Checksum,
    long FileSizeBytes,
    DateTime UploadedAt,
    string Status);

public record InstallationSummaryDto(
    Guid Id,
    string Name,
    string? Location,
    string? InstalledVersionServer,
    string? InstalledVersionClient,
    string Status,
    DateTime? LastSeen,
    bool IsConnected);
