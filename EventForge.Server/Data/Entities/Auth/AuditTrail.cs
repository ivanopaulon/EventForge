using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Auth;

/// <summary>
/// Represents audit trail entries for tenant switching and user impersonation operations.
/// </summary>
public class AuditTrail : AuditableEntity
{
    /// <summary>
    /// User ID who performed the action (super admin).
    /// </summary>
    [Required]
    [Display(Name = "Performed By User ID", Description = "User ID who performed the action.")]
    public Guid PerformedByUserId { get; set; }

    /// <summary>
    /// Type of audit operation.
    /// </summary>
    [Required]
    [Display(Name = "Operation Type", Description = "Type of audit operation.")]
    public AuditOperationType OperationType { get; set; }

    /// <summary>
    /// Source tenant ID (where the operation started).
    /// </summary>
    [Display(Name = "Source Tenant ID", Description = "Source tenant ID where the operation started.")]
    public Guid? SourceTenantId { get; set; }

    /// <summary>
    /// Target tenant ID (where the operation is directed).
    /// </summary>
    [Display(Name = "Target Tenant ID", Description = "Target tenant ID where the operation is directed.")]
    public Guid? TargetTenantId { get; set; }

    /// <summary>
    /// Target user ID (for impersonation operations).
    /// </summary>
    [Display(Name = "Target User ID", Description = "Target user ID for impersonation operations.")]
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Session ID for tracking related operations.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Session ID cannot exceed 100 characters.")]
    [Display(Name = "Session ID", Description = "Session ID for tracking related operations.")]
    public string? SessionId { get; set; }

    /// <summary>
    /// IP address from which the operation was performed.
    /// </summary>
    [MaxLength(45, ErrorMessage = "IP address cannot exceed 45 characters.")]
    [Display(Name = "IP Address", Description = "IP address from which the operation was performed.")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client that performed the operation.
    /// </summary>
    [MaxLength(500, ErrorMessage = "User agent cannot exceed 500 characters.")]
    [Display(Name = "User Agent", Description = "User agent of the client that performed the operation.")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional details about the operation (JSON format).
    /// </summary>
    [Display(Name = "Details", Description = "Additional details about the operation in JSON format.")]
    public string? Details { get; set; }

    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    [Display(Name = "Was Successful", Description = "Indicates if the operation was successful.")]
    public bool WasSuccessful { get; set; } = true;

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Error message cannot exceed 1000 characters.")]
    [Display(Name = "Error Message", Description = "Error message if the operation failed.")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Date and time when the operation was performed (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Performed At", Description = "Date and time when the operation was performed (UTC).")]
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: The user who performed the action.
    /// </summary>
    public virtual User PerformedByUser { get; set; } = null!;

    /// <summary>
    /// Navigation property: The source tenant (if applicable).
    /// </summary>
    public virtual Tenant? SourceTenant { get; set; }

    /// <summary>
    /// Navigation property: The target tenant (if applicable).
    /// </summary>
    public virtual Tenant? TargetTenant { get; set; }

    /// <summary>
    /// Navigation property: The target user (for impersonation).
    /// </summary>
    public virtual User? TargetUser { get; set; }
}

/// <summary>
/// Types of audit operations for tenant switching and impersonation.
/// </summary>
public enum AuditOperationType
{
    /// <summary>
    /// Super admin switched to a different tenant context.
    /// </summary>
    TenantSwitch = 0,

    /// <summary>
    /// Super admin started impersonating a user.
    /// </summary>
    ImpersonationStart = 1,

    /// <summary>
    /// Super admin ended impersonation and returned to original session.
    /// </summary>
    ImpersonationEnd = 2,

    /// <summary>
    /// Admin tenant access was granted to a user.
    /// </summary>
    AdminTenantGranted = 3,

    /// <summary>
    /// Admin tenant access was revoked from a user.
    /// </summary>
    AdminTenantRevoked = 4,

    /// <summary>
    /// Tenant was disabled/enabled.
    /// </summary>
    TenantStatusChanged = 5
}