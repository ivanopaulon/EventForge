namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Result of a bulk price update operation.
/// </summary>
public class BulkUpdateResultDto
{
    /// <summary>
    /// Number of items successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of items that failed to update.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// List of errors encountered during the update.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when the update was performed.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
