using EventForge.DTOs.Promotions;

namespace EventForge.DTOs.RetailCart
{
    /// <summary>
    /// DTO for a cart session item.
    /// </summary>
    public class CartSessionItemDto
    {
        /// <summary>
        /// Item ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product ID.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code/SKU.
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Unit price before any discounts.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product category IDs.
        /// </summary>
        public List<Guid>? CategoryIds { get; set; }

        /// <summary>
        /// Original line total before promotions.
        /// </summary>
        public decimal OriginalLineTotal { get; set; }

        /// <summary>
        /// Final line total after promotions.
        /// </summary>
        public decimal FinalLineTotal { get; set; }

        /// <summary>
        /// Additional discount applied by promotions.
        /// </summary>
        public decimal PromotionDiscount { get; set; }

        /// <summary>
        /// Final effective discount percentage.
        /// </summary>
        public decimal EffectiveDiscountPercentage { get; set; }

        /// <summary>
        /// Promotions applied to this line item.
        /// </summary>
        public List<AppliedPromotionDto> AppliedPromotions { get; set; } = new List<AppliedPromotionDto>();
    }
}