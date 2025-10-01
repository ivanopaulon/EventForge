using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.Products
{

    /// <summary>
    /// DTO for Product creation operations.
    /// </summary>
    public class CreateProductDto
    {
        /// <summary>
        /// Product name.
        /// </summary>
        [Required(ErrorMessage = "The product name is required.")]
        [MaxLength(100, ErrorMessage = "The product name cannot exceed 100 characters.")]
        [Display(Name = "Name", Description = "Product name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short product description.
        /// </summary>
        [MaxLength(50, ErrorMessage = "The short description cannot exceed 50 characters.")]
        [Display(Name = "Short Description", Description = "Short product description.")]
        public string ShortDescription { get; set; } = string.Empty;

        /// <summary>
        /// Detailed product description.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
        [Display(Name = "Description", Description = "Detailed product description.")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Product code (SKU or similar).
        /// </summary>
        [Display(Name = "Code", Description = "Product code (SKU or similar).")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Product image URL.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The image URL cannot exceed 500 characters.")]
        [Display(Name = "Image", Description = "Product image URL.")]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Product status.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the product.")]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

        /// <summary>
        /// Indicates if the price includes VAT.
        /// </summary>
        [Display(Name = "VAT Included", Description = "Indicates if the price includes VAT.")]
        public bool IsVatIncluded { get; set; } = false;

        /// <summary>
        /// Default product price.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive.")]
        [Display(Name = "Default Price", Description = "Default product price.")]
        public decimal? DefaultPrice { get; set; }

        /// <summary>
        /// VAT rate identifier.
        /// </summary>
        [Display(Name = "VAT Rate", Description = "Identifier of the VAT rate.")]
        public Guid? VatRateId { get; set; }

        /// <summary>
        /// Unit of measure identifier.
        /// </summary>
        [Display(Name = "Unit of Measure", Description = "Identifier of the unit of measure.")]
        public Guid? UnitOfMeasureId { get; set; }

        /// <summary>
        /// Main category node identifier.
        /// </summary>
        [Display(Name = "Category", Description = "Identifier of the main category.")]
        public Guid? CategoryNodeId { get; set; }

        /// <summary>
        /// Family node identifier.
        /// </summary>
        [Display(Name = "Family", Description = "Identifier of the family.")]
        public Guid? FamilyNodeId { get; set; }

        /// <summary>
        /// Statistical group node identifier.
        /// </summary>
        [Display(Name = "Statistical Group", Description = "Identifier of the statistical group.")]
        public Guid? GroupNodeId { get; set; }

        /// <summary>
        /// Station identifier.
        /// </summary>
        [Display(Name = "Station", Description = "Identifier of the station.")]
        public Guid? StationId { get; set; }

        /// <summary>
        /// Indicates if the product is a bundle.
        /// </summary>
        [Display(Name = "Is Bundle", Description = "Indicates if the product is a bundle.")]
        public bool IsBundle { get; set; } = false;

        /// <summary>
        /// Brand identifier.
        /// </summary>
        [Display(Name = "Brand", Description = "Identifier of the brand.")]
        public Guid? BrandId { get; set; }

        /// <summary>
        /// Model identifier.
        /// </summary>
        [Display(Name = "Model", Description = "Identifier of the model.")]
        public Guid? ModelId { get; set; }

        /// <summary>
        /// Preferred supplier identifier.
        /// </summary>
        [Display(Name = "Preferred Supplier", Description = "Identifier of the preferred supplier.")]
        public Guid? PreferredSupplierId { get; set; }

        /// <summary>
        /// Reorder point - inventory level at which to reorder.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Reorder point must be non-negative.")]
        [Display(Name = "Reorder Point", Description = "Inventory level at which to reorder.")]
        public decimal? ReorderPoint { get; set; }

        /// <summary>
        /// Safety stock level - minimum stock to maintain.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Safety stock must be non-negative.")]
        [Display(Name = "Safety Stock", Description = "Minimum stock to maintain.")]
        public decimal? SafetyStock { get; set; }

        /// <summary>
        /// Target stock level - desired inventory level.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Target stock level must be non-negative.")]
        [Display(Name = "Target Stock Level", Description = "Desired inventory level.")]
        public decimal? TargetStockLevel { get; set; }

        /// <summary>
        /// Average daily demand for inventory planning.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Average daily demand must be non-negative.")]
        [Display(Name = "Average Daily Demand", Description = "Average daily demand for inventory planning.")]
        public decimal? AverageDailyDemand { get; set; }
    }
}
