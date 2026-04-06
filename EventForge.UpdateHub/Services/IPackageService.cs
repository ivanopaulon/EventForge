namespace EventForge.UpdateHub.Services;

public interface IPackageService
{
    Task<UpdatePackage?> GetLatestAsync(PackageComponent component, CancellationToken ct = default);
    Task<UpdatePackage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByChecksumAsync(string checksum, CancellationToken ct = default);
    Task<IReadOnlyList<UpdatePackage>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UpdatePackage>> GetByStatusAsync(PackageStatus status, CancellationToken ct = default);
    Task<UpdatePackage> CreateAsync(UpdatePackage package, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, PackageStatus status, CancellationToken ct = default);
    Task<string> GetDownloadPathAsync(Guid packageId, CancellationToken ct = default);
}
