using Microsoft.EntityFrameworkCore;

namespace EventForge.UpdateHub.Services;

public class PackageService(UpdateHubDbContext db, IConfiguration configuration) : IPackageService
{
    private string PackageStorePath => configuration["UpdateHub:PackageStorePath"] ?? "packages";

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
}
