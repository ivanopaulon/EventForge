namespace EventForge.DTOs.PriceLists
{

    /// <summary>
    /// DTO representing the result of a bulk import operation for price list entries.
    /// Part of Issue #245 price optimization implementation.
    /// </summary>
    public class BulkImportResultDto
    {
        /// <summary>
        /// Total number of entries processed in the import.
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Number of entries successfully imported.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of entries that failed to import.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Number of existing entries that were updated/replaced.
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Number of new entries that were created.
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// Number of entries that were skipped due to validation errors.
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// Duration of the import operation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Date and time when the import was performed.
        /// </summary>
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who performed the import.
        /// </summary>
        public string? ImportedBy { get; set; }

        /// <summary>
        /// Price list identifier where entries were imported.
        /// </summary>
        public Guid PriceListId { get; set; }

        /// <summary>
        /// Whether existing entries were replaced during import.
        /// </summary>
        public bool ReplacedExisting { get; set; }

        /// <summary>
        /// Collection of validation errors encountered during import.
        /// </summary>
        public ICollection<BulkImportErrorDto> Errors { get; set; } = new List<BulkImportErrorDto>();

        /// <summary>
        /// Collection of warnings generated during import.
        /// </summary>
        public ICollection<BulkImportWarningDto> Warnings { get; set; } = new List<BulkImportWarningDto>();

        /// <summary>
        /// Whether the import was completely successful (no failures or errors).
        /// </summary>
        public bool IsSuccessful => FailureCount == 0 && !Errors.Any();

        /// <summary>
        /// Summary message describing the import result.
        /// </summary>
        public string SummaryMessage => $"Processed {TotalProcessed} entries: {SuccessCount} succeeded, {FailureCount} failed, {SkippedCount} skipped";
    }

    /// <summary>
    /// DTO representing an error encountered during bulk import.
    /// </summary>
    public class BulkImportErrorDto
    {
        /// <summary>
        /// Row number or index where the error occurred.
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Product identifier that caused the error (if available).
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Error code or type.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Detailed error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Field or property name that caused the error (if applicable).
        /// </summary>
        public string? FieldName { get; set; }

        /// <summary>
        /// Original value that caused the error (if applicable).
        /// </summary>
        public string? OriginalValue { get; set; }
    }

    /// <summary>
    /// DTO representing a warning generated during bulk import.
    /// </summary>
    public class BulkImportWarningDto
    {
        /// <summary>
        /// Row number or index where the warning occurred.
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Product identifier related to the warning (if available).
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Warning code or type.
        /// </summary>
        public string WarningCode { get; set; } = string.Empty;

        /// <summary>
        /// Warning message.
        /// </summary>
        public string WarningMessage { get; set; } = string.Empty;

        /// <summary>
        /// Action taken to resolve the warning.
        /// </summary>
        public string? ActionTaken { get; set; }
    }
}