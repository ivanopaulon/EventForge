using System.ComponentModel.DataAnnotations;

namespace Prym.ManagementHub.Data.Entities;

/// <summary>
/// Represents an update package discovered or uploaded for server or client.
/// </summary>
public class UpdatePackage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string Version { get; set; } = string.Empty;

    public PackageComponent Component { get; set; }

    [MaxLength(2000)]
    public string? ReleaseNotes { get; set; }

    /// <summary>SHA-256 hex checksum of the package zip file.</summary>
    [Required, MaxLength(64)]
    public string Checksum { get; set; } = string.Empty;

    /// <summary>Relative path under the PackageStorePath directory.</summary>
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? UploadedBy { get; set; }

    /// <summary>Lifecycle status of this package.</summary>
    public PackageStatus Status { get; set; } = PackageStatus.ReadyToDeploy;

    /// <summary>
    /// When <see langword="true"/>, agents will download the package but never install it
    /// automatically — they will always queue it and wait for operator approval,
    /// regardless of the installation's own <c>UpdateMode</c>.
    /// </summary>
    public bool IsManualInstall { get; set; }

    /// <summary>Short git commit SHA embedded in the package manifest.</summary>
    [MaxLength(40)]
    public string? GitCommit { get; set; }

    public ICollection<UpdateHistory> UpdateHistory { get; set; } = [];
}

public enum PackageComponent { Server = 1, Client = 2, Agent = 3 }

public enum PackageStatus
{
    /// <summary>Package ingested and ready to be pushed to agents.</summary>
    ReadyToDeploy = 1,

    /// <summary>Update is currently being deployed to one or more agents.</summary>
    Deploying = 2,

    /// <summary>Package has been deployed at least once successfully.</summary>
    Deployed = 3,

    /// <summary>Package is disabled / superseded.</summary>
    Archived = 4
}
