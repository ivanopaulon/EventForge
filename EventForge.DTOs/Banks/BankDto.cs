namespace EventForge.DTOs.Banks
{

    /// <summary>
    /// DTO for Bank output/display operations.
    /// </summary>
    public class BankDto
    {
        /// <summary>
        /// Unique identifier for the bank.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the bank.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Bank code (e.g., ABI, SWIFT/BIC).
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// SWIFT/BIC code.
        /// </summary>
        public string? SwiftBic { get; set; }

        /// <summary>
        /// Bank branch or agency.
        /// </summary>
        public string? Branch { get; set; }

        /// <summary>
        /// Bank address.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Country where the bank is located.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Bank phone number.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Bank email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the bank was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the bank.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the bank was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the bank.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
