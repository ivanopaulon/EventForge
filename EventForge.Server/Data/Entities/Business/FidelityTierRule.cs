using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Business;

/// <summary>
/// Promotion/demotion rule for a <see cref="FidelityTier"/>. A card reaches the tier when the
/// customer's completed spend over the trailing <see cref="EvaluationPeriodMonths"/> window meets
/// <see cref="MinimumSpendThreshold"/>.
/// </summary>
public class FidelityTierRule : AuditableEntity
{
    [Required]
    public Guid TierId { get; set; }

    public FidelityTier? Tier { get; set; }

    /// <summary>
    /// Minimum trailing-window spend required to reach the tier. Null means no spend requirement
    /// (e.g. the base tier).
    /// </summary>
    public decimal? MinimumSpendThreshold { get; set; }

    /// <summary>
    /// Trailing window, in months, over which spend is accumulated for tier evaluation.
    /// </summary>
    public int EvaluationPeriodMonths { get; set; } = 12;
}
