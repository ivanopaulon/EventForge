namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for InsurancePolicy output/display operations.
    /// </summary>
    public class InsurancePolicyDto
    {
        /// <summary>
        /// Unique identifier for the insurance policy.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Associated team member ID.
        /// </summary>
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Team member name (for display purposes).
        /// </summary>
        public string? TeamMemberName { get; set; }

        /// <summary>
        /// Insurance provider company name.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Insurance policy number.
        /// </summary>
        public string PolicyNumber { get; set; } = string.Empty;

        /// <summary>
        /// Date from which the insurance is valid.
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Date until which the insurance is valid.
        /// </summary>
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Associated document reference ID (if policy document is uploaded).
        /// </summary>
        public Guid? DocumentReferenceId { get; set; }

        /// <summary>
        /// Type or category of insurance coverage (e.g., "Sports Liability", "Medical", "Comprehensive").
        /// </summary>
        public string? CoverageType { get; set; }

        /// <summary>
        /// Maximum coverage amount (if applicable).
        /// </summary>
        public decimal? CoverageAmount { get; set; }

        /// <summary>
        /// Currency for the coverage amount.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Additional notes about the insurance policy.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the insurance policy was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the insurance policy.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the insurance policy was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the insurance policy.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates if the insurance policy is currently valid.
        /// </summary>
        public bool IsValid => DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;

        /// <summary>
        /// Days until expiration (calculated property).
        /// </summary>
        public int DaysUntilExpiration => (ValidTo.Date - DateTime.UtcNow.Date).Days;

        /// <summary>
        /// Status description based on validity.
        /// </summary>
        public string StatusDescription
        {
            get
            {
                if (!IsValid && DateTime.UtcNow < ValidFrom)
                    return "Not yet valid";
                if (!IsValid && DateTime.UtcNow > ValidTo)
                    return "Expired";
                if (DaysUntilExpiration <= 30 && DaysUntilExpiration > 0)
                    return "Expiring soon";
                return "Valid";
            }
        }

        /// <summary>
        /// Formatted coverage amount with currency.
        /// </summary>
        public string? FormattedCoverageAmount
        {
            get
            {
                if (CoverageAmount.HasValue)
                {
                    return $"{CoverageAmount:N2} {Currency ?? "EUR"}";
                }
                return null;
            }
        }
    }
}