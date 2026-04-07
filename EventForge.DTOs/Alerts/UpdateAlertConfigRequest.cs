using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Alerts;

/// <summary>
/// Request to update alert configuration.
/// </summary>
public class UpdateAlertConfigRequest
{
    [Required]
    [Range(0, 100)]
    public decimal PriceIncreaseThresholdPercentage { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal PriceDecreaseThresholdPercentage { get; set; }

    [Required]
    [Range(0, 100)]
    public decimal VolatilityThresholdPercentage { get; set; }

    [Required]
    [Range(1, 365)]
    public int DaysWithoutUpdateThreshold { get; set; }

    public bool EnableEmailNotifications { get; set; }
    public bool EnableBrowserNotifications { get; set; }

    public bool AlertOnPriceIncrease { get; set; }
    public bool AlertOnPriceDecrease { get; set; }
    public bool AlertOnBetterSupplier { get; set; }
    public bool AlertOnVolatility { get; set; }

    [Required]
    public string NotificationFrequency { get; set; } = "Immediate";
}
