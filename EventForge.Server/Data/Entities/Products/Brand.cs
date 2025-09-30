using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Products;

/// <summary>
/// Represents a product brand/manufacturer.
/// </summary>
public class Brand : AuditableEntity
{
    /// <summary>
    /// Brand name.
    /// </summary>
    [Required(ErrorMessage = "The brand name is required.")]
    [MaxLength(200, ErrorMessage = "The brand name cannot exceed 200 characters.")]
    [Display(Name = "Name", Description = "Brand name.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brand description.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Brand description.")]
    public string? Description { get; set; }

    /// <summary>
    /// Brand website URL.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The website URL cannot exceed 500 characters.")]
    [Display(Name = "Website", Description = "Brand website URL.")]
    public string? Website { get; set; }

    /// <summary>
    /// Country of origin or headquarters.
    /// </summary>
    [MaxLength(100, ErrorMessage = "The country cannot exceed 100 characters.")]
    [Display(Name = "Country", Description = "Country of origin or headquarters.")]
    public string? Country { get; set; }

    /// <summary>
    /// Models associated with this brand.
    /// </summary>
    [Display(Name = "Models", Description = "Models associated with this brand.")]
    public ICollection<Model> Models { get; set; } = new List<Model>();

    /// <summary>
    /// Products associated with this brand.
    /// </summary>
    [Display(Name = "Products", Description = "Products associated with this brand.")]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
