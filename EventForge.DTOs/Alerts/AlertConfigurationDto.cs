namespace EventForge.DTOs.Alerts;

/// <summary>
/// DTO for alert configuration.
/// </summary>
public class AlertConfigurationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;

    // Thresholds
    public decimal PriceIncreaseThresholdPercentage { get; set; }
    public decimal PriceDecreaseThresholdPercentage { get; set; }
    public decimal VolatilityThresholdPercentage { get; set; }
    public int DaysWithoutUpdateThreshold { get; set; }

    // Preferences
    public bool EnableEmailNotifications { get; set; }
    public bool EnableBrowserNotifications { get; set; }

    // Filters
    public bool AlertOnPriceIncrease { get; set; }
    public bool AlertOnPriceDecrease { get; set; }
    public bool AlertOnBetterSupplier { get; set; }
    public bool AlertOnVolatility { get; set; }

    public string NotificationFrequency { get; set; } = "Immediate";
    public DateTime? LastDigestSentAt { get; set; }
}
