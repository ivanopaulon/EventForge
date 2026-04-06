using Microsoft.EntityFrameworkCore;

namespace EventForge.UpdateHub.Services;

public class InstallationService(UpdateHubDbContext db) : IInstallationService
{
    public Task<Installation?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.ApiKey == apiKey, ct);

    public Task<Installation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Installation?> GetByInstallationCodeAsync(string code, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.InstallationCode == code, ct);

    public async Task<IReadOnlyList<Installation>> GetAllAsync(CancellationToken ct = default)
        => await db.Installations.OrderBy(x => x.Name).ToListAsync(ct);

    public async Task UpdateLastSeenAsync(Guid id, string? versionServer, string? versionClient, InstallationStatus status, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return;
        installation.LastSeen = DateTime.UtcNow;
        if (versionServer is not null) installation.InstalledVersionServer = versionServer;
        if (versionClient is not null) installation.InstalledVersionClient = versionClient;
        installation.Status = status;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Installation> CreateAsync(Installation installation, CancellationToken ct = default)
    {
        db.Installations.Add(installation);
        await db.SaveChangesAsync(ct);
        return installation;
    }

    public async Task<UpdateHistory> StartUpdateHistoryAsync(Guid installationId, Guid packageId, string? fromVersionServer, string? fromVersionClient, CancellationToken ct = default)
    {
        var pkg = await db.UpdatePackages.FindAsync([packageId], ct);
        var history = new UpdateHistory
        {
            InstallationId = installationId,
            PackageId = packageId,
            Status = UpdateHistoryStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            FromVersion = pkg?.Component == PackageComponent.Server ? fromVersionServer : fromVersionClient,
            ToVersion = pkg?.Version,
            PhaseDescription = "Starting"
        };
        db.UpdateHistories.Add(history);
        await db.SaveChangesAsync(ct);
        return history;
    }

    public async Task CompleteUpdateHistoryAsync(Guid historyId, UpdateHistoryStatus status, string? errorMessage, bool rolledBack, CancellationToken ct = default)
    {
        var history = await db.UpdateHistories.FindAsync([historyId], ct);
        if (history is null) return;
        history.Status = status;
        history.ErrorMessage = errorMessage;
        history.CompletedAt = DateTime.UtcNow;
        history.RolledBack = rolledBack;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateProgressPhaseAsync(Guid historyId, string phase, CancellationToken ct = default)
    {
        var history = await db.UpdateHistories.FindAsync([historyId], ct);
        if (history is null) return;
        history.PhaseDescription = phase;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RevokeAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return false;
        installation.IsRevoked = true;
        installation.RevokedAt = DateTime.UtcNow;
        installation.RevokedReason = reason;
        installation.Status = InstallationStatus.Offline;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReinstateAsync(Guid id, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return false;
        installation.IsRevoked = false;
        installation.RevokedAt = null;
        installation.RevokedReason = null;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> ReissueApiKeyAsync(Guid id, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return null;
        var newKey = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();
        installation.ApiKey = newKey;
        installation.IsRevoked = false;
        installation.RevokedAt = null;
        installation.RevokedReason = null;
        await db.SaveChangesAsync(ct);
        return newKey;
    }
}
