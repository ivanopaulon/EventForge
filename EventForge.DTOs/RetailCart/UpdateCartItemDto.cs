using System;
using System.Collections.Generic;

namespace EventForge.DTOs.RetailCart
{
    /// <summary>
    /// DTO for updating a cart item quantity.
    /// </summary>
    public class UpdateCartItemDto
    {
        /// <summary>
        /// New quantity.
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// DTO for applying coupons to a cart session.
    /// </summary>
    public class ApplyCouponsDto
    {
        /// <summary>
        /// Coupon codes to apply.
        /// </summary>
        public List<string> CouponCodes { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for adding an item to a cart session.
    /// </summary>
    public class AddCartItemDto
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
        /// Unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantity to add.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Product category IDs.
        /// </summary>
        public List<Guid>? CategoryIds { get; set; }
    }
}