namespace EventForge.DTOs.Monitoring;

/// <summary>
/// Root DTO for the monitoring dashboard.
/// </summary>
public class MonitoringDashboardDto
{
    /// <summary>
    /// Timestamp when the dashboard snapshot was taken (UTC).
    /// </summary>
    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Performance metrics for pricing operations and query layer.
    /// </summary>
    public PricingMetricsDto PricingMetrics { get; set; } = new();

    /// <summary>
    /// Top promotions ordered by usage count.
    /// </summary>
    public List<PromotionUsageItemDto> TopPromotions { get; set; } = new();

    /// <summary>
    /// Promotions that are approaching their MaxUses limit (usage &gt; 80%).
    /// </summary>
    public List<PromotionUsageItemDto> NearLimitPromotions { get; set; } = new();

    /// <summary>
    /// Recent system error / warning log entries.
    /// </summary>
    public List<SystemHealthEntryDto> RecentErrors { get; set; } = new();
}
