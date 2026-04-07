using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for InsurancePolicy update operations.
    /// </summary>
    public class UpdateInsurancePolicyDto
    {
        /// <summary>
        /// Insurance provider company name.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The provider cannot exceed 100 characters.")]
        [Display(Name = "Provider", Description = "Insurance provider company name.")]
        public string? Provider { get; set; }

        /// <summary>
        /// Insurance policy number.
        /// </summary>
        [MaxLength(50, ErrorMessage = "The policy number cannot exceed 50 characters.")]
        [Display(Name = "Policy Number", Description = "Insurance policy number.")]
        public string? PolicyNumber { get; set; }

        /// <summary>
        /// Date from which the insurance is valid.
        /// </summary>
        [Display(Name = "Valid From", Description = "Date from which the insurance is valid.")]
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Date until which the insurance is valid.
        /// </summary>
        [Display(Name = "Valid To", Description = "Date until which the insurance is valid.")]
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Associated document reference ID (if policy document is uploaded).
        /// </summary>
        [Display(Name = "Document Reference", Description = "Associated document reference.")]
        public Guid? DocumentReferenceId { get; set; }

        /// <summary>
        /// Type or category of insurance coverage (e.g., "Sports Liability", "Medical", "Comprehensive").
        /// </summary>
        [MaxLength(100, ErrorMessage = "The coverage type cannot exceed 100 characters.")]
        [Display(Name = "Coverage Type", Description = "Type or category of insurance coverage.")]
        public string? CoverageType { get; set; }

        /// <summary>
        /// Maximum coverage amount (if applicable).
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Coverage amount must be non-negative.")]
        [Display(Name = "Coverage Amount", Description = "Maximum coverage amount.")]
        public decimal? CoverageAmount { get; set; }

        /// <summary>
        /// Currency for the coverage amount.
        /// </summary>
        [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
        [Display(Name = "Currency", Description = "Currency for the coverage amount.")]
        public string? Currency { get; set; }

        /// <summary>
        /// Additional notes about the insurance policy.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
        [Display(Name = "Notes", Description = "Additional notes about the insurance policy.")]
        public string? Notes { get; set; }
    }
}