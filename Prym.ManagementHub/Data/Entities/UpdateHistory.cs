using System.ComponentModel.DataAnnotations;

namespace Prym.ManagementHub.Data.Entities;

/// <summary>
/// Records every update attempt on an installation.
/// </summary>
public class UpdateHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstallationId { get; set; }
    public Installation? Installation { get; set; }

    public Guid PackageId { get; set; }
    public UpdatePackage? Package { get; set; }

    public UpdateHistoryStatus Status { get; set; } = UpdateHistoryStatus.Pending;

    [MaxLength(200)]
    public string? PhaseDescription { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(50)]
    public string? FromVersion { get; set; }

    [MaxLength(50)]
    public string? ToVersion { get; set; }

    public bool RolledBack { get; set; }
}

public enum UpdateHistoryStatus { Pending, InProgress, Succeeded, Failed, RolledBack }
