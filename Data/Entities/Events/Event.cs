using System.ComponentModel.DataAnnotations;
using EventForge.Data.Entities.Teams;

namespace EventForge.Data.Entities.Events;

/// <summary>
/// Represents the base class for an event entity in the domain.
/// This entity contains only domain invariants and business logic that must always be enforced,
/// regardless of the data source (API, UI, import, etc.).
/// All input validation is handled at the DTO layer.
/// </summary>
public class Event : AuditableEntity
{
    /// <summary>
    /// Unique event name. Must be set for a valid domain object.
    /// </summary>
    [Required(ErrorMessage = "The event name is required.")]
    [MaxLength(100, ErrorMessage = "The event name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Unique event name.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short event description. Must be set for a valid domain object.
    /// </summary>
    [Required(ErrorMessage = "The short description is required.")]
    [MaxLength(200, ErrorMessage = "The short description cannot exceed 200 characters.")]
    [Display(Name = "Short Description", Description = "Short event description.")]
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed event description.
    /// </summary>
    [Display(Name = "Long Description", Description = "Detailed event description.")]
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>
    /// Event location.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The location cannot exceed 200 characters.")]
    [Display(Name = "Location", Description = "Event location.")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Event start date and time (UTC).
    /// </summary>
    [Required(ErrorMessage = "The start date is required.")]
    [Display(Name = "Start Date", Description = "Event start date and time (UTC).")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Event end date and time (UTC).
    /// </summary>
    [Display(Name = "End Date", Description = "Event end date and time (UTC).")]
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum event capacity.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    [Display(Name = "Capacity", Description = "Maximum event capacity.")]
    public int Capacity { get; set; } = 1;

    /// <summary>
    /// Event status.
    /// </summary>
    [Display(Name = "Status", Description = "Event status.")]
    public EventStatus Status { get; set; } = EventStatus.Planned;

    /// <summary>
    /// Teams associated with the event.
    /// </summary>
    [Display(Name = "Teams", Description = "Teams associated with the event.")]
    public ICollection<Team> Teams { get; set; } = new List<Team>();

    /// <summary>
    /// Price lists associated with the event.
    /// </summary>
    [Display(Name = "Price Lists", Description = "Price lists associated with the event.")]
    public ICollection<Data.Entities.PriceList.PriceList> PriceLists { get; set; } = new List<Data.Entities.PriceList.PriceList>();

    /// <summary>
    /// Checks domain invariants for the event entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if domain invariants are violated.</exception>
    public void CheckInvariants()
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
            throw new InvalidOperationException("End date must be after start date.");
        if (Capacity < 1)
            throw new InvalidOperationException("Capacity must be at least 1.");
    }
}

/// <summary>
/// Event status enumeration.
/// </summary>
public enum EventStatus
{
    Planned,    // Planned
    Ongoing,    // Ongoing
    Completed,  // Completed
    Cancelled   // Cancelled
}

