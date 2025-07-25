using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Auth;

/// <summary>
/// Audit log for user login attempts and activities.
/// </summary>
public class LoginAudit : AuditableEntity
{
    /// <summary>
    /// Foreign key to User (nullable for failed login attempts with invalid usernames).
    /// </summary>
    [Display(Name = "User ID", Description = "Foreign key to User.")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Username attempted during login (for tracking failed attempts).
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "Username", Description = "Username attempted during login.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Type of login event (Success, Failed, Logout, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Display(Name = "Event Type", Description = "Type of login event.")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// IP address from which the login was attempted.
    /// </summary>
    [MaxLength(45)] // IPv6 support
    [Display(Name = "IP Address", Description = "IP address from which the login was attempted.")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the browser/client.
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "User Agent", Description = "User agent string from the browser/client.")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Date and time of the login event (UTC).
    /// </summary>
    [Required]
    [Display(Name = "Event Time", Description = "Date and time of the login event (UTC).")]
    public DateTime EventTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the login attempt was successful.
    /// </summary>
    [Display(Name = "Success", Description = "Indicates if the login attempt was successful.")]
    public bool Success { get; set; } = false;

    /// <summary>
    /// Reason for failed login (if applicable).
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "Failure Reason", Description = "Reason for failed login (if applicable).")]
    public string? FailureReason { get; set; }

    /// <summary>
    /// Session ID for tracking user sessions.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Session ID", Description = "Session ID for tracking user sessions.")]
    public string? SessionId { get; set; }

    /// <summary>
    /// Duration of the session (for logout events).
    /// </summary>
    [Display(Name = "Session Duration", Description = "Duration of the session (for logout events).")]
    public TimeSpan? SessionDuration { get; set; }

    /// <summary>
    /// Additional metadata about the login event (JSON).
    /// </summary>
    [Display(Name = "Metadata", Description = "Additional metadata about the login event (JSON).")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property: The user (if login was for a valid user).
    /// </summary>
    public virtual User? User { get; set; }
}