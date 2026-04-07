namespace EventForge.DTOs.Promotions
{

    /// <summary>
    /// DTO for PromotionRule output/display operations
    /// </summary>
    public class PromotionRuleDto
    {
        /// <summary>
        /// Unique identifier for the promotion rule
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Foreign key to the parent promotion
        /// </summary>
        public Guid PromotionId { get; set; }

        /// <summary>
        /// Type of the rule (e.g., Discount, BuyXGetY, FixedPrice, Bundle, etc.)
        /// </summary>
        public string RuleType { get; set; } = string.Empty;

        /// <summary>
        /// Percentage discount (if applicable)
        /// </summary>
        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Fixed discount amount (if applicable)
        /// </summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// Quantity required to trigger the rule (e.g., for BuyXGetY)
        /// </summary>
        public int? RequiredQuantity { get; set; }

        /// <summary>
        /// Quantity given for free or at discount (e.g., for BuyXGetY)
        /// </summary>
        public int? FreeQuantity { get; set; }

        /// <summary>
        /// Fixed price for the bundle (if applicable)
        /// </summary>
        public decimal? FixedPrice { get; set; }

        /// <summary>
        /// Minimum order amount to activate this rule
        /// </summary>
        public decimal? MinOrderAmount { get; set; }

        /// <summary>
        /// Indicates if this rule can be combined with others
        /// </summary>
        public bool IsCombinable { get; set; }

        /// <summary>
        /// Products associated with this rule.
        /// </summary>
        public List<PromotionRuleProductDto> Products { get; set; } = new();

        /// <summary>
        /// Product category IDs this rule applies to.
        /// </summary>
        public List<Guid>? CategoryIds { get; set; }

        /// <summary>
        /// Business Party Group IDs this rule applies to.
        /// </summary>
        public List<Guid>? BusinessPartyGroupIds { get; set; }

        /// <summary>
        /// Sales channels this rule applies to.
        /// </summary>
        public List<string>? SalesChannels { get; set; }

        /// <summary>
        /// Days of week when the rule is valid.
        /// </summary>
        public List<DayOfWeek>? ValidDays { get; set; }

        /// <summary>
        /// Start time for time-limited rules.
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// End time for time-limited rules.
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Date and time when the promotion rule was created (UTC)
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the promotion rule
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the promotion rule was last modified (UTC)
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the promotion rule
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
