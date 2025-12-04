using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{

    /// <summary>
    /// DTO for adding an item to a sale session.
    /// </summary>
    public class AddSaleItemDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        [Required(ErrorMessage = "Product ID is required")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity to add.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
        public decimal Quantity { get; set; } = 1;

        /// <summary>
        /// Unit price (optional, will be fetched from product if not provided).
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Discount percentage to apply.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>
        /// Notes for this item.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Indicates if this is a service (not a product).
        /// </summary>
        public bool IsService { get; set; }
    }

    /// <summary>
    /// DTO for updating an item quantity.
    /// </summary>
    public class UpdateSaleItemDto
    {
        /// <summary>
        /// New quantity.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Unit price for this item.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount percentage.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>
        /// Notes for this item.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for a sale item.
    /// </summary>
    public class SaleItemDto
    {
        /// <summary>
        /// Item identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code.
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantity.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Discount percentage.
        /// </summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// Total amount for this line.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Tax rate.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Tax amount.
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// Notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Is service flag.
        /// </summary>
        public bool IsService { get; set; }

        /// <summary>
        /// Applied promotion identifier.
        /// </summary>
        public Guid? PromotionId { get; set; }

        /// <summary>
        /// Thumbnail URL for product display in table.
        /// </summary>
        public string? ProductThumbnailUrl { get; set; }

        /// <summary>
        /// Full image URL for product preview.
        /// </summary>
        public string? ProductImageUrl { get; set; }

        /// <summary>
        /// VAT rate name (e.g., "22%", "10%", "4%").
        /// </summary>
        public string? VatRateName { get; set; }

        /// <summary>
        /// VAT rate identifier.
        /// </summary>
        public Guid? VatRateId { get; set; }

        /// <summary>
        /// Unit of measure name (e.g., "PZ", "KG").
        /// </summary>
        public string? UnitOfMeasureName { get; set; }

        /// <summary>
        /// Brand name.
        /// </summary>
        public string? BrandName { get; set; }
    }
}
