namespace EventForge.DTOs.Bulk;

/// <summary>
/// Represents an error that occurred during bulk operation for a specific item.
/// </summary>
public class BulkItemError
{
    /// <summary>
    /// ID of the item that failed.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Optional item name or code for display.
    /// </summary>
    public string? ItemName { get; set; }

    /// <summary>
    /// Error message describing what went wrong.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
