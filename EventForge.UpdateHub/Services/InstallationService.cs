using Microsoft.EntityFrameworkCore;

namespace EventForge.UpdateHub.Services;

public class InstallationService(UpdateHubDbContext db, IConnectionTracker connectionTracker) : IInstallationService
{
    public Task<Installation?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.ApiKey == apiKey, ct);

    public Task<Installation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Installation?> GetByInstallationCodeAsync(string code, CancellationToken ct = default)
        => db.Installations.FirstOrDefaultAsync(x => x.InstallationCode == code, ct);

    public async Task<IReadOnlyList<Installation>> GetAllAsync(CancellationToken ct = default)
        => await db.Installations.OrderBy(x => x.Name).ToListAsync(ct);

    public async Task UpdateRegistrationInfoAsync(Guid id, RegistrationInfo info, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return;

        installation.LastSeen = DateTime.UtcNow;
        installation.Status = info.Status;

        if (info.Name is not null)            installation.Name = info.Name;
        if (info.Location is not null)        installation.Location = info.Location;
        if (info.VersionServer is not null)   installation.InstalledVersionServer = info.VersionServer;
        if (info.VersionClient is not null)   installation.InstalledVersionClient = info.VersionClient;
        if (info.MachineName is not null)     installation.MachineName = info.MachineName;
        if (info.OSVersion is not null)       installation.OSVersion = info.OSVersion;
        if (info.DotNetVersion is not null)   installation.DotNetVersion = info.DotNetVersion;
        if (info.AgentVersion is not null)    installation.AgentVersion = info.AgentVersion;
        if (info.IpAddress is not null)       installation.IpAddress = info.IpAddress;
        if (info.Tags is not null)            installation.Tags = info.Tags;

        await db.SaveChangesAsync(ct);
    }

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

    public async Task<Guid?> CompleteUpdateHistoryAsync(Guid historyId, UpdateHistoryStatus status, string? errorMessage, bool rolledBack, CancellationToken ct = default)
    {
        var history = await db.UpdateHistories.FindAsync([historyId], ct);
        if (history is null) return null;
        history.Status = status;
        history.ErrorMessage = errorMessage;
        history.CompletedAt = DateTime.UtcNow;
        history.RolledBack = rolledBack;
        await db.SaveChangesAsync(ct);
        return history.PackageId;
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
        var newKey = Convert.ToHexStringLower(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        installation.ApiKey = newKey;
        installation.IsRevoked = false;
        installation.RevokedAt = null;
        installation.RevokedReason = null;
        await db.SaveChangesAsync(ct);
        return newKey;
    }

    public async Task SetUpdateModeAsync(Guid id, InstallationUpdateMode mode, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return;
        installation.UpdateMode = mode;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UpdateHistorySummary>> GetRecentHistoryAsync(
        Guid installationId, int max = 5, CancellationToken ct = default)
    {
        var rows = await db.UpdateHistories
            .Include(h => h.Package)
            .Where(h => h.InstallationId == installationId)
            .OrderByDescending(h => h.StartedAt)
            .Take(max)
            .ToListAsync(ct);

        return rows.Select(ToSummary).ToList();
    }

    public async Task<Dictionary<Guid, IReadOnlyList<UpdateHistorySummary>>> GetAllRecentHistoryAsync(
        IEnumerable<Guid> installationIds, int maxPerInstallation = 5, CancellationToken ct = default)
    {
        var ids = installationIds.ToHashSet();
        if (ids.Count == 0) return [];

        var rows = await db.UpdateHistories
            .Include(h => h.Package)
            .Where(h => ids.Contains(h.InstallationId))
            .OrderByDescending(h => h.StartedAt)
            .ToListAsync(ct);

        return rows
            .GroupBy(h => h.InstallationId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UpdateHistorySummary>)g
                    .Take(maxPerInstallation)
                    .Select(ToSummary)
                    .ToList());
    }

    private static UpdateHistorySummary ToSummary(UpdateHistory h) =>
        new(h.Id, h.PackageId, h.Package?.Version, h.Package?.Component,
            h.Status, h.PhaseDescription, h.StartedAt, h.CompletedAt);

    public async Task<IReadOnlyList<PendingInstallSummary>> GetPendingInstallsAsync(CancellationToken ct = default)
    {
        var rows = await db.UpdateHistories
            .Include(h => h.Package)
            .Include(h => h.Installation)
            .Where(h => h.Status == UpdateHistoryStatus.InProgress
                        && h.PhaseDescription == "AwaitingMaintenanceWindow")
            .OrderBy(h => h.StartedAt)
            .ToListAsync(ct);

        var onlineIds = connectionTracker.GetOnlineInstallationIds();

        return rows.Select(h => new PendingInstallSummary(
            InstallationId:  h.InstallationId,
            InstallationName: h.Installation?.Name ?? h.InstallationId.ToString(),
            IsConnected:     onlineIds.Contains(h.InstallationId),
            HistoryId:       h.Id,
            PackageId:       h.PackageId,
            Component:       h.Package?.Component.ToString(),
            Version:         h.Package?.Version,
            IsManualInstall: h.Package?.IsManualInstall ?? false,
            QueuedAt:        h.StartedAt)).ToList();
    }
}
