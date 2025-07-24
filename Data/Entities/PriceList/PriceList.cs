using System.ComponentModel.DataAnnotations;
using EventForge.Data.Entities.Events;
using EventForge.Data.Entities.Audit;

namespace EventForge.Data.Entities.PriceList;

/// <summary>
/// Represents a price list that can be used for one or more events.
/// </summary>
public class PriceList : AuditableEntity
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
    public PriceListStatus Status { get; set; } = PriceListStatus.Active;

    /// <summary>
    /// Indicates if this is the default price list for the event.
    /// </summary>
    [Display(Name = "Default", Description = "Indicates if this is the default price list for the event.")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Priority of the price list (0 = highest priority).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100.")]
    [Display(Name = "Priority", Description = "Priority of the price list (0 = highest priority).")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Event associated with the price list.
    /// </summary>
    [Required]
    [Display(Name = "Event", Description = "Event associated with the price list.")]
    public Guid EventId { get; set; }

    /// <summary>
    /// Navigation property for the associated event.
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Product prices associated with this price list.
    /// </summary>
    [Display(Name = "Product Prices", Description = "Product prices associated with this price list.")]
    public ICollection<PriceListEntry> ProductPrices { get; set; } = new List<PriceListEntry>();
}

/// <summary>
/// Status for the price list.
/// </summary>
public enum PriceListStatus
{
    Active,     // Price list is active and usable
    Suspended,  // Temporarily suspended
    Expired,    // Price list is expired (no longer valid)
    Deleted     // Price list is deleted/disabled
}