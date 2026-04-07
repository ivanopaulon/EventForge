using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for updating a Model.
    /// </summary>
    public class UpdateModelDto
    {
        /// <summary>
        /// Brand identifier.
        /// </summary>
        [Required(ErrorMessage = "The brand is required.")]
        public Guid BrandId { get; set; }

        /// <summary>
        /// Model name.
        /// </summary>
        [Required(ErrorMessage = "The model name is required.")]
        [MaxLength(200, ErrorMessage = "The model name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Model description.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "The description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Manufacturer part number (MPN).
        /// </summary>
        [MaxLength(100, ErrorMessage = "The manufacturer part number cannot exceed 100 characters.")]
        public string? ManufacturerPartNumber { get; set; }
    }
}
