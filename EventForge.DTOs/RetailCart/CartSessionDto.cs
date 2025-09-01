using System;
using System.Collections.Generic;
using EventForge.DTOs.Promotions;

namespace EventForge.DTOs.RetailCart
{
    /// <summary>
    /// DTO for a retail cart session.
    /// </summary>
    public class CartSessionDto
    {
        /// <summary>
        /// Session ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Customer ID (optional).
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Sales channel.
        /// </summary>
        public string? SalesChannel { get; set; }

        /// <summary>
        /// Currency code.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Cart items.
        /// </summary>
        public List<CartSessionItemDto> Items { get; set; } = new List<CartSessionItemDto>();

        /// <summary>
        /// Applied coupon codes.
        /// </summary>
        public List<string> CouponCodes { get; set; } = new List<string>();

        /// <summary>
        /// Original total before promotions.
        /// </summary>
        public decimal OriginalTotal { get; set; }

        /// <summary>
        /// Final total after promotions.
        /// </summary>
        public decimal FinalTotal { get; set; }

        /// <summary>
        /// Total discount amount.
        /// </summary>
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Applied promotions.
        /// </summary>
        public List<AppliedPromotionDto> AppliedPromotions { get; set; } = new List<AppliedPromotionDto>();

        /// <summary>
        /// Session creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}