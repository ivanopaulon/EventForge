using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Represents a backup operation.
/// </summary>
public class BackupOperation : AuditableEntity
{
    /// <summary>
    /// Current status of the backup operation.
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
    [Display(Name = "Status", Description = "Current status of the backup operation.")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Progress must be between 0 and 100.")]
    [Display(Name = "Progress Percentage", Description = "Progress percentage (0-100).")]
    public int ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// Current operation being performed.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Current operation cannot exceed 200 characters.")]
    [Display(Name = "Current Operation", Description = "Current operation being performed.")]
    public string? CurrentOperation { get; set; }

    /// <summary>
    /// Date and time when the backup was started (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Started At", Description = "Date and time when the backup was started.")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the backup was completed (UTC).
    /// </summary>
    [Display(Name = "Completed At", Description = "Date and time when the backup was completed.")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Path to the backup file.
    /// </summary>
    [MaxLength(500, ErrorMessage = "File path cannot exceed 500 characters.")]
    [Display(Name = "File Path", Description = "Path to the backup file.")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Size of the backup file in bytes.
    /// </summary>
    [Display(Name = "File Size Bytes", Description = "Size of the backup file in bytes.")]
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Error message if the backup failed.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Error message cannot exceed 1000 characters.")]
    [Display(Name = "Error Message", Description = "Error message if the backup failed.")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// User ID who started the backup.
    /// </summary>
    [Required]
    [Display(Name = "Started By User ID", Description = "User ID who started the backup.")]
    public Guid StartedByUserId { get; set; }

    /// <summary>
    /// Description provided when starting the backup.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description provided when starting the backup.")]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if audit logs were included in the backup.
    /// </summary>
    [Display(Name = "Include Audit Logs", Description = "Indicates if audit logs were included.")]
    public bool IncludeAuditLogs { get; set; } = true;

    /// <summary>
    /// Indicates if user data was included in the backup.
    /// </summary>
    [Display(Name = "Include User Data", Description = "Indicates if user data was included.")]
    public bool IncludeUserData { get; set; } = true;

    /// <summary>
    /// Indicates if configuration was included in the backup.
    /// </summary>
    [Display(Name = "Include Configuration", Description = "Indicates if configuration was included.")]
    public bool IncludeConfiguration { get; set; } = true;

    /// <summary>
    /// Navigation property: The user who started the backup.
    /// </summary>
    public virtual User StartedByUser { get; set; } = null!;
}