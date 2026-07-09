namespace Prym.DTOs.Promotions
{

    /// <summary>
    /// DTO summarizing a promotion a given product belongs to, used to display the
    /// "in which promotions is this product?" indicator on the product detail page.
    /// </summary>
    public class ProductPromotionMembershipDto
    {
        /// <summary>
        /// Unique identifier of the promotion.
        /// </summary>
        public Guid PromotionId { get; set; }

        /// <summary>
        /// Name of the promotion.
        /// </summary>
        public string PromotionName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the promotion is currently active, i.e. its Status is Active
        /// AND the current date/time falls between StartDate and EndDate.
        /// A Suspended promotion is never considered active, regardless of dates.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Start date of the promotion.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the promotion.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Indicates whether the product is included in this promotion via a rule that targets
        /// all products (i.e. a rule with no explicit product targeting - an empty
        /// <c>PromotionRuleProduct</c> list), rather than via an explicit product-targeting rule.
        /// Included so the UI can distinguish "explicitly targeted" from "generic/all-products" discounts.
        /// </summary>
        public bool AppliesToAllProducts { get; set; }
    }
}
