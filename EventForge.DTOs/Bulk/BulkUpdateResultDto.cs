namespace EventForge.DTOs.Bulk;

/// <summary>
/// Result of a bulk update operation.
/// </summary>
public class BulkUpdateResultDto
{
    /// <summary>
    /// Total number of items requested for update.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of items successfully updated.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of items that failed to update.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// List of errors that occurred during the update.
    /// </summary>
    public List<BulkItemError> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when the operation was processed.
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Time taken to process the operation.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Indicates if the transaction was rolled back due to errors.
    /// </summary>
    public bool RolledBack { get; set; }
}
