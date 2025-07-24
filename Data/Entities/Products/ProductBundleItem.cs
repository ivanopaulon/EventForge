using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Products;

/// <summary>
/// Represents a component of a product bundle.
/// </summary>
public class ProductBundleItem : AuditableEntity
{
    /// <summary>
    /// Parent bundle product.
    /// </summary>
    [Required(ErrorMessage = "The bundle product is required.")]
    [Display(Name = "Bundle Product", Description = "Parent bundle product.")]
    public Guid BundleProductId { get; set; }

    /// <summary>
    /// Navigation property for the parent bundle product.
    /// </summary>
    public Product? BundleProduct { get; set; }

    /// <summary>
    /// Component product (child).
    /// </summary>
    [Required(ErrorMessage = "The component product is required.")]
    [Display(Name = "Component Product", Description = "Component product (child).")]
    public Guid ComponentProductId { get; set; }

    /// <summary>
    /// Navigation property for the component product.
    /// </summary>
    public Product? ComponentProduct { get; set; }

    /// <summary>
    /// Quantity of the component in the bundle.
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
    [Display(Name = "Quantity", Description = "Quantity of the component in the bundle.")]
    public int Quantity { get; set; } = 1;
}