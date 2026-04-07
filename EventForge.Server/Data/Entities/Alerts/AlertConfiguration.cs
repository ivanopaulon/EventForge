using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Alerts;

/// <summary>
/// User-specific configuration for alert thresholds and preferences.
/// Part of FASE 5: Price Alerts System.
/// </summary>
public class AlertConfiguration : AuditableEntity
{
    /// <summary>
    /// User identifier for whom this configuration applies.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Display(Name = "User ID", Description = "User identifier for this configuration.")]
    public string UserId { get; set; } = string.Empty;

    // Thresholds

    /// <summary>
    /// Percentage threshold for price increase alerts.
    /// </summary>
    [Required]
    [Display(Name = "Price Increase Threshold %", Description = "Percentage threshold for price increase alerts.")]
    public decimal PriceIncreaseThresholdPercentage { get; set; } = 5.0m;

    /// <summary>
    /// Percentage threshold for price decrease alerts.
    /// </summary>
    [Required]
    [Display(Name = "Price Decrease Threshold %", Description = "Percentage threshold for price decrease alerts.")]
    public decimal PriceDecreaseThresholdPercentage { get; set; } = 10.0m;

    /// <summary>
    /// Percentage threshold for volatility alerts.
    /// </summary>
    [Required]
    [Display(Name = "Volatility Threshold %", Description = "Percentage threshold for volatility alerts.")]
    public decimal VolatilityThresholdPercentage { get; set; } = 15.0m;

    /// <summary>
    /// Days without update before triggering an alert.
    /// </summary>
    [Required]
    [Display(Name = "Days Without Update Threshold", Description = "Days without update threshold.")]
    public int DaysWithoutUpdateThreshold { get; set; } = 90;

    // Preferences

    /// <summary>
    /// Enable email notifications.
    /// </summary>
    [Display(Name = "Enable Email Notifications", Description = "Enable email notifications.")]
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// Enable browser notifications.
    /// </summary>
    [Display(Name = "Enable Browser Notifications", Description = "Enable browser notifications.")]
    public bool EnableBrowserNotifications { get; set; } = true;

    // Filters

    /// <summary>
    /// Enable alerts for price increases.
    /// </summary>
    [Display(Name = "Alert On Price Increase", Description = "Enable alerts for price increases.")]
    public bool AlertOnPriceIncrease { get; set; } = true;

    /// <summary>
    /// Enable alerts for price decreases.
    /// </summary>
    [Display(Name = "Alert On Price Decrease", Description = "Enable alerts for price decreases.")]
    public bool AlertOnPriceDecrease { get; set; } = true;

    /// <summary>
    /// Enable alerts for better supplier availability.
    /// </summary>
    [Display(Name = "Alert On Better Supplier", Description = "Enable alerts for better suppliers.")]
    public bool AlertOnBetterSupplier { get; set; } = true;

    /// <summary>
    /// Enable alerts for price volatility.
    /// </summary>
    [Display(Name = "Alert On Volatility", Description = "Enable alerts for volatility.")]
    public bool AlertOnVolatility { get; set; } = true;

    /// <summary>
    /// Notification frequency preference.
    /// </summary>
    [Required]
    [Display(Name = "Notification Frequency", Description = "How often to send notifications.")]
    public AlertFrequency NotificationFrequency { get; set; } = AlertFrequency.Immediate;

    /// <summary>
    /// Date and time when the last digest was sent.
    /// </summary>
    [Display(Name = "Last Digest Sent At", Description = "Date and time of last digest.")]
    public DateTime? LastDigestSentAt { get; set; }
}

/// <summary>
/// Notification frequency options.
/// </summary>
public enum AlertFrequency
{
    /// <summary>
    /// Send notifications immediately.
    /// </summary>
    Immediate,

    /// <summary>
    /// Send hourly digest.
    /// </summary>
    Hourly,

    /// <summary>
    /// Send daily digest.
    /// </summary>
    Daily,

    /// <summary>
    /// Send weekly digest.
    /// </summary>
    Weekly
}
