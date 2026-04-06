namespace EventForge.UpdateHub.Services;

public interface IInstallationService
{
    Task<Installation?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task<Installation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Installation>> GetAllAsync(CancellationToken ct = default);
    Task UpdateLastSeenAsync(Guid id, string? versionServer, string? versionClient, InstallationStatus status, CancellationToken ct = default);
    Task<Installation> CreateAsync(Installation installation, CancellationToken ct = default);
    Task<UpdateHistory> StartUpdateHistoryAsync(Guid installationId, Guid packageId, string? fromVersionServer, string? fromVersionClient, CancellationToken ct = default);
    Task CompleteUpdateHistoryAsync(Guid historyId, UpdateHistoryStatus status, string? errorMessage, bool rolledBack, CancellationToken ct = default);
    Task UpdateProgressPhaseAsync(Guid historyId, string phase, CancellationToken ct = default);
}
