using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service for resolving product prices based on price lists.
/// </summary>
public interface IPriceResolutionService
{
    /// <summary>
    /// Resolves the price for a product based on the cascading priority:
    /// 1. Forced price list parameter (forcedPriceListId - highest priority)
    /// 2. Forced price list in document (DocumentHeader.PriceListId)
    /// 3. Default price list from Business Party (Customer/Supplier based on direction)
    /// 4. General active price list (first valid for direction Sales/Purchase)
    /// 5. Fallback: Product.DefaultPrice
    /// </summary>
    /// <param name="productId">ID of the product</param>
    /// <param name="documentHeaderId">Optional document header ID (to get forced price list)</param>
    /// <param name="businessPartyId">Optional business party ID (to get default price list)</param>
    /// <param name="forcedPriceListId">Optional forced price list ID (overrides all)</param>
    /// <param name="direction">Price list direction (Input for purchase, Output for sales)</param>
    /// <returns>Price resolution result with price and metadata</returns>
    Task<PriceResolutionResult> ResolvePriceAsync(
        Guid productId,
        Guid? documentHeaderId = null,
        Guid? businessPartyId = null,
        Guid? forcedPriceListId = null,
        PriceListDirection? direction = null);
}
