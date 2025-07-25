using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Common;


/// <summary>
/// Represents a unit of measure entity (e.g., "Kg", "L", "Pcs").
/// </summary>
public class UM : AuditableEntity
{
    /// <summary>
    /// Name of the unit of measure (e.g., "Kilogram", "Liter", "Piece").
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
    [Display(Name = "Name", Description = "Name of the unit of measure.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Symbol of the unit of measure (e.g., "kg", "l", "pcs").
    /// </summary>
    [Required(ErrorMessage = "The symbol is required.")]
    [MaxLength(10, ErrorMessage = "The symbol cannot exceed 10 characters.")]
    [Display(Name = "Symbol", Description = "Symbol of the unit of measure.")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Description of the unit of measure.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The description cannot exceed 200 characters.")]
    [Display(Name = "Description", Description = "Description of the unit of measure.")]
    public string? Description { get; set; }

    /// <summary>
    /// Status of the unit of measure.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the unit of measure.")]
    public ProductUMStatus Status { get; set; } = ProductUMStatus.Active;

    /// <summary>
    /// Indicates if this is the default unit of measure.
    /// </summary>
    [Display(Name = "Default", Description = "Indicates if this is the default unit of measure.")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Products associated with this unit of measure.
    /// </summary>
    [Display(Name = "Products", Description = "Products associated with this unit of measure.")]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Status for unit of measure.
/// </summary>
public enum ProductUMStatus
{
    Active,     // Unit is active and usable
    Suspended,  // Unit is temporarily suspended
    Deleted     // Unit is deleted/disabled
}