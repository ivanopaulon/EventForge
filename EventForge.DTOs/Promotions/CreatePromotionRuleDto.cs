using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Promotions
{
    /// <summary>
    /// DTO for creating a new promotion rule.
    /// </summary>
    public class CreatePromotionRuleDto
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

        /// <summary>
        /// Product IDs this rule applies to. If empty, applies to all products (or filtered by CategoryIds).
        /// </summary>
        public List<Guid>? ProductIds { get; set; }

        /// <summary>
        /// Product category IDs this rule applies to.
        /// </summary>
        public List<Guid>? CategoryIds { get; set; }

        /// <summary>
        /// Business Party Group IDs this rule applies to. Leave empty for all customers.
        /// </summary>
        public List<Guid>? BusinessPartyGroupIds { get; set; }

        /// <summary>
        /// Sales channels this rule applies to. Leave empty for all channels.
        /// </summary>
        public List<string>? SalesChannels { get; set; }

        /// <summary>
        /// Days of week when the rule is valid. Leave empty for all days.
        /// </summary>
        public List<DayOfWeek>? ValidDays { get; set; }

        /// <summary>
        /// Start time for time-limited rules (UTC).
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// End time for time-limited rules (UTC).
        /// </summary>
        public TimeSpan? EndTime { get; set; }
    }
}
