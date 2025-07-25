using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.Audit;

/// <summary>
/// Query parameters for filtering and paginating audit logs.
/// </summary>
public class AuditLogQueryParameters
{
    /// <summary>
    /// Filter by entity name.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Filter by entity ID.
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Filter by user who made the change.
    /// </summary>
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Filter by operation type (Insert, Update, Delete).
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Filter by property name.
    /// </summary>
    public string? PropertyName { get; set; }

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
    /// Sort field (defaults to ChangedAt).
    /// </summary>
    public string SortBy { get; set; } = "ChangedAt";

    /// <summary>
    /// Sort direction (asc or desc, defaults to desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Calculate skip count for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}