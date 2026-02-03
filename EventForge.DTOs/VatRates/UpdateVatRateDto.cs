using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.VatRates
{

    /// <summary>
    /// DTO for VAT Rate update operations.
    /// </summary>
    public class UpdateVatRateDto
    {
        /// <summary>
        /// Name of the VAT rate (e.g., "VAT 22%").
        /// </summary>
        [Required(ErrorMessage = "The name is required.")]
        [MaxLength(50, ErrorMessage = "The name cannot exceed 50 characters.")]
        [Display(Name = "Name", Description = "Name of the VAT rate.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Percentage of the VAT rate (e.g., 22).
        /// </summary>
        [Range(0, 100, ErrorMessage = "The percentage must be between 0 and 100.")]
        [Display(Name = "Percentage", Description = "Percentage of the VAT rate.")]
        public decimal Percentage { get; set; }

        /// <summary>
        /// Status of the VAT rate.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the VAT rate.")]
        public VatRateStatus Status { get; set; }

        /// <summary>
        /// Start date of the VAT rate validity.
        /// </summary>
        [Display(Name = "Valid From", Description = "Start date of the VAT rate validity.")]
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// End date of the VAT rate validity.
        /// </summary>
        [Display(Name = "Valid To", Description = "End date of the VAT rate validity.")]
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Additional notes about the VAT rate.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The notes cannot exceed 200 characters.")]
        [Display(Name = "Notes", Description = "Additional notes about the VAT rate.")]
        public string? Notes { get; set; }

        /// <summary>
        /// Foreign key to the VAT nature (optional, used for Italian tax compliance).
        /// </summary>
        [Display(Name = "VAT Nature", Description = "Reference to the VAT nature for Italian tax compliance.")]
        public Guid? VatNatureId { get; set; }

        // --- Fiscal Printer Support ---

        /// <summary>
        /// Fiscal code for fiscal printer integration (1-10).
        /// </summary>
        [Range(1, 10, ErrorMessage = "Fiscal code must be between 1 and 10.")]
        [Display(Name = "Fiscal Code", Description = "Code for fiscal printer integration (1-10).")]
        public int? FiscalCode { get; set; }
    }
}
