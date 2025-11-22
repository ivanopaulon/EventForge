using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// Request DTO for querying price history with filters, sorting, and pagination.
/// </summary>
public class PriceHistoryRequest
{
    /// <summary>
    /// Start date for filtering price history (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for filtering price history (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by change source (Manual, BulkEdit, CSVImport, AutoUpdate).
    /// </summary>
    [MaxLength(50)]
    public string? ChangeSource { get; set; }

    /// <summary>
    /// Minimum absolute change percentage to include (e.g., 5 for +5% or -5%).
    /// </summary>
    [Range(0, 1000)]
    public decimal? MinChangePercentage { get; set; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Field to sort by (ChangedAt, PriceChange, PriceChangePercentage).
    /// </summary>
    [MaxLength(50)]
    public string SortBy { get; set; } = "ChangedAt";

    /// <summary>
    /// Sort direction (Asc, Desc).
    /// </summary>
    [MaxLength(10)]
    public string SortDirection { get; set; } = "Desc";
}
