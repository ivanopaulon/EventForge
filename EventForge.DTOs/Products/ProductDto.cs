using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Products
{

    /// <summary>
    /// DTO for Product output/display operations.
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// Unique identifier for the product.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short product description.
        /// </summary>
        public string ShortDescription { get; set; } = string.Empty;

        /// <summary>
        /// Detailed product description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Product code (SKU or similar).
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Product image URL (deprecated - use ImageDocumentId).
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Image document identifier (references DocumentReference).
        /// </summary>
        public Guid? ImageDocumentId { get; set; }

        /// <summary>
        /// Thumbnail URL for the product image (from ImageDocument if available).
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Product status.
        /// </summary>
        public ProductStatus Status { get; set; }

        /// <summary>
        /// Indicates if the price includes VAT.
        /// </summary>
        public bool IsVatIncluded { get; set; }

        /// <summary>
        /// Default product price.
        /// </summary>
        public decimal? DefaultPrice { get; set; }

        /// <summary>
        /// VAT rate identifier.
        /// </summary>
        public Guid? VatRateId { get; set; }

        /// <summary>
        /// Unit of measure identifier.
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
        /// Indicates if the product is a bundle.
        /// </summary>
        public bool IsBundle { get; set; }

        /// <summary>
        /// Number of product codes associated with the product.
        /// </summary>
        public int CodeCount { get; set; }

        /// <summary>
        /// Number of units associated with the product.
        /// </summary>
        public int UnitCount { get; set; }

        /// <summary>
        /// Number of bundle items if the product is a bundle.
        /// </summary>
        public int BundleItemCount { get; set; }

        /// <summary>
        /// Date and time when the product was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the product.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the product was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the product.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
