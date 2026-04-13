using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Promotions
{
    /// <summary>
    /// Request DTO for validating a coupon code.
    /// </summary>
    public class ValidateCouponRequestDto
    {
        /// <summary>
        /// The coupon code to validate.
        /// </summary>
        [Required(ErrorMessage = "The coupon code is required.")]
        [StringLength(50, ErrorMessage = "The coupon code cannot exceed 50 characters.")]
        public string CouponCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional customer ID for customer-specific promotion validation.
        /// </summary>
        public Guid? CustomerId { get; set; }
    }
}
