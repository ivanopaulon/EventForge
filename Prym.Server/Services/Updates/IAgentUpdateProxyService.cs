namespace Prym.Server.Services.Updates;

/// <summary>
/// Raised when the Agent is not reachable or its URL is not configured.
/// </summary>
public sealed class AgentNotConfiguredException(string message) : Exception(message);

/// <summary>
/// Proxies update-queue management requests to the co-located UpdateAgent's REST API
/// (at <c>Agent:LocalUrl</c>), on behalf of SuperAdmin users.
/// </summary>
public interface IAgentUpdateProxyService
{
    /// <summary>
    /// Returns all packages currently queued on the agent awaiting installation,
    /// including queue-head flag and blocked state.
    /// </summary>
    Task<IReadOnlyList<AgentPendingInstallDto>> GetPendingInstallsAsync(CancellationToken ct = default);

    /// <summary>
    /// Signals the agent to install the specified queued package immediately,
    /// bypassing the configured maintenance window.
    /// </summary>
    Task TriggerInstallNowAsync(Guid packageId, CancellationToken ct = default);

    /// <summary>
    /// Signals the agent to unblock its install queue.
    /// When <paramref name="skipAndRemove"/> is <see langword="true"/>, the blocking entry
    /// is removed from the queue; otherwise the queue retries at the next maintenance window.
    /// </summary>
    Task TriggerUnblockQueueAsync(Guid packageId, bool skipAndRemove, CancellationToken ct = default);
}

/// <summary>Represents a pending update entry as reported by the Agent's REST API.</summary>
public record AgentPendingInstallDto(
    string InstallationId,
    string InstallationName,
    Guid PackageId,
    string? Component,
    string? Version,
    bool IsManualInstall,
    DateTime QueuedAt,
    bool IsQueueHead,
    bool IsBlocked,
    string? BlockedReason,
    bool FileExists);
