using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Audit;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Data.Entities.Alerts;

/// <summary>
/// Represents an alert for supplier price changes and recommendations.
/// Part of FASE 5: Price Alerts System.
/// </summary>
public class SupplierPriceAlert : AuditableEntity
{
    /// <summary>
    /// Product identifier (optional - can be null for supplier-level alerts).
    /// </summary>
    [Display(Name = "Product", Description = "Product identifier for product-specific alerts.")]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Supplier identifier (optional - can be null for product-level alerts).
    /// </summary>
    [Display(Name = "Supplier", Description = "Supplier identifier for supplier-specific alerts.")]
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// Type of alert.
    /// </summary>
    [Required]
    [Display(Name = "Alert Type", Description = "Type of alert generated.")]
    public AlertType AlertType { get; set; }

    /// <summary>
    /// Severity level of the alert.
    /// </summary>
    [Required]
    [Display(Name = "Severity", Description = "Severity level of the alert.")]
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Current status of the alert.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the alert.")]
    public AlertStatus Status { get; set; } = AlertStatus.New;

    /// <summary>
    /// Previous price before the change.
    /// </summary>
    [Display(Name = "Old Price", Description = "Previous price before the change.")]
    public decimal? OldPrice { get; set; }

    /// <summary>
    /// New price after the change.
    /// </summary>
    [Display(Name = "New Price", Description = "New price after the change.")]
    public decimal? NewPrice { get; set; }

    /// <summary>
    /// Percentage of price change.
    /// </summary>
    [Display(Name = "Price Change %", Description = "Percentage of price change.")]
    public decimal? PriceChangePercentage { get; set; }

    /// <summary>
    /// Currency code for the prices.
    /// </summary>
    [MaxLength(10)]
    [Display(Name = "Currency", Description = "Currency code for prices.")]
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Potential savings amount if action is taken.
    /// </summary>
    [Display(Name = "Potential Savings", Description = "Potential savings amount.")]
    public decimal? PotentialSavings { get; set; }

    /// <summary>
    /// Alert title for display.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Display(Name = "Alert Title", Description = "Alert title for display.")]
    public string AlertTitle { get; set; } = string.Empty;

    /// <summary>
    /// Detailed alert message.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    [Display(Name = "Alert Message", Description = "Detailed alert message.")]
    public string AlertMessage { get; set; } = string.Empty;

    /// <summary>
    /// Recommended action to take.
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "Recommended Action", Description = "Recommended action to take.")]
    public string? RecommendedAction { get; set; }

    /// <summary>
    /// Reference to better supplier suggestion if applicable.
    /// </summary>
    [Display(Name = "Better Supplier Suggestion", Description = "Reference to better supplier.")]
    public Guid? BetterSupplierSuggestionId { get; set; }

    /// <summary>
    /// Date and time when the alert was acknowledged.
    /// </summary>
    [Display(Name = "Acknowledged At", Description = "Date and time of acknowledgment.")]
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// User who acknowledged the alert.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Acknowledged By", Description = "User who acknowledged the alert.")]
    public string? AcknowledgedByUserId { get; set; }

    /// <summary>
    /// Date and time when the alert was resolved.
    /// </summary>
    [Display(Name = "Resolved At", Description = "Date and time of resolution.")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User who resolved the alert.
    /// </summary>
    [MaxLength(100)]
    [Display(Name = "Resolved By", Description = "User who resolved the alert.")]
    public string? ResolvedByUserId { get; set; }

    /// <summary>
    /// Notes on how the alert was resolved.
    /// </summary>
    [MaxLength(1000)]
    [Display(Name = "Resolution Notes", Description = "Notes on alert resolution.")]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Indicates whether an email notification was sent.
    /// </summary>
    [Display(Name = "Email Sent", Description = "Whether email notification was sent.")]
    public bool EmailSent { get; set; } = false;

    /// <summary>
    /// Date and time when the email was sent.
    /// </summary>
    [Display(Name = "Email Sent At", Description = "Date and time email was sent.")]
    public DateTime? EmailSentAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Product navigation property.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Supplier navigation property.
    /// </summary>
    public BusinessParty? Supplier { get; set; }
}

/// <summary>
/// Types of alerts that can be generated.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Price has increased beyond threshold.
    /// </summary>
    PriceIncrease,

    /// <summary>
    /// Price has decreased beyond threshold.
    /// </summary>
    PriceDecrease,

    /// <summary>
    /// A better supplier option is available.
    /// </summary>
    BetterSupplierAvailable,

    /// <summary>
    /// Price is showing high volatility.
    /// </summary>
    PriceVolatility,

    /// <summary>
    /// Current supplier is no longer competitive.
    /// </summary>
    SupplierNonCompetitive,

    /// <summary>
    /// Lead time has increased significantly.
    /// </summary>
    LeadTimeIncrease,

    /// <summary>
    /// No price update in extended period.
    /// </summary>
    NoRecentUpdate
}

/// <summary>
/// Severity levels for alerts.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert.
    /// </summary>
    Info,

    /// <summary>
    /// Warning level alert.
    /// </summary>
    Warning,

    /// <summary>
    /// High priority alert.
    /// </summary>
    High,

    /// <summary>
    /// Critical alert requiring immediate attention.
    /// </summary>
    Critical
}

/// <summary>
/// Status of an alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// New, unread alert.
    /// </summary>
    New,

    /// <summary>
    /// Alert has been acknowledged by a user.
    /// </summary>
    Acknowledged,

    /// <summary>
    /// Alert has been resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// Alert has been dismissed without action.
    /// </summary>
    Dismissed
}
