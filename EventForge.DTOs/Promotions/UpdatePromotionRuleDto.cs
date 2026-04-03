using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Promotions
{
    /// <summary>
    /// DTO for updating an existing promotion rule.
    /// </summary>
    public class UpdatePromotionRuleDto
    {
        /// <summary>
        /// Type of the rule (e.g., Discount, BuyXGetY, FixedPrice, Bundle, etc.)
        /// </summary>
        [Required]
        public string RuleType { get; set; } = string.Empty;

        /// <summary>
        /// Percentage discount (if applicable).
        /// </summary>
        [Range(0, 100)]
        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Fixed discount amount (if applicable).
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// Quantity required to trigger the rule (e.g., for BuyXGetY).
        /// </summary>
        public int? RequiredQuantity { get; set; }

        /// <summary>
        /// Quantity given for free or at discount (e.g., for BuyXGetY).
        /// </summary>
        public int? FreeQuantity { get; set; }

        /// <summary>
        /// Fixed price for the bundle (if applicable).
        /// </summary>
        public decimal? FixedPrice { get; set; }

        /// <summary>
        /// Minimum order amount to activate this rule.
        /// </summary>
        public decimal? MinOrderAmount { get; set; }

        /// <summary>
        /// Indicates if this rule can be combined with others.
        /// </summary>
        public bool IsCombinable { get; set; } = true;
    }
}
