namespace Prym.ManagementHub.Services;

/// <summary>Data access and business logic for managing <see cref="UpdatePackage"/> records.</summary>
public interface IPackageService
{
    /// <summary>Returns the most recently uploaded <c>ReadyToDeploy</c> package for <paramref name="component"/>, or <see langword="null"/>.</summary>
    Task<UpdatePackage?> GetLatestAsync(PackageComponent component, CancellationToken ct = default);

    /// <summary>Returns the package with the given <paramref name="id"/>, or <see langword="null"/>.</summary>
    Task<UpdatePackage?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if any package with the given SHA-256 <paramref name="checksum"/> already exists in the database.</summary>
    Task<bool> ExistsByChecksumAsync(string checksum, CancellationToken ct = default);

    /// <summary>Returns all packages ordered by upload date descending.</summary>
    Task<IReadOnlyList<UpdatePackage>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns all packages whose <see cref="UpdatePackage.Status"/> matches <paramref name="status"/>, ordered by upload date descending.</summary>
    Task<IReadOnlyList<UpdatePackage>> GetByStatusAsync(PackageStatus status, CancellationToken ct = default);

    /// <summary>Persists a new <see cref="UpdatePackage"/> record and returns the saved entity.</summary>
    Task<UpdatePackage> CreateAsync(UpdatePackage package, CancellationToken ct = default);

    /// <summary>Updates the <see cref="UpdatePackage.Status"/> of the package with the given <paramref name="id"/>.</summary>
    Task SetStatusAsync(Guid id, PackageStatus status, CancellationToken ct = default);

    /// <summary>Returns the absolute file path for the given package's zip file.</summary>
    Task<string> GetDownloadPathAsync(Guid packageId, CancellationToken ct = default);

    /// <summary>
    /// Returns a suggested next version string for <paramref name="component"/> by finding the most
    /// recently uploaded package (regardless of status), parsing its version and incrementing either
    /// the major or minor part based on <paramref name="versionType"/> ("major" | "minor").
    /// Returns a sensible default when no previous package exists.
    /// </summary>
    Task<string> GetSuggestedNextVersionAsync(PackageComponent component, string versionType, CancellationToken ct = default);

    /// <summary>Returns the total count of all packages without loading entity data.</summary>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns packages with <see cref="PackageStatus.Archived"/> or <see cref="PackageStatus.Deployed"/>
    /// whose <see cref="UpdatePackage.UploadedAt"/> is older than <paramref name="cutoff"/>.
    /// Used by <see cref="PackageCleanupService"/>.
    /// </summary>
    Task<IReadOnlyList<UpdatePackage>> GetExpiredPackagesAsync(DateTime cutoff, CancellationToken ct = default);

    /// <summary>Permanently removes the package record from the database.</summary>
    Task DeleteAsync(Guid packageId, CancellationToken ct = default);
}
