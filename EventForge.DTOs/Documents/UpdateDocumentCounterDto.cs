using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for updating a document counter.
    /// </summary>
    public class UpdateDocumentCounterDto
    {
        /// <summary>
        /// Current counter value.
        /// </summary>
        [Required(ErrorMessage = "Current value is required.")]
        public int CurrentValue { get; set; }

        /// <summary>
        /// Prefix to prepend to the generated number (optional).
        /// </summary>
        [StringLength(10, ErrorMessage = "Prefix cannot exceed 10 characters.")]
        public string? Prefix { get; set; }

        /// <summary>
        /// Number of digits for zero-padding (e.g., 5 = "00001").
        /// </summary>
        [Range(1, 10, ErrorMessage = "Padding length must be between 1 and 10.")]
        public int PaddingLength { get; set; } = 5;

        /// <summary>
        /// Format pattern for the document number (e.g., "{PREFIX}{SERIES}/{YEAR}/{NUMBER}").
        /// Available placeholders: {PREFIX}, {SERIES}, {YEAR}, {NUMBER}
        /// </summary>
        [StringLength(50, ErrorMessage = "Format pattern cannot exceed 50 characters.")]
        public string? FormatPattern { get; set; }

        /// <summary>
        /// Indicates if this counter automatically resets at year change.
        /// </summary>
        public bool ResetOnYearChange { get; set; } = true;

        /// <summary>
        /// Additional notes or description.
        /// </summary>
        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }
    }
}
