using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Products
{

    /// <summary>
    /// DTO for ProductUnit creation operations.
    /// </summary>
    public class CreateProductUnitDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        [Required(ErrorMessage = "The product is required.")]
        [Display(Name = "Product", Description = "Identifier of the associated product.")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Unit of measure identifier.
        /// </summary>
        [Required(ErrorMessage = "The unit of measure is required.")]
        [Display(Name = "Unit of Measure", Description = "Identifier of the unit of measure.")]
        public Guid UnitOfMeasureId { get; set; }

        /// <summary>
        /// Conversion factor to the base unit.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "The conversion factor must be greater than zero.")]
        [Display(Name = "Conversion Factor", Description = "Number of base units contained in this unit.")]
        public decimal ConversionFactor { get; set; } = 1m;

        /// <summary>
        /// Unit type (e.g., Base, Pack, Pallet).
        /// </summary>
        [Required(ErrorMessage = "The unit type is required.")]
        [MaxLength(20, ErrorMessage = "The unit type cannot exceed 20 characters.")]
        [Display(Name = "Unit Type", Description = "Type of unit (Base, Pack, Pallet, etc.).")]
        public string UnitType { get; set; } = "Base";

        /// <summary>
        /// Additional description for the unit.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The description cannot exceed 100 characters.")]
        [Display(Name = "Description", Description = "Additional description for the unit.")]
        public string? Description { get; set; }

        /// <summary>
        /// Status of the product unit.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the product unit.")]
        public ProductUnitStatus Status { get; set; } = ProductUnitStatus.Active;
    }
}
