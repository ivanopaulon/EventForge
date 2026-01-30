using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for creating a product code with associated unit of measure.
    /// Used for quick product creation with multiple barcodes/UoMs.
    /// </summary>
    public class ProductCodeWithUnitDto
    {
        /// <summary>
        /// Code type (SKU, EAN, UPC, Barcode, etc.).
        /// </summary>
        [Required(ErrorMessage = "The code type is required.")]
        [MaxLength(30, ErrorMessage = "The code type cannot exceed 30 characters.")]
        public string CodeType { get; set; } = "Barcode";

        /// <summary>
        /// Code value (barcode, SKU, etc.).
        /// </summary>
        [Required(ErrorMessage = "The code value is required.")]
        [MaxLength(100, ErrorMessage = "The code value cannot exceed 100 characters.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Alternative description for the code.
        /// </summary>
        [MaxLength(200, ErrorMessage = "The alternative description cannot exceed 200 characters.")]
        public string? AlternativeDescription { get; set; }

        /// <summary>
        /// Optional unit of measure identifier for this code.
        /// If specified, a ProductUnit will be created/linked.
        /// </summary>
        public Guid? UnitOfMeasureId { get; set; }

        /// <summary>
        /// Unit type (Base, Pack, Pallet, etc.).
        /// Used when creating a new ProductUnit.
        /// </summary>
        [MaxLength(20, ErrorMessage = "The unit type cannot exceed 20 characters.")]
        public string UnitType { get; set; } = "Base";

        /// <summary>
        /// Conversion factor to the base unit.
        /// Must be greater than 0.001 for non-base units.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "The conversion factor must be greater than 0.001.")]
        public decimal ConversionFactor { get; set; } = 1m;

        /// <summary>
        /// Additional description for the unit.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The unit description cannot exceed 100 characters.")]
        public string? UnitDescription { get; set; }
    }
}
