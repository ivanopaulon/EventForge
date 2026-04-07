using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for creating a new Brand.
    /// </summary>
    public class CreateBrandDto
    {
        /// <summary>
        /// Brand name.
        /// </summary>
        [Required(ErrorMessage = "The brand name is required.")]
        [MaxLength(200, ErrorMessage = "The brand name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Brand description.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "The description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Brand website URL.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The website URL cannot exceed 500 characters.")]
        public string? Website { get; set; }

        /// <summary>
        /// Country of origin or headquarters.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The country cannot exceed 100 characters.")]
        public string? Country { get; set; }
    }
}
