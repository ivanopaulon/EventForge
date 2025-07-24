using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.PriceLists;

/// <summary>
/// DTO for PriceList update operations.
/// </summary>
public class UpdatePriceListDto
{
    /// <summary>
    /// Name of the price list.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the price list.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the price list.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the price list.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the price list validity.
    /// </summary>
    [Display(Name = "Valid From", Description = "Start date of the price list validity.")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date of the price list validity.
    /// </summary>
    [Display(Name = "Valid To", Description = "End date of the price list validity.")]
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Additional notes for the price list.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the price list.")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Status of the price list.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the price list.")]
    public PriceListStatus Status { get; set; }

    /// <summary>
    /// Indicates if this is the default price list for the event.
    /// </summary>
    [Display(Name = "Default", Description = "Indicates if this is the default price list for the event.")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Priority of the price list (0 = highest priority).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100.")]
    [Display(Name = "Priority", Description = "Priority of the price list (0 = highest priority).")]
    public int Priority { get; set; }
}