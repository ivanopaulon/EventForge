namespace EventForge.DTOs.PriceHistory;

/// <summary>
/// Response DTO for price history queries with pagination and statistics.
/// </summary>
public class PriceHistoryResponse
{
    /// <summary>
    /// List of price history items for the current page.
    /// </summary>
    public List<PriceHistoryItem> Items { get; set; } = new();

    /// <summary>
    /// Total number of records matching the query.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Statistics for the entire result set (not just the current page).
    /// </summary>
    public PriceHistoryStatistics? Statistics { get; set; }
}
