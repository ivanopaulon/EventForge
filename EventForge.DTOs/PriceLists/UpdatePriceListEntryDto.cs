using EventForge.DTOs.Common;
using System;
using System.ComponentModel.DataAnnotations;
namespace EventForge.DTOs.PriceLists
{

    /// <summary>
    /// DTO for PriceListEntry update operations.
    /// </summary>
    public class UpdatePriceListEntryDto
    {
        /// <summary>
        /// Product price for the price list.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "The price must be greater than or equal to zero.")]
        [Display(Name = "Price", Description = "Product price for the price list.")]
        public decimal Price { get; set; }

        /// <summary>
        /// Currency code (e.g., EUR, USD).
        /// </summary>
        [MaxLength(3, ErrorMessage = "The currency code cannot exceed 3 characters.")]
        [Display(Name = "Currency", Description = "Currency code (ISO 4217).")]
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Score assigned to the product.
        /// </summary>
        [Range(0, 100, ErrorMessage = "The score must be between 0 and 100.")]
        [Display(Name = "Score", Description = "Score assigned to the product.")]
        public int Score { get; set; }

        /// <summary>
        /// Indicates if the price is editable from the frontend.
        /// </summary>
        [Display(Name = "Editable in Frontend", Description = "Indicates if the price is editable from the frontend.")]
        public bool IsEditableInFrontend { get; set; }

        /// <summary>
        /// Indicates if the item can be discounted.
        /// </summary>
        [Display(Name = "Discountable", Description = "Indicates if the item can be discounted.")]
        public bool IsDiscountable { get; set; }

        /// <summary>
        /// Status of the price list entry.
        /// </summary>
        [Required]
        [Display(Name = "Status", Description = "Current status of the price list entry.")]
        public PriceListEntryStatus Status { get; set; }

        /// <summary>
        /// Minimum quantity to apply this price.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "The minimum quantity must be at least 1.")]
        [Display(Name = "Minimum Quantity", Description = "Minimum quantity to apply this price.")]
        public int MinQuantity { get; set; }

        /// <summary>
        /// Maximum quantity to apply this price (0 = no limit).
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "The maximum quantity must be greater than or equal to zero.")]
        [Display(Name = "Maximum Quantity", Description = "Maximum quantity to apply this price (0 = no limit).")]
        public int MaxQuantity { get; set; }

        /// <summary>
        /// Additional notes for the price list entry.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
        [Display(Name = "Notes", Description = "Additional notes for the price list entry.")]
        public string? Notes { get; set; }
    }
}
