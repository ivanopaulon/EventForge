using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Warehouse;

/// <summary>
/// Represents stock level alerts for monitoring inventory levels.
/// </summary>
public class StockAlert : AuditableEntity
{
    /// <summary>
    /// Stock entry this alert is for.
    /// </summary>
    [Required(ErrorMessage = "Stock entry is required.")]
    [Display(Name = "Stock", Description = "Stock entry this alert is for.")]
    public Guid StockId { get; set; }

    /// <summary>
    /// Navigation property for the stock entry.
    /// </summary>
    public Stock? Stock { get; set; }

    /// <summary>
    /// Type of alert.
    /// </summary>
    [Required(ErrorMessage = "Alert type is required.")]
    [Display(Name = "Alert Type", Description = "Type of stock alert.")]
    public StockAlertType AlertType { get; set; }

    /// <summary>
    /// Severity level of the alert.
    /// </summary>
    [Display(Name = "Severity", Description = "Severity level of the alert.")]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>
    /// Current stock level when alert was triggered.
    /// </summary>
    [Display(Name = "Current Level", Description = "Current stock level when alert was triggered.")]
    public decimal CurrentLevel { get; set; }

    /// <summary>
    /// Threshold value that triggered the alert.
    /// </summary>
    [Display(Name = "Threshold", Description = "Threshold value that triggered the alert.")]
    public decimal Threshold { get; set; }

    /// <summary>
    /// Alert message.
    /// </summary>
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
    [Display(Name = "Message", Description = "Alert message.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Status of the alert.
    /// </summary>
    [Display(Name = "Status", Description = "Status of the alert.")]
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    /// <summary>
    /// Date when the alert was triggered.
    /// </summary>
    [Required(ErrorMessage = "Triggered date is required.")]
    [Display(Name = "Triggered Date", Description = "Date when the alert was triggered.")]
    public DateTime TriggeredDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the alert was acknowledged.
    /// </summary>
    [Display(Name = "Acknowledged Date", Description = "Date when the alert was acknowledged.")]
    public DateTime? AcknowledgedDate { get; set; }

    /// <summary>
    /// User who acknowledged the alert.
    /// </summary>
    [StringLength(100, ErrorMessage = "Acknowledged by cannot exceed 100 characters.")]
    [Display(Name = "Acknowledged By", Description = "User who acknowledged the alert.")]
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Date when the alert was resolved.
    /// </summary>
    [Display(Name = "Resolved Date", Description = "Date when the alert was resolved.")]
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// User who resolved the alert.
    /// </summary>
    [StringLength(100, ErrorMessage = "Resolved by cannot exceed 100 characters.")]
    [Display(Name = "Resolved By", Description = "User who resolved the alert.")]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes.
    /// </summary>
    [StringLength(500, ErrorMessage = "Resolution notes cannot exceed 500 characters.")]
    [Display(Name = "Resolution Notes", Description = "Notes about how the alert was resolved.")]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Whether this alert should trigger email notifications.
    /// </summary>
    [Display(Name = "Email Notifications", Description = "Whether this alert should trigger email notifications.")]
    public bool SendEmailNotifications { get; set; } = true;

    /// <summary>
    /// Email addresses to notify (comma-separated).
    /// </summary>
    [StringLength(500, ErrorMessage = "Notification emails cannot exceed 500 characters.")]
    [Display(Name = "Notification Emails", Description = "Email addresses to notify (comma-separated).")]
    public string? NotificationEmails { get; set; }

    /// <summary>
    /// Date when last notification was sent.
    /// </summary>
    [Display(Name = "Last Notification Date", Description = "Date when last notification was sent.")]
    public DateTime? LastNotificationDate { get; set; }

    /// <summary>
    /// Number of notifications sent for this alert.
    /// </summary>
    [Display(Name = "Notification Count", Description = "Number of notifications sent for this alert.")]
    public int NotificationCount { get; set; } = 0;
}

/// <summary>
/// Types of stock alerts.
/// </summary>
public enum StockAlertType
{
    LowStock,        // Stock level below minimum threshold
    HighStock,       // Stock level above maximum threshold
    Reorder,         // Stock level at reorder point
    Expiry,          // Product/lot approaching expiry
    Overstock,       // Excessive stock levels
    ZeroStock,       // Stock completely depleted
    NegativeStock,   // Stock levels gone negative (system error)
    QualityHold,     // Stock on quality hold
    Blocked,         // Stock blocked for other reasons
    SlowMoving,      // Slow-moving stock
    DeadStock,       // Dead/obsolete stock
    LocationFull,    // Storage location at capacity
    Custom           // Custom alert type
}

/// <summary>
/// Severity levels for alerts.
/// </summary>
public enum AlertSeverity
{
    Info,            // Informational
    Warning,         // Warning level
    Error,           // Error level
    Critical         // Critical level
}

/// <summary>
/// Status of alerts.
/// </summary>
public enum AlertStatus
{
    Active,          // Alert is active
    Acknowledged,    // Alert has been acknowledged
    Resolved,        // Alert has been resolved
    Dismissed,       // Alert has been dismissed
    Escalated        // Alert has been escalated
}