namespace Prym.Hub.Services;

/// <summary>Information auto-detected from a publish/deploy folder.</summary>
/// <param name="Component">Detected component type, or <see langword="null"/> if unrecognised.</param>
/// <param name="Version">Detected semantic version, or <see langword="null"/> if not found.</param>
/// <param name="GitCommit">Detected short git commit hash, or <see langword="null"/> if not found.</param>
/// <param name="DetectionError">Non-null when detection failed at the folder level (e.g. folder not found).</param>
public record PackageFolderInfo(
    PackageComponent? Component,
    string? Version,
    string? GitCommit,
    string? DetectionError = null);

/// <summary>Builds update packages directly from a local publish/deploy folder.</summary>
public interface IPackageBuildService
{
    /// <summary>
    /// Analyses <paramref name="folderPath"/> and returns automatically detected metadata
    /// (component, version, git commit) without making any changes.
    /// </summary>
    Task<PackageFolderInfo> DetectFromFolderAsync(string folderPath, CancellationToken ct = default);

    /// <summary>
    /// Creates a correctly structured ZIP package from the contents of <paramref name="folderPath"/>,
    /// computes its SHA-256 checksum and registers the package as
    /// <see cref="PackageStatus.ReadyToDeploy"/> in the database.
    /// </summary>
    Task<UpdatePackage> BuildFromFolderAsync(
        string folderPath,
        PackageComponent component,
        string version,
        string? releaseNotes,
        string? gitCommit,
        bool isManualInstall = false,
        CancellationToken ct = default);
}
