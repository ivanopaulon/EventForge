namespace EventForge.UpdateHub.Services;

/// <summary>Data access and business logic for managing registered Agent installations.</summary>
public interface IInstallationService
{
    /// <summary>Returns the installation whose <see cref="Installation.ApiKey"/> matches <paramref name="apiKey"/>, or <see langword="null"/>.</summary>
    Task<Installation?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default);

    /// <summary>Returns the installation with the given <paramref name="id"/>, or <see langword="null"/>.</summary>
    Task<Installation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the installation whose <see cref="Installation.InstallationCode"/> matches <paramref name="code"/>, or <see langword="null"/>.</summary>
    Task<Installation?> GetByInstallationCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns all registered installations ordered by name.</summary>
    Task<IReadOnlyList<Installation>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the <see cref="Installation.LastSeen"/>, version, and status fields.
    /// Only non-<see langword="null"/> version values overwrite the stored values.
    /// </summary>
    Task UpdateLastSeenAsync(Guid id, string? versionServer, string? versionClient, InstallationStatus status, CancellationToken ct = default);

    /// <summary>Updates full registration info sent by the agent on every SignalR connect.</summary>
    Task UpdateRegistrationInfoAsync(Guid id, RegistrationInfo info, CancellationToken ct = default);

    /// <summary>Persists a new <see cref="Installation"/> record and returns the saved entity.</summary>
    Task<Installation> CreateAsync(Installation installation, CancellationToken ct = default);

    /// <summary>Creates an <see cref="UpdateHistory"/> record in <c>InProgress</c> state and returns it.</summary>
    Task<UpdateHistory> StartUpdateHistoryAsync(Guid installationId, Guid packageId, string? fromVersionServer, string? fromVersionClient, CancellationToken ct = default);

    /// <summary>Marks an existing <see cref="UpdateHistory"/> record as completed (success or failure).</summary>
    Task CompleteUpdateHistoryAsync(Guid historyId, UpdateHistoryStatus status, string? errorMessage, bool rolledBack, CancellationToken ct = default);

    /// <summary>Updates the <see cref="UpdateHistory.PhaseDescription"/> for an in-progress update.</summary>
    Task UpdateProgressPhaseAsync(Guid historyId, string phase, CancellationToken ct = default);

    /// <summary>Revoke the installation's API key. The agent will receive 403 until reinstated.</summary>
    Task<bool> RevokeAsync(Guid id, string? reason, CancellationToken ct = default);

    /// <summary>Reinstate a previously revoked installation.</summary>
    Task<bool> ReinstateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Re-issue a new API key for an installation (invalidates the old one).</summary>
    Task<string?> ReissueApiKeyAsync(Guid id, CancellationToken ct = default);

    /// <summary>Sets the update mode (Automatic or Manual) for an installation.</summary>
    Task SetUpdateModeAsync(Guid id, InstallationUpdateMode mode, CancellationToken ct = default);
}

/// <summary>Rich identity payload sent by the agent on every SignalR connect.</summary>
public record RegistrationInfo(
    string? Name,
    string? Location,
    string? VersionServer,
    string? VersionClient,
    string? MachineName,
    string? OSVersion,
    string? DotNetVersion,
    string? AgentVersion,
    string? IpAddress,
    string? Tags,
    InstallationStatus Status = InstallationStatus.Online);
