using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Logs;

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
    /// Filter by logger category or source.
    /// </summary>
    public string? Logger { get; set; }

    /// <summary>
    /// Filter by message content (partial match).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Filter by machine name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Filter by application name.
    /// </summary>
    public string? Application { get; set; }

    /// <summary>
    /// Filter by environment.
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Filter by correlation ID.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by request path.
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Filter by request method.
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// Filter by HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Filter by minimum log level severity.
    /// </summary>
    public string? MinLevel { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

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
    /// Sort field (defaults to Timestamp).
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc or desc, defaults to desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Calculate skip count for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}