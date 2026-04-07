namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for Model output operations.
    /// </summary>
    public class ModelDto
    {
        /// <summary>
        /// Unique identifier for the model.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Brand identifier.
        /// </summary>
        public Guid BrandId { get; set; }

        /// <summary>
        /// Brand name.
        /// </summary>
        public string? BrandName { get; set; }

        /// <summary>
        /// Model name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Model description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Manufacturer part number (MPN).
        /// </summary>
        public string? ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Date and time when the model was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the model.
        /// </summary>
        public string? CreatedBy { get; set; }
    }
}
