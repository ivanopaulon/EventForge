using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.Products;

/// <summary>
/// Represents an alias code (e.g., SKU, barcode) for a product.
/// </summary>
public class ProductCode : AuditableEntity
{
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

    /// <summary>
    /// Code type (SKU, EAN, UPC, etc.).
    /// </summary>
    [Required(ErrorMessage = "The code type is required.")]
    [MaxLength(30, ErrorMessage = "The code type cannot exceed 30 characters.")]
    [Display(Name = "Code Type", Description = "Type of the code (SKU, EAN, UPC, etc.).")]
    public string CodeType { get; set; } = string.Empty;

    /// <summary>
    /// Code value.
    /// </summary>
    [Required(ErrorMessage = "The code value is required.")]
    [MaxLength(100, ErrorMessage = "The code value cannot exceed 100 characters.")]
    [Display(Name = "Code", Description = "Value of the code.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Alternative description for the code.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The alternative description cannot exceed 200 characters.")]
    [Display(Name = "Alternative Description", Description = "Alternative description for the code.")]
    public string? AlternativeDescription { get; set; }

    /// <summary>
    /// Status of the product code.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the product code.")]
    public ProductCodeStatus Status { get; set; } = ProductCodeStatus.Active;
}

/// <summary>
/// Status for the product code.
/// </summary>
public enum ProductCodeStatus
{
    Active,     // Code is active and usable
    Suspended,  // Code is temporarily suspended
    Expired,    // Code is expired/no longer valid
    Deleted     // Code is deleted/disabled
}