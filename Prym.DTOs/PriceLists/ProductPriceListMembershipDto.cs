namespace Prym.DTOs.PriceLists
{

    /// <summary>
    /// DTO summarizing a price list a given product belongs to, used to display the
    /// "in which price lists is this product?" indicator on the product detail page.
    /// </summary>
    public class ProductPriceListMembershipDto
    {
        /// <summary>
        /// Unique identifier of the price list.
        /// </summary>
        public Guid PriceListId { get; set; }

        /// <summary>
        /// Name of the price list.
        /// </summary>
        public string PriceListName { get; set; } = string.Empty;

        /// <summary>
        /// Product price in this price list.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Indicates whether the price list entry is currently active
        /// (i.e. its status is Active, as opposed to Suspended or Deleted).
        /// </summary>
        public bool IsActive { get; set; }
    }
}
