using Microsoft.EntityFrameworkCore;

namespace Prym.ManagementHub.Services;

public class InstallationService(ManagementHubDbContext db, IConnectionTracker connectionTracker) : IInstallationService
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
        if (info.LocalIpAddress is not null)  installation.LocalIpAddress = info.LocalIpAddress;
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

    public async Task<UpdateHistory> StartUpdateHistoryAsync(Guid installationId, Guid packageId, string? fromVersionServer, string? fromVersionClient, string? fromVersionAgent = null, CancellationToken ct = default)
    {
        var pkg = await db.UpdatePackages.FindAsync([packageId], ct);
        var history = new UpdateHistory
        {
            InstallationId = installationId,
            PackageId = packageId,
            Status = UpdateHistoryStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            FromVersion = pkg?.Component switch
            {
                PackageComponent.Server => fromVersionServer,
                PackageComponent.Agent  => fromVersionAgent,
                _                       => fromVersionClient
            },
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

    public async Task<HistoryPackageInfo?> GetHistoryPackageInfoAsync(Guid historyId, CancellationToken ct = default)
    {
        var history = await db.UpdateHistories
            .AsNoTracking()
            .Include(h => h.Package)
            .FirstOrDefaultAsync(h => h.Id == historyId, ct);
        if (history is null) return null;
        return new HistoryPackageInfo(history.PackageId, history.Package?.Version, history.Package?.Component);
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
        IReadOnlyList<Guid> installationIds, int maxPerInstallation = 5, CancellationToken ct = default)
    {
        if (installationIds.Count == 0) return [];

        // Execute one parameterised query per installation so EF Core generates
        // "SELECT … WHERE InstallationId = @p0 ORDER BY StartedAt DESC LIMIT @p1",
        // which uses an index efficiently without a correlated subquery or full table scan.
        // Performance note: acceptable for typical hub deployments with up to ~1 000 installations.
        // At significantly larger scale, consider a single UNION or window-function query instead.
        var result = new Dictionary<Guid, IReadOnlyList<UpdateHistorySummary>>(installationIds.Count);

        foreach (var id in installationIds)
        {
            var rows = await db.UpdateHistories
                .Include(h => h.Package)
                .Where(h => h.InstallationId == id)
                .OrderByDescending(h => h.StartedAt)
                .Take(maxPerInstallation)
                .ToListAsync(ct);

            if (rows.Count > 0)
                result[id] = rows.Select(ToSummary).ToList();
        }

        return result;
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

    public async Task<IReadOnlyList<Guid>> GetStaleInstallationsAsync(DateTime cutoff, CancellationToken ct = default)
        => await db.Installations
            .Where(i => i.Status != InstallationStatus.Offline
                        && i.LastSeen.HasValue
                        && i.LastSeen.Value < cutoff)
            .Select(i => i.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Installation>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        if (idSet.Count == 0) return [];
        return await db.Installations
            .Where(i => idSet.Contains(i.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Installation>> FindByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
    {
        var lower = name.Trim().ToLowerInvariant();
        var query = db.Installations
            .Where(i => i.Name.ToLower() == lower);
        if (excludeId.HasValue)
            query = query.Where(i => i.Id != excludeId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<bool> MergeAsync(Guid targetId, Guid sourceId, CancellationToken ct = default)
    {
        if (targetId == sourceId) return false;

        var target = await db.Installations.FindAsync([targetId], ct);
        var source = await db.Installations.FindAsync([sourceId], ct);
        if (target is null || source is null) return false;

        // Copy non-null fields from source; prefer the most-recent LastSeen.
        if (source.LastSeen.HasValue && (!target.LastSeen.HasValue || source.LastSeen > target.LastSeen))
            target.LastSeen = source.LastSeen;
        if (source.InstalledVersionServer is not null && target.InstalledVersionServer is null)
            target.InstalledVersionServer = source.InstalledVersionServer;
        if (source.InstalledVersionClient is not null && target.InstalledVersionClient is null)
            target.InstalledVersionClient = source.InstalledVersionClient;
        if (source.MachineName is not null && target.MachineName is null)
            target.MachineName = source.MachineName;
        if (source.OSVersion is not null && target.OSVersion is null)
            target.OSVersion = source.OSVersion;
        if (source.DotNetVersion is not null && target.DotNetVersion is null)
            target.DotNetVersion = source.DotNetVersion;
        if (source.AgentVersion is not null && target.AgentVersion is null)
            target.AgentVersion = source.AgentVersion;
        if (source.IpAddress is not null && target.IpAddress is null)
            target.IpAddress = source.IpAddress;
        if (source.LocalIpAddress is not null && target.LocalIpAddress is null)
            target.LocalIpAddress = source.LocalIpAddress;
        if (source.Location is not null && target.Location is null)
            target.Location = source.Location;
        if (source.Tags is not null && target.Tags is null)
            target.Tags = source.Tags;
        if (source.Notes is not null && target.Notes is null)
            target.Notes = source.Notes;

        // Re-assign all UpdateHistory records from source → target in one query.
        await db.UpdateHistories
            .Where(h => h.InstallationId == sourceId)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.InstallationId, targetId), ct);

        db.Installations.Remove(source);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var installation = await db.Installations.FindAsync([id], ct);
        if (installation is null) return (false, "Installazione non trovata.");

        var hasActive = await db.UpdateHistories
            .AnyAsync(h => h.InstallationId == id && h.Status == UpdateHistoryStatus.InProgress, ct);
        if (hasActive)
            return (false, "Impossibile eliminare: ci sono aggiornamenti in corso per questa installazione.");

        // Cascade-delete all history records first.
        await db.UpdateHistories
            .Where(h => h.InstallationId == id)
            .ExecuteDeleteAsync(ct);

        db.Installations.Remove(installation);
        await db.SaveChangesAsync(ct);
        return (true, null);
    }
}
