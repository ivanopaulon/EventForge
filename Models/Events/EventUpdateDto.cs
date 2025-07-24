using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.Events;

/// <summary>
/// DTO for updating an existing event.
/// </summary>
public class EventUpdateDto
{
    /// <summary>
    /// Event name.
    /// </summary>
    [Required(ErrorMessage = "The event name is required.")]
    [MaxLength(100, ErrorMessage = "The event name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short event description.
    /// </summary>
    [Required(ErrorMessage = "The short description is required.")]
    [MaxLength(200, ErrorMessage = "The short description cannot exceed 200 characters.")]
    public string ShortDescription { get; set; } = string.Empty;

    /// <summary>
    /// Detailed event description.
    /// </summary>
    [MaxLength(2000, ErrorMessage = "The long description cannot exceed 2000 characters.")]
    public string? LongDescription { get; set; }

    /// <summary>
    /// Event location.
    /// </summary>
    [MaxLength(200, ErrorMessage = "The location cannot exceed 200 characters.")]
    public string? Location { get; set; }

    /// <summary>
    /// Event start date and time (UTC).
    /// </summary>
    [Required(ErrorMessage = "The start date is required.")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Event end date and time (UTC).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum event capacity.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; } = 1;

    /// <summary>
    /// Event status.
    /// </summary>
    public int Status { get; set; } = 0; // EventStatus.Planned
}