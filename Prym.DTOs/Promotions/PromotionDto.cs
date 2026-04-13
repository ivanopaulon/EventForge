namespace Prym.DTOs.Promotions
{

    /// <summary>
    /// DTO for Promotion output/display operations.
    /// </summary>
    public class PromotionDto
    {
        /// <summary>
        /// Unique identifier for the promotion.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the promotion.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the promotion.
        /// </summary>
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
        /// Number of times this promotion has been used.
        /// </summary>
        public int CurrentUses { get; set; }

        /// <summary>
        /// Remaining uses for this promotion. Null if there is no usage limit.
        /// </summary>
        public int? RemainingUses => MaxUses.HasValue ? MaxUses.Value - CurrentUses : null;

        /// <summary>
        /// Coupon code required to activate the promotion.
        /// </summary>
        public string? CouponCode { get; set; }

        /// <summary>
        /// Priority of the promotion.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Indicates if this promotion can be combined with others.
        /// </summary>
        public bool IsCombinable { get; set; }

        /// <summary>
        /// Maximum total discount percentage cap. Null = no cap.
        /// </summary>
        public decimal? MaxTotalDiscountPercentage { get; set; }

        /// <summary>
        /// Maximum uses per individual customer. Null = no per-customer limit.
        /// </summary>
        public int? MaxUsesPerCustomer { get; set; }

        /// <summary>
        /// Indicates if the promotion is currently active.
        /// </summary>
        public bool IsCurrentlyActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

        /// <summary>
        /// Date and time when the promotion was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the promotion.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the promotion was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the promotion.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Row version for optimistic concurrency. Returned by the server so clients can
        /// include it in update requests to detect concurrent modifications.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
