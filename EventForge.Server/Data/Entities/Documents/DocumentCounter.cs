using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Manages counters for automatic document numbering per document type and series.
/// </summary>
public class DocumentCounter : AuditableEntity
{
    /// <summary>
    /// Document type for which this counter is used.
    /// </summary>
    [Required(ErrorMessage = "Document type is required.")]
    [Display(Name = "Document Type", Description = "Document type for which this counter is used.")]
    public Guid DocumentTypeId { get; set; }

    /// <summary>
    /// Navigation property for the document type.
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Series identifier (e.g., "A", "B", "2024", etc.). Empty string for default series.
    /// </summary>
    [Required(ErrorMessage = "Series is required.")]
    [StringLength(10, ErrorMessage = "Series cannot exceed 10 characters.")]
    [Display(Name = "Series", Description = "Series identifier for progressive numbering.")]
    public string Series { get; set; } = string.Empty;

    /// <summary>
    /// Current counter value.
    /// </summary>
    [Required(ErrorMessage = "Current value is required.")]
    [Display(Name = "Current Value", Description = "Current counter value.")]
    public int CurrentValue { get; set; } = 0;

    /// <summary>
    /// Year for which this counter is valid (null = valid for all years).
    /// </summary>
    [Display(Name = "Year", Description = "Year for which this counter is valid.")]
    public int? Year { get; set; }

    /// <summary>
    /// Prefix to prepend to the generated number (optional).
    /// </summary>
    [StringLength(10, ErrorMessage = "Prefix cannot exceed 10 characters.")]
    [Display(Name = "Prefix", Description = "Prefix to prepend to the generated number.")]
    public string? Prefix { get; set; }

    /// <summary>
    /// Number of digits for zero-padding (e.g., 5 = "00001").
    /// </summary>
    [Range(1, 10, ErrorMessage = "Padding length must be between 1 and 10.")]
    [Display(Name = "Padding Length", Description = "Number of digits for zero-padding.")]
    public int PaddingLength { get; set; } = 5;

    /// <summary>
    /// Format pattern for the document number (e.g., "{PREFIX}{SERIES}/{YEAR}/{NUMBER}").
    /// Available placeholders: {PREFIX}, {SERIES}, {YEAR}, {NUMBER}
    /// </summary>
    [StringLength(50, ErrorMessage = "Format pattern cannot exceed 50 characters.")]
    [Display(Name = "Format Pattern", Description = "Format pattern for the document number.")]
    public string? FormatPattern { get; set; }

    /// <summary>
    /// Indicates if this counter automatically resets at year change.
    /// </summary>
    [Display(Name = "Reset on Year Change", Description = "Indicates if this counter resets at year change.")]
    public bool ResetOnYearChange { get; set; } = true;

    /// <summary>
    /// Additional notes or description.
    /// </summary>
    [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    [Display(Name = "Notes", Description = "Additional notes or description.")]
    public string? Notes { get; set; }
}
