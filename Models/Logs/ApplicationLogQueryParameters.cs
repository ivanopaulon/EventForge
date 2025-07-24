using System.ComponentModel.DataAnnotations;

namespace EventForge.Models.Logs;

/// <summary>
/// Query parameters for filtering and paginating application logs.
/// </summary>
public class ApplicationLogQueryParameters
{
    /// <summary>
    /// Filter by log level (Information, Warning, Error, Debug, etc.).
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Filter by message content (contains search).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter logs that contain exceptions.
    /// </summary>
    public bool? HasException { get; set; }

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
    /// Sort field (defaults to TimeStamp).
    /// </summary>
    public string SortBy { get; set; } = "TimeStamp";

    /// <summary>
    /// Sort direction (asc or desc, defaults to desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Calculate skip count for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}