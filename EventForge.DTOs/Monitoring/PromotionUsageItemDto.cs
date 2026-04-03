namespace EventForge.DTOs.Monitoring;

/// <summary>
/// Represents a promotion's usage information for the monitoring dashboard.
/// </summary>
public class PromotionUsageItemDto
{
    /// <summary>
    /// Promotion identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Promotion name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of times the promotion has been used.
    /// </summary>
    public int CurrentUses { get; set; }

    /// <summary>
    /// Maximum allowed uses, or null if unlimited.
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Usage percentage relative to MaxUses (0–100), or null if unlimited.
    /// </summary>
    public double? UsagePercentage => MaxUses.HasValue && MaxUses.Value > 0
        ? Math.Round((double)CurrentUses / MaxUses.Value * 100, 1)
        : null;

    /// <summary>
    /// Whether the promotion is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Start date of the promotion.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the promotion.
    /// </summary>
    public DateTime EndDate { get; set; }
}
