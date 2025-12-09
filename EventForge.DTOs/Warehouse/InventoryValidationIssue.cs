using System;

namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Represents a validation issue found in an inventory document.
/// </summary>
public class InventoryValidationIssue
{
    /// <summary>
    /// Severity level of the issue.
    /// </summary>
    public string Severity { get; set; } = string.Empty; // "Critical", "Error", "Warning", "Info"

    /// <summary>
    /// Issue code for categorization.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message describing the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional row ID if the issue is specific to a row.
    /// </summary>
    public Guid? RowId { get; set; }

    /// <summary>
    /// Additional details about the issue.
    /// </summary>
    public string? Details { get; set; }
}
