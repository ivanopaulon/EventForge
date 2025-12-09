using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse;

/// <summary>
/// Result of an inventory document validation check.
/// </summary>
public class InventoryValidationResultDto
{
    /// <summary>
    /// ID of the validated document.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Timestamp when the validation was performed.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Total number of rows in the document.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Whether the document passed validation (no critical/error issues).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of issues found during validation.
    /// </summary>
    public List<InventoryValidationIssue> Issues { get; set; } = new List<InventoryValidationIssue>();

    /// <summary>
    /// Statistics about the document.
    /// </summary>
    public InventoryStats Stats { get; set; } = new InventoryStats();
}
