using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.VatRates
{
    /// <summary>
    /// DTO for creating a new VAT Nature.
    /// </summary>
    public class CreateVatNatureDto
    {
        /// <summary>
        /// Code of the VAT nature (e.g., "N1", "N2", "N3").
        /// </summary>
        [Required(ErrorMessage = "The code is required.")]
        [MaxLength(10, ErrorMessage = "The code cannot exceed 10 characters.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Name of the VAT nature.
        /// </summary>
        [Required(ErrorMessage = "The name is required.")]
        [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description explaining the purpose and usage of the VAT nature.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }
}
