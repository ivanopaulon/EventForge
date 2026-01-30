namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for DocumentCounter output/display operations.
    /// </summary>
    public class DocumentCounterDto
    {
        /// <summary>
        /// Unique identifier for the document counter.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Document type for which this counter is used.
        /// </summary>
        public Guid DocumentTypeId { get; set; }

        /// <summary>
        /// Document type name for display.
        /// </summary>
        public string? DocumentTypeName { get; set; }

        /// <summary>
        /// Series identifier (e.g., "A", "B", "2024", etc.).
        /// </summary>
        public string Series { get; set; } = string.Empty;

        /// <summary>
        /// Current counter value.
        /// </summary>
        public int CurrentValue { get; set; }

        /// <summary>
        /// Year for which this counter is valid (null = valid for all years).
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Prefix to prepend to the generated number (optional).
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Number of digits for zero-padding (e.g., 5 = "00001").
        /// </summary>
        public int PaddingLength { get; set; }

        /// <summary>
        /// Format pattern for the document number.
        /// </summary>
        public string? FormatPattern { get; set; }

        /// <summary>
        /// Indicates if this counter automatically resets at year change.
        /// </summary>
        public bool ResetOnYearChange { get; set; }

        /// <summary>
        /// Additional notes or description.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the counter was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the counter.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the counter was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the counter.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
