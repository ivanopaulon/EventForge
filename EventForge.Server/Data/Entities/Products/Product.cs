using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Products;


/// <summary>
/// Represents a product in the inventory.
/// </summary>
public class Product : AuditableEntity
{
    /// <summary>
    /// Product name.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Product name.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short product description.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The short description cannot exceed 50 characters.")]
    [Display(Name = "Short Description", Description = "Short product description.")]
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed product description.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Detailed product description.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Product code (SKU or similar).
    /// </summary>
    [Display(Name = "Code", Description = "Product code (SKU or similar).")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Product image URL.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The image URL cannot exceed 500 characters.")]
    [Display(Name = "Image", Description = "Product image URL.")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Product status.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the product.")]
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    /// <summary>
    /// Indicates if the price includes VAT.
    /// </summary>
    [Display(Name = "VAT Included", Description = "Indicates if the price includes VAT.")]
    public bool IsVatIncluded { get; set; } = false;

    /// <summary>
    /// Default product price.
    /// </summary>
    [Display(Name = "Default Price", Description = "Default product price.")]
    public decimal? DefaultPrice { get; set; }

    /// <summary>
    /// VAT rate identifier.
    /// </summary>
    [Display(Name = "VAT Rate", Description = "Identifier of the VAT rate.")]
    public Guid? VatRateId { get; set; }

    /// <summary>
    /// VAT rate used for the product.
    /// </summary>
    public VatRate? VatRate { get; set; }

    /// <summary>
    /// Unit of measure identifier.
    /// </summary>
    [Display(Name = "Unit of Measure", Description = "Identifier of the unit of measure.")]
    public Guid? UnitOfMeasureId { get; set; }

    /// <summary>
    /// Unit of measure for the product.
    /// </summary>
    public UM? UnitOfMeasure { get; set; }

    /// <summary>
    /// Units of measure associated with the product.
    /// </summary>
    [Display(Name = "Units", Description = "Units of measure associated with the product.")]
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();

    /// <summary>
    /// Main category node identifier.
    /// </summary>
    [Display(Name = "Category", Description = "Identifier of the main category.")]
    public Guid? CategoryNodeId { get; set; }

    /// <summary>
    /// Main category node of the product.
    /// </summary>
    public ClassificationNode? CategoryNode { get; set; }

    /// <summary>
    /// Family node identifier.
    /// </summary>
    [Display(Name = "Family", Description = "Identifier of the family.")]
    public Guid? FamilyNodeId { get; set; }

    /// <summary>
    /// Family node of the product.
    /// </summary>
    public ClassificationNode? FamilyNode { get; set; }

    /// <summary>
    /// Statistical group node identifier.
    /// </summary>
    [Display(Name = "Statistical Group", Description = "Identifier of the statistical group.")]
    public Guid? GroupNodeId { get; set; }

    /// <summary>
    /// Statistical group node of the product.
    /// </summary>
    public ClassificationNode? GroupNode { get; set; }

    /// <summary>
    /// List of alias codes for the product.
    /// </summary>
    [Display(Name = "Product Codes", Description = "List of alias codes for the product.")]
    public ICollection<ProductCode> Codes { get; set; } = new List<ProductCode>();

    /// <summary>
    /// Station identifier.
    /// </summary>
    [Display(Name = "Station", Description = "Identifier of the station.")]
    public Guid? StationId { get; set; }

    /// <summary>
    /// Station associated with the product.
    /// </summary>
    public Station? Station { get; set; }

    /// <summary>
    /// Components if the product is a bundle.
    /// </summary>
    [Display(Name = "Bundle Items", Description = "Components if the product is a bundle.")]
    public ICollection<ProductBundleItem> BundleItems { get; set; } = new List<ProductBundleItem>();

    /// <summary>
    /// Bundles in which this product is a component.
    /// </summary>
    [Display(Name = "Included In Bundles", Description = "Bundles in which this product is a component.")]
    public ICollection<ProductBundleItem> IncludedInBundles { get; set; } = new List<ProductBundleItem>();

    /// <summary>
    /// Indicates if the product is a bundle (composed of other products).
    /// </summary>
    [Display(Name = "Is Bundle", Description = "Indicates if the product is a bundle.")]
    public bool IsBundle { get; set; } = false;
}