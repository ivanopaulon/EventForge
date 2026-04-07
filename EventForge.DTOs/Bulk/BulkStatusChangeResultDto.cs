namespace EventForge.DTOs.Bulk;

/// <summary>
/// Result of a bulk status change operation.
/// </summary>
public class BulkStatusChangeResultDto
{
    /// <summary>
    /// Total number of documents requested for status change.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of documents successfully changed.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of documents that failed to change.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// List of errors that occurred during the status change.
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
