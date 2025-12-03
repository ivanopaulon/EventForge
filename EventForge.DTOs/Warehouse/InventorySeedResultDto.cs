using System;

namespace EventForge.DTOs.Warehouse;

/// <summary>
/// DTO for the result of a bulk inventory seed operation.
/// </summary>
public class InventorySeedResultDto
{
    /// <summary>
    /// Number of products found for seeding.
    /// </summary>
    public int ProductsFound { get; set; }

    /// <summary>
    /// Number of inventory rows created.
    /// </summary>
    public int RowsCreated { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Informational message about the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID of the created inventory document (if CreateDocument was true).
    /// </summary>
    public Guid? DocumentId { get; set; }
}
