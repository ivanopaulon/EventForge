using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Represents a system alert or notification.
/// </summary>
public class SystemAlert : AuditableEntity
{
    /// <summary>
    /// Severity of the alert.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Severity", Description = "Alert severity level.")]
    public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

    /// <summary>
    /// Title of the alert.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Display(Name = "Title", Description = "Alert title.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed message of the alert.
    /// </summary>
    [Required]
    [Display(Name = "Message", Description = "Alert message.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the alert was created.
    /// </summary>
    [Required]
    [Display(Name = "Created At", Description = "Alert creation time.")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the alert was resolved.
    /// </summary>
    [Display(Name = "Resolved At", Description = "Alert resolution time.")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Username who resolved the alert.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Resolved By", Description = "User who resolved the alert.")]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Indicates if the alert is still active.
    /// </summary>
    [Display(Name = "Is Active", Description = "Whether the alert is active.")]
    public bool IsActive { get; set; } = true;
}
