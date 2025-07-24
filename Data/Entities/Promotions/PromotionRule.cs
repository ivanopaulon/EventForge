using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Promotions;


/// <summary>
/// Represents a single rule or condition for a promotion.
/// </summary>
public class PromotionRule : AuditableEntity
{
    /// <summary>
    /// Foreign key to the parent promotion.
    /// </summary>
    [Required(ErrorMessage = "The promotion is required.")]
    [Display(Name = "Promotion", Description = "Reference to the parent promotion.")]
    public Guid PromotionId { get; set; }

    /// <summary>
    /// Navigation property for the parent promotion.
    /// </summary>
    public Promotion? Promotion { get; set; }

    /// <summary>
    /// Type of the rule (e.g., Discount, BuyXGetY, FixedPrice, Bundle, etc.).
    /// </summary>
    [Required(ErrorMessage = "The rule type is required.")]
    [Display(Name = "Rule Type", Description = "Type of the rule (Discount, BuyXGetY, FixedPrice, Bundle, etc.).")]
    public PromotionRuleType RuleType { get; set; }

    /// <summary>
    /// Percentage discount (if applicable).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
    [Display(Name = "Discount Percentage", Description = "Percentage discount (if applicable).")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>
    /// Fixed discount amount (if applicable).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative.")]
    [Display(Name = "Discount Amount", Description = "Fixed discount amount (if applicable).")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Quantity required to trigger the rule (e.g., for BuyXGetY).
    /// </summary>
    [Display(Name = "Required Quantity", Description = "Quantity required to trigger the rule (e.g., for BuyXGetY).")]
    public int? RequiredQuantity { get; set; }

    /// <summary>
    /// Quantity given for free or at discount (e.g., for BuyXGetY).
    /// </summary>
    [Display(Name = "Free Quantity", Description = "Quantity given for free or at discount (e.g., for BuyXGetY).")]
    public int? FreeQuantity { get; set; }

    /// <summary>
    /// Fixed price for the bundle (if applicable).
    /// </summary>
    [Display(Name = "Fixed Price", Description = "Fixed price for the bundle (if applicable).")]
    public decimal? FixedPrice { get; set; }

    /// <summary>
    /// Minimum order amount to activate this rule.
    /// </summary>
    [Display(Name = "Minimum Order Amount", Description = "Minimum order amount to activate this rule.")]
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// List of product categories this rule applies to.
    /// </summary>
    [Display(Name = "Category IDs", Description = "List of product categories this rule applies to.")]
    public List<Guid>? CategoryIds { get; set; }

    /// <summary>
    /// List of customer groups this rule applies to.
    /// </summary>
    [Display(Name = "Customer Group IDs", Description = "List of customer groups this rule applies to.")]
    public List<Guid>? CustomerGroupIds { get; set; }

    /// <summary>
    /// List of sales channels this rule applies to.
    /// </summary>
    [Display(Name = "Sales Channels", Description = "List of sales channels this rule applies to.")]
    public List<string>? SalesChannels { get; set; }

    /// <summary>
    /// Days of week when the rule is valid.
    /// </summary>
    [Display(Name = "Valid Days", Description = "Days of week when the rule is valid.")]
    public List<DayOfWeek>? ValidDays { get; set; }

    /// <summary>
    /// Start time (if time-limited).
    /// </summary>
    [Display(Name = "Start Time", Description = "Start time (if time-limited).")]
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// End time (if time-limited).
    /// </summary>
    [Display(Name = "End Time", Description = "End time (if time-limited).")]
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Indicates if this rule can be combined with others.
    /// </summary>
    [Display(Name = "Is Combinable", Description = "Indicates if this rule can be combined with others.")]
    public bool IsCombinable { get; set; } = true;

    /// <summary>
    /// List of products this rule applies to.
    /// </summary>
    [Display(Name = "Products", Description = "List of products this rule applies to.")]
    public List<PromotionRuleProduct> Products { get; set; } = new();
}