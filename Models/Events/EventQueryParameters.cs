using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.Events;

/// <summary>
/// Query parameters for filtering and paginating events.
/// </summary>
public class EventQueryParameters
{
    /// <summary>
    /// Filter by event name (partial match).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by location (partial match).
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Filter by event status.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// Filter by start date (events starting from this date).
    /// </summary>
    public DateTime? StartDateFrom { get; set; }

    /// <summary>
    /// Filter by start date (events starting until this date).
    /// </summary>
    public DateTime? StartDateTo { get; set; }

    /// <summary>
    /// Filter by end date (events ending from this date).
    /// </summary>
    public DateTime? EndDateFrom { get; set; }

    /// <summary>
    /// Filter by end date (events ending until this date).
    /// </summary>
    public DateTime? EndDateTo { get; set; }

    /// <summary>
    /// Filter by minimum capacity.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MinCapacity must be greater than 0.")]
    public int? MinCapacity { get; set; }

    /// <summary>
    /// Filter by maximum capacity.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "MaxCapacity must be greater than 0.")]
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Include teams and members in the response.
    /// </summary>
    public bool IncludeTeams { get; set; } = false;

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field (defaults to StartDate).
    /// </summary>
    public string SortBy { get; set; } = "StartDate";

    /// <summary>
    /// Sort direction (asc or desc, defaults to asc).
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Calculate skip count for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}