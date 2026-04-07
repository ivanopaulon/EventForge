namespace EventForge.DTOs.Alerts;

/// <summary>
/// Statistics for alert dashboard.
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int NewAlerts { get; set; }
    public int AcknowledgedAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int DismissedAlerts { get; set; }

    public int CriticalAlerts { get; set; }
    public int HighPriorityAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public int InfoAlerts { get; set; }

    public decimal TotalPotentialSavings { get; set; }
    public string Currency { get; set; } = "EUR";

    public int PriceIncreaseAlerts { get; set; }
    public int PriceDecreaseAlerts { get; set; }
    public int BetterSupplierAlerts { get; set; }
    public int VolatilityAlerts { get; set; }

    public DateTime? LastAlertDate { get; set; }
    public DateTime? OldestUnreadAlertDate { get; set; }
}
