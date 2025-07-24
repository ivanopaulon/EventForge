using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Promotions;


/// <summary>
/// Associates a product with a promotion rule.
/// </summary>
public class PromotionRuleProduct : AuditableEntity
{
    /// <summary>
    /// Foreign key to the associated promotion rule.
    /// </summary>
    [Required(ErrorMessage = "The promotion rule is required.")]
    [Display(Name = "Promotion Rule", Description = "Identifier of the associated promotion rule.")]
    public Guid PromotionRuleId { get; set; }

    /// <summary>
    /// Navigation property for the associated promotion rule.
    /// </summary>
    public PromotionRule? PromotionRule { get; set; }

    /// <summary>
    /// Foreign key to the associated product.
    /// </summary>
    [Required(ErrorMessage = "The product is required.")]
    [Display(Name = "Product", Description = "Identifier of the associated product.")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Navigation property for the associated product.
    /// </summary>
    public Product? Product { get; set; }
}