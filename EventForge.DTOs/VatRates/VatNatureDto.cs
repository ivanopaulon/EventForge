namespace EventForge.DTOs.VatRates
{
    /// <summary>
    /// DTO for VAT Nature output/display operations.
    /// </summary>
    public class VatNatureDto
    {
        /// <summary>
        /// Unique identifier for the VAT nature.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Code of the VAT nature (e.g., "N1", "N2", "N3").
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Name of the VAT nature.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description explaining the purpose and usage of the VAT nature.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Date and time when the VAT nature was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the VAT nature.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the VAT nature was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the VAT nature.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
