using System.ComponentModel.DataAnnotations;

namespace EventForge.Data.Entities.StationMonitor;


/// <summary>
/// Represents an order queue item for a station (e.g., bar, kitchen), managed in FIFO order.
/// </summary>
public class StationOrderQueueItem : AuditableEntity
{
    /// <summary>
    /// Foreign key to the station.
    /// </summary>
    [Required(ErrorMessage = "The station is required.")]
    [Display(Name = "Station", Description = "Station that must handle the order.")]
    public Guid StationId { get; set; }
    public Station? Station { get; set; }

    /// <summary>
    /// Foreign key to the related document.
    /// </summary>
    [Required(ErrorMessage = "The document is required.")]
    [Display(Name = "Document", Description = "Source document of the order.")]
    public Guid DocumentHeaderId { get; set; }
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Foreign key to the specific document row (optional, for precise tracking).
    /// </summary>
    [Display(Name = "Document Row", Description = "Specific document row.")]
    public Guid? DocumentRowId { get; set; }
    public DocumentRow? DocumentRow { get; set; }

    /// <summary>
    /// Foreign key to the client or team member (optional).
    /// </summary>
    [Display(Name = "Team Member", Description = "Client or team member recipient.")]
    public Guid? TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }

    /// <summary>
    /// Foreign key to the product to be prepared.
    /// </summary>
    [Required(ErrorMessage = "The product is required.")]
    [Display(Name = "Product", Description = "Product to be prepared.")]
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>
    /// Quantity to be prepared.
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    [Display(Name = "Quantity", Description = "Quantity to be prepared.")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Current status of the queue item.
    /// </summary>
    [Display(Name = "Status", Description = "Order status.")]
    public StationOrderQueueStatus Status { get; set; } = StationOrderQueueStatus.Waiting;

    /// <summary>
    /// Custom sort order for FIFO management.
    /// </summary>
    [Display(Name = "Sort Order", Description = "Display order in the queue.")]
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Date and time when the item was assigned to the station (optional).
    /// </summary>
    [Display(Name = "Assigned At", Description = "Date and time assigned to the station.")]
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Date and time when preparation started (optional).
    /// </summary>
    [Display(Name = "Started At", Description = "Date and time when preparation started.")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Date and time when the item was completed (optional).
    /// </summary>
    [Display(Name = "Completed At", Description = "Date and time when completed.")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Additional notes for the queue item.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes.")]
    public string? Notes { get; set; }
}