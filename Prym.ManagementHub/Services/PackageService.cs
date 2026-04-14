using Microsoft.EntityFrameworkCore;

namespace Prym.ManagementHub.Services;

public class PackageService(ManagementHubDbContext db, ManagementHubOptions hubOptions) : IPackageService
{
    private string PackageStorePath => hubOptions.PackageStorePath;

    public Task<UpdatePackage?> GetLatestAsync(PackageComponent component, CancellationToken ct = default)
        => db.UpdatePackages
            .Where(x => x.Component == component && x.Status == PackageStatus.ReadyToDeploy)
            .OrderByDescending(x => x.UploadedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<UpdatePackage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.UpdatePackages.FindAsync([id], ct);

    public Task<bool> ExistsByChecksumAsync(string checksum, CancellationToken ct = default)
        => db.UpdatePackages.AnyAsync(x => x.Checksum == checksum, ct);

    public async Task<IReadOnlyList<UpdatePackage>> GetAllAsync(CancellationToken ct = default)
        => await db.UpdatePackages.OrderByDescending(x => x.UploadedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<UpdatePackage>> GetByStatusAsync(PackageStatus status, CancellationToken ct = default)
        => await db.UpdatePackages
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(ct);

    public async Task<UpdatePackage> CreateAsync(UpdatePackage package, CancellationToken ct = default)
    {
        db.UpdatePackages.Add(package);
        await db.SaveChangesAsync(ct);
        return package;
    }

    public async Task SetStatusAsync(Guid id, PackageStatus status, CancellationToken ct = default)
    {
        await db.UpdatePackages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status), ct);
    }

    public async Task<string> GetDownloadPathAsync(Guid packageId, CancellationToken ct = default)
    {
        var pkg = await db.UpdatePackages.FindAsync([packageId], ct);
        if (pkg is null) throw new FileNotFoundException($"Package {packageId} not found.");
        return Path.Combine(PackageStorePath, pkg.FilePath);
    }

    public async Task<string> GetSuggestedNextVersionAsync(
        PackageComponent component, string versionType, CancellationToken ct = default)
    {
        // Use the most recently uploaded package for the component regardless of status,
        // so the suggestion always continues from the last used version.
        var latest = await db.UpdatePackages
            .Where(x => x.Component == component)
            .OrderByDescending(x => x.UploadedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is null)
            return versionType.Equals("major", StringComparison.OrdinalIgnoreCase) ? "2.0.0" : "1.1.0";

        // Strip NBGV build metadata (e.g. "1.2.3+gabcdef1" → "1.2.3").
        var raw = latest.Version;
        var plusIdx = raw.IndexOf('+');
        if (plusIdx >= 0) raw = raw[..plusIdx];

        if (Version.TryParse(raw, out var parsed))
        {
            var major = parsed.Major < 0 ? 1 : parsed.Major;
            var minor = parsed.Minor < 0 ? 0 : parsed.Minor;

            return versionType.Equals("major", StringComparison.OrdinalIgnoreCase)
                ? $"{major + 1}.0.0"
                : $"{major}.{minor + 1}.0";
        }

        // Fallback: cannot parse — return unchanged with a "-next" suffix.
        return raw + "-next";
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => db.UpdatePackages.CountAsync(ct);

    public async Task<IReadOnlyList<UpdatePackage>> GetExpiredPackagesAsync(DateTime cutoff, CancellationToken ct = default)
        => await db.UpdatePackages
            .Where(p => (p.Status == PackageStatus.Archived || p.Status == PackageStatus.Deployed)
                        && p.UploadedAt < cutoff)
            .ToListAsync(ct);

    public async Task DeleteAsync(Guid packageId, CancellationToken ct = default)
    {
        await db.UpdatePackages
            .Where(p => p.Id == packageId)
            .ExecuteDeleteAsync(ct);
    }
}
