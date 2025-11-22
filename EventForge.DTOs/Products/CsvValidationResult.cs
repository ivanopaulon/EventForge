using System.Collections.Generic;

namespace EventForge.DTOs.Products;

/// <summary>
/// Result of CSV validation before import.
/// </summary>
public class CsvValidationResult
{
    /// <summary>
    /// Whether the CSV file is valid for import.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of detected column names from the CSV.
    /// </summary>
    public List<string> DetectedColumns { get; set; } = new();

    /// <summary>
    /// Auto-detected column mapping suggestions.
    /// </summary>
    public ColumnMapping SuggestedMapping { get; set; } = new();

    /// <summary>
    /// Preview rows from the CSV (first 10 rows).
    /// </summary>
    public List<CsvPreviewRow> PreviewRows { get; set; } = new();

    /// <summary>
    /// Validation errors found in the CSV.
    /// </summary>
    public List<CsvImportError> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Information about the CSV file.
    /// </summary>
    public CsvFileInfo FileInfo { get; set; } = new();
}
