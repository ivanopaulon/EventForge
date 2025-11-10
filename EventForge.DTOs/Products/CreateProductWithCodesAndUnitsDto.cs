using EventForge.DTOs.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for creating a product with multiple codes and units of measure in a single transaction.
    /// Used for quick product creation during inventory procedures.
    /// </summary>
    public class CreateProductWithCodesAndUnitsDto
    {
        /// <summary>
        /// Product name.
        /// </summary>
        [Required(ErrorMessage = "The product name is required.")]
        [MaxLength(100, ErrorMessage = "The product name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short product description.
        /// </summary>
        [MaxLength(50, ErrorMessage = "The short description cannot exceed 50 characters.")]
        public string ShortDescription { get; set; } = string.Empty;

        /// <summary>
        /// Detailed product description.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Product code (SKU or similar).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Product status.
        /// </summary>
        [Required]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        /// <summary>
        /// Indicates if the price includes VAT.
        /// </summary>
        public bool IsVatIncluded { get; set; } = true;

        /// <summary>
        /// Default product price.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal? DefaultPrice { get; set; }

        /// <summary>
        /// VAT rate identifier.
        /// </summary>
        public Guid? VatRateId { get; set; }

        /// <summary>
        /// Base unit of measure identifier.
        /// </summary>
        public Guid? UnitOfMeasureId { get; set; }

        /// <summary>
        /// Main category node identifier.
        /// </summary>
        public Guid? CategoryNodeId { get; set; }

        /// <summary>
        /// Family node identifier.
        /// </summary>
        public Guid? FamilyNodeId { get; set; }

        /// <summary>
        /// Statistical group node identifier.
        /// </summary>
        public Guid? GroupNodeId { get; set; }

        /// <summary>
        /// Station identifier.
        /// </summary>
        public Guid? StationId { get; set; }

        /// <summary>
        /// Brand identifier.
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// Model identifier.
        /// </summary>
        public Guid? ModelId { get; set; }

        /// <summary>
        /// List of product codes with associated units to create.
        /// </summary>
        public List<ProductCodeWithUnitDto> CodesWithUnits { get; set; } = new List<ProductCodeWithUnitDto>();
    }
}
