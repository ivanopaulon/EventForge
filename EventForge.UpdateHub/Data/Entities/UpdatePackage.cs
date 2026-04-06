using System.ComponentModel.DataAnnotations;

namespace EventForge.UpdateHub.Data.Entities;

/// <summary>
/// Represents an uploaded update package for server or client.
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

    public bool IsActive { get; set; } = true;

    public ICollection<UpdateHistory> UpdateHistory { get; set; } = [];
}

public enum PackageComponent { Server = 1, Client = 2 }
