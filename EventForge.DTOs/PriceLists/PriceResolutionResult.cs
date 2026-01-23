using System;

namespace EventForge.DTOs.PriceLists
{
    /// <summary>
    /// Result of price resolution for a product
    /// </summary>
    public class PriceResolutionResult
    {
        /// <summary>
        /// Resolved price for the product
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// ID of the price list that was applied (if any)
        /// </summary>
        public Guid? AppliedPriceListId { get; set; }

        /// <summary>
        /// Name of the price list that was applied
        /// </summary>
        public string? PriceListName { get; set; }

        /// <summary>
        /// Original price from the price list before any adjustments
        /// </summary>
        public decimal? OriginalPrice { get; set; }

        /// <summary>
        /// Indicates if the price came from a price list (true) or default product price (false)
        /// </summary>
        public bool IsPriceFromList { get; set; }

        /// <summary>
        /// Source of the price: "ForcedList", "PartyList", "GeneralList", "DefaultPrice"
        /// </summary>
        public string Source { get; set; } = "DefaultPrice";
    }
}
