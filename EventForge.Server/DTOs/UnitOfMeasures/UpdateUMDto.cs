using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.UnitOfMeasures;

/// <summary>
/// DTO for Unit of Measure update operations.
/// </summary>
public class UpdateUMDto
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
    public ProductUMStatus Status { get; set; }

    /// <summary>
    /// Indicates if this is the default unit of measure.
    /// </summary>
    [Display(Name = "Default", Description = "Indicates if this is the default unit of measure.")]
    public bool IsDefault { get; set; }
}