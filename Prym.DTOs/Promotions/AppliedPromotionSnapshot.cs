using Prym.DTOs.Common;

namespace Prym.DTOs.Promotions;

/// <summary>
/// Snapshot of a promotion applied to a document row, serialized as JSON in
/// <c>DocumentRow.AppliedPromotionsJSON</c> for traceability and reporting.
/// </summary>
public class AppliedPromotionSnapshot
{
    /// <summary>
    /// Unique identifier of the promotion that was applied.
    /// </summary>
    public Guid PromotionId { get; set; }

    /// <summary>
    /// Human-readable name of the promotion at the time it was applied.
    /// </summary>
    public string PromotionName { get; set; } = string.Empty;

    /// <summary>
    /// Absolute discount amount applied by this promotion (in the document currency).
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Percentage discount applied by this promotion.
    /// <c>null</c> when the promotion is not percentage-based (e.g. fixed-amount discounts).
    /// </summary>
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Type of the promotion rule that produced this discount.
    /// </summary>
    public PromotionRuleType PromotionType { get; set; }
}
