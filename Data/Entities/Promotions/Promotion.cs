using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Promotions;


/// <summary>
/// Represents a promotion campaign.
/// </summary>
public class Promotion : AuditableEntity
{
    /// <summary>
    /// Name of the promotion.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [StringLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the promotion.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the promotion.
    /// </summary>
    [StringLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the promotion.")]
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the promotion.
    /// </summary>
    [Display(Name = "Start Date", Description = "Start date of the promotion.")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the promotion.
    /// </summary>
    [Display(Name = "End Date", Description = "End date of the promotion.")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Minimum order amount to activate the promotion.
    /// </summary>
    [Display(Name = "Minimum Order Amount", Description = "Minimum order amount to activate the promotion.")]
    public decimal? MinOrderAmount { get; set; }

    /// <summary>
    /// Maximum number of times this promotion can be used (global or per customer).
    /// </summary>
    [Display(Name = "Maximum Uses", Description = "Maximum number of times this promotion can be used (global or per customer).")]
    public int? MaxUses { get; set; }

    /// <summary>
    /// Coupon code required to activate the promotion.
    /// </summary>
    [StringLength(50, ErrorMessage = "The coupon code cannot exceed 50 characters.")]
    [Display(Name = "Coupon Code", Description = "Coupon code required to activate the promotion.")]
    public string? CouponCode { get; set; }

    /// <summary>
    /// Priority of the promotion (higher value = higher priority).
    /// </summary>
    [Display(Name = "Priority", Description = "Priority of the promotion (higher value = higher priority).")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Indicates if this promotion can be combined with others.
    /// </summary>
    [Display(Name = "Is Combinable", Description = "Indicates if this promotion can be combined with others.")]
    public bool IsCombinable { get; set; } = true;

    /// <summary>
    /// List of rules associated with this promotion.
    /// </summary>
    [Display(Name = "Rules", Description = "List of rules associated with this promotion.")]
    public List<PromotionRule> Rules { get; set; } = new();
}