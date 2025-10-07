using System;

namespace EventForge.DTOs.PriceLists
{

    /// <summary>
    /// DTO for exporting price list entries in a structured format for bulk operations.
    /// Part of Issue #245 price optimization implementation.
    /// </summary>
    public class ExportablePriceListEntryDto
    {
        /// <summary>
        /// Price list entry identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name for reference.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Product code for reference.
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product SKU for reference.
        /// </summary>
        public string? ProductSku { get; set; }

        /// <summary>
        /// Price list identifier.
        /// </summary>
        public Guid PriceListId { get; set; }

        /// <summary>
        /// Price per unit.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Score assigned to the product.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Whether the price is editable in the frontend.
        /// </summary>
        public bool IsEditableInFrontend { get; set; }

        /// <summary>
        /// Whether the item can be discounted.
        /// </summary>
        public bool IsDiscountable { get; set; } = true;

        /// <summary>
        /// Status of the price list entry.
        /// </summary>
        public string Status { get; set; } = "Attivo";

        /// <summary>
        /// Minimum quantity to apply this price.
        /// </summary>
        public int MinQuantity { get; set; } = 1;

        /// <summary>
        /// Maximum quantity to apply this price (0 = no limit).
        /// </summary>
        public int MaxQuantity { get; set; } = 0;

        /// <summary>
        /// Notes for the price list entry.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date when the entry was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the entry.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date when the entry was last modified.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the entry.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Whether the entry is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Category of the product for grouping during export.
        /// </summary>
        public string? ProductCategory { get; set; }

        /// <summary>
        /// Unit of measure symbol for the product.
        /// </summary>
        public string? UnitOfMeasure { get; set; }

        /// <summary>
        /// Default price of the product for comparison.
        /// </summary>
        public decimal? ProductDefaultPrice { get; set; }

        /// <summary>
        /// Difference between this price and the product's default price.
        /// </summary>
        public decimal? PriceDifference => ProductDefaultPrice.HasValue ? (decimal?)(Price - ProductDefaultPrice.Value) : null;

        /// <summary>
        /// Percentage difference from the product's default price.
        /// </summary>
        public decimal? PriceDifferencePercentage => ProductDefaultPrice.HasValue && ProductDefaultPrice.Value != 0
            ? (decimal?)Math.Round((Price - ProductDefaultPrice.Value) / ProductDefaultPrice.Value * 100, 2)
            : null;
    }
}