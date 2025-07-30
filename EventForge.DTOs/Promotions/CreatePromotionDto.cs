using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Promotions
{

    /// <summary>
    /// DTO for creating a new promotion.
    /// </summary>
    public class CreatePromotionDto
    {
        /// <summary>
        /// Name of the promotion.
        /// </summary>
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the promotion.
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Start date of the promotion.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the promotion.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Minimum order amount to activate the promotion.
        /// </summary>
        public decimal? MinOrderAmount { get; set; }

        /// <summary>
        /// Maximum number of times this promotion can be used.
        /// </summary>
        public int? MaxUses { get; set; }

        /// <summary>
        /// Coupon code required to activate the promotion.
        /// </summary>
        [StringLength(50, ErrorMessage = "Coupon code cannot exceed 50 characters.")]
        public string? CouponCode { get; set; }

        /// <summary>
        /// Priority of the promotion.
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Indicates if this promotion can be combined with others.
        /// </summary>
        public bool IsCombinable { get; set; } = true;
    }
}
