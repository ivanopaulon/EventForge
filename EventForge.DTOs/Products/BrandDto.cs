using System;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for Brand output operations.
    /// </summary>
    public class BrandDto
    {
        /// <summary>
        /// Unique identifier for the brand.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Brand name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Brand description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Brand website URL.
        /// </summary>
        public string? Website { get; set; }

        /// <summary>
        /// Country of origin or headquarters.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Date and time when the brand was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the brand.
        /// </summary>
        public string? CreatedBy { get; set; }
    }
}
