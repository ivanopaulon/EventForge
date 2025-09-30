using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Products;

/// <summary>
/// Represents a product model within a brand.
/// </summary>
public class Model : AuditableEntity
{
    /// <summary>
    /// Brand identifier (foreign key).
    /// </summary>
    [Required(ErrorMessage = "The brand is required.")]
    [Display(Name = "Brand", Description = "Brand identifier.")]
    public Guid BrandId { get; set; }

    /// <summary>
    /// Brand associated with this model.
    /// </summary>
    public Brand? Brand { get; set; }

    /// <summary>
    /// Model name.
    /// </summary>
    [Required(ErrorMessage = "The model name is required.")]
    [MaxLength(200, ErrorMessage = "The model name cannot exceed 200 characters.")]
    [Display(Name = "Name", Description = "Model name.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model description.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Model description.")]
    public string? Description { get; set; }

    /// <summary>
    /// Manufacturer part number (MPN).
    /// </summary>
    [MaxLength(100, ErrorMessage = "The manufacturer part number cannot exceed 100 characters.")]
    [Display(Name = "Manufacturer Part Number", Description = "Manufacturer part number (MPN).")]
    public string? ManufacturerPartNumber { get; set; }

    /// <summary>
    /// Products associated with this model.
    /// </summary>
    [Display(Name = "Products", Description = "Products associated with this model.")]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
