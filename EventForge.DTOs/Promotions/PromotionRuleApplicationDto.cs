using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.Promotions
{
    
    /// <summary>
    /// DTO for applying promotion rules to a cart or order.
    /// </summary>
    public class ApplyPromotionRulesDto
    {
        /// <summary>
        /// Collection of cart items to apply promotions to.
        /// </summary>
        public List<CartItemDto> CartItems { get; set; } = new();
    
        /// <summary>
        /// Customer ID (optional, for customer-specific promotions).
        /// </summary>
        public Guid? CustomerId { get; set; }
    
        /// <summary>
        /// Sales channel (optional, for channel-specific promotions).
        /// </summary>
        public string? SalesChannel { get; set; }
    
        /// <summary>
        /// Coupon codes to apply (optional).
        /// </summary>
        public List<string>? CouponCodes { get; set; }
    
        /// <summary>
        /// Order date/time for date-based rules.
        /// </summary>
        public DateTime OrderDateTime { get; set; } = DateTime.UtcNow;
    
        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";
    }
    
    /// <summary>
    /// DTO representing a cart item for promotion rule application.
    /// </summary>
    public class CartItemDto
    {
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
        /// Any existing line discount percentage.
        /// </summary>
        public decimal ExistingLineDiscount { get; set; } = 0m;
    }
    
    /// <summary>
    /// DTO for the result of applying promotion rules.
    /// </summary>
    public class PromotionApplicationResultDto
    {
        /// <summary>
        /// Original order total before promotions.
        /// </summary>
        public decimal OriginalTotal { get; set; }
    
        /// <summary>
        /// Final order total after all promotions.
        /// </summary>
        public decimal FinalTotal { get; set; }
    
        /// <summary>
        /// Total discount amount applied.
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }
    
        /// <summary>
        /// Collection of applied promotions with details.
        /// </summary>
        public List<AppliedPromotionDto> AppliedPromotions { get; set; } = new();
    
        /// <summary>
        /// Updated cart items with applied discounts.
        /// </summary>
        public List<CartItemResultDto> CartItems { get; set; } = new();
    
        /// <summary>
        /// Warnings or messages about promotion application.
        /// </summary>
        public List<string> Messages { get; set; } = new();
    
        /// <summary>
        /// Whether all promotions were successfully applied.
        /// </summary>
        public bool Success { get; set; } = true;
    }
    
    /// <summary>
    /// DTO for an applied promotion with details.
    /// </summary>
    public class AppliedPromotionDto
    {
        /// <summary>
        /// Promotion ID.
        /// </summary>
        public Guid PromotionId { get; set; }
    
        /// <summary>
        /// Promotion name.
        /// </summary>
        public string PromotionName { get; set; } = string.Empty;
    
        /// <summary>
        /// Promotion rule ID that was applied.
        /// </summary>
        public Guid PromotionRuleId { get; set; }
    
        /// <summary>
        /// Type of rule that was applied.
        /// </summary>
        public PromotionRuleType RuleType { get; set; }
    
        /// <summary>
        /// Discount amount applied by this promotion.
        /// </summary>
        public decimal DiscountAmount { get; set; }
    
        /// <summary>
        /// Percentage discount applied (if applicable).
        /// </summary>
        public decimal? DiscountPercentage { get; set; }
    
        /// <summary>
        /// Description of what was applied.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    
        /// <summary>
        /// Product IDs affected by this promotion.
        /// </summary>
        public List<Guid> AffectedProductIds { get; set; } = new();
    }
    
    /// <summary>
    /// DTO for cart item result after promotion application.
    /// </summary>
    public class CartItemResultDto : CartItemDto
    {
        /// <summary>
        /// Line total before promotions.
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
        public List<AppliedPromotionDto> AppliedPromotions { get; set; } = new();
    }
}
