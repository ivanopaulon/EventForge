namespace EventForge.UpdateHub.Services;

public interface IInstallationService
{
    Task<Installation?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task<Installation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Installation?> GetByInstallationCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Installation>> GetAllAsync(CancellationToken ct = default);
    Task UpdateLastSeenAsync(Guid id, string? versionServer, string? versionClient, InstallationStatus status, CancellationToken ct = default);
    Task<Installation> CreateAsync(Installation installation, CancellationToken ct = default);
    Task<UpdateHistory> StartUpdateHistoryAsync(Guid installationId, Guid packageId, string? fromVersionServer, string? fromVersionClient, CancellationToken ct = default);
    Task CompleteUpdateHistoryAsync(Guid historyId, UpdateHistoryStatus status, string? errorMessage, bool rolledBack, CancellationToken ct = default);
    Task UpdateProgressPhaseAsync(Guid historyId, string phase, CancellationToken ct = default);
    /// <summary>Revoke the installation's API key. The agent will receive 403 until reinstated.</summary>
    Task<bool> RevokeAsync(Guid id, string? reason, CancellationToken ct = default);
    /// <summary>Reinstate a previously revoked installation.</summary>
    Task<bool> ReinstateAsync(Guid id, CancellationToken ct = default);
    /// <summary>Re-issue a new API key for an installation (invalidates the old one).</summary>
    Task<string?> ReissueApiKeyAsync(Guid id, CancellationToken ct = default);
}
