using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.Server.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Services.PriceLists
{
    /// <summary>
    /// Service for resolving product prices based on price lists and business rules
    /// </summary>
    public class PriceResolutionService : IPriceResolutionService
    {
        private readonly EventForgeDbContext _context;

        public PriceResolutionService(EventForgeDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<PriceResolutionResult> ResolvePriceAsync(
            Guid productId,
            Guid? documentHeaderId = null,
            Guid? businessPartyId = null,
            Guid? forcedPriceListId = null,
            PriceListDirection? direction = null,
            CancellationToken cancellationToken = default)
        {
            // Try priority 1: Forced price list ID parameter
            if (forcedPriceListId.HasValue)
            {
                var forcedResult = await TryGetPriceFromPriceListAsync(productId, forcedPriceListId.Value, cancellationToken);
                if (forcedResult != null)
                {
                    forcedResult.Source = "ParameterList";
                    return forcedResult;
                }
            }

            // Try priority 2: Document header price list
            if (documentHeaderId.HasValue)
            {
                var documentHeader = await _context.DocumentHeaders
                    .Include(d => d.PriceList)
                    .Include(d => d.DocumentType)
                    .FirstOrDefaultAsync(d => d.Id == documentHeaderId.Value, cancellationToken);

                if (documentHeader?.PriceListId.HasValue == true)
                {
                    var docResult = await TryGetPriceFromPriceListAsync(productId, documentHeader.PriceListId.Value, cancellationToken);
                    if (docResult != null)
                    {
                        docResult.Source = "DocumentList";
                        return docResult;
                    }
                }

                // If direction is not provided, try to infer from document type
                if (direction == null && documentHeader?.DocumentType != null)
                {
                    direction = documentHeader.DocumentType.IsStockIncrease 
                        ? PriceListDirection.Input 
                        : PriceListDirection.Output;
                }
            }

            // Try priority 3: Business Party default price list
            if (businessPartyId.HasValue && direction.HasValue)
            {
                var businessParty = await _context.BusinessParties
                    .Include(bp => bp.DefaultSalesPriceList)
                    .Include(bp => bp.DefaultPurchasePriceList)
                    .FirstOrDefaultAsync(bp => bp.Id == businessPartyId.Value, cancellationToken);

                if (businessParty != null)
                {
                    Guid? partyPriceListId = direction.Value == PriceListDirection.Output
                        ? businessParty.DefaultSalesPriceListId
                        : businessParty.DefaultPurchasePriceListId;

                    if (partyPriceListId.HasValue)
                    {
                        var partyResult = await TryGetPriceFromPriceListAsync(productId, partyPriceListId.Value, cancellationToken);
                        if (partyResult != null)
                        {
                            partyResult.Source = "PartyList";
                            return partyResult;
                        }
                    }
                }
            }

            // Try priority 4: General active price list for the direction
            if (direction.HasValue)
            {
                var generalPriceList = await _context.PriceLists
                    .Where(pl => pl.Direction == direction.Value
                        && pl.Status == Data.Entities.PriceList.PriceListStatus.Active
                        && (pl.ValidFrom == null || pl.ValidFrom <= DateTime.UtcNow)
                        && (pl.ValidTo == null || pl.ValidTo >= DateTime.UtcNow))
                    .OrderBy(pl => pl.Priority)
                    .FirstOrDefaultAsync(cancellationToken);

                if (generalPriceList != null)
                {
                    var generalResult = await TryGetPriceFromPriceListAsync(productId, generalPriceList.Id, cancellationToken);
                    if (generalResult != null)
                    {
                        generalResult.Source = "GeneralList";
                        return generalResult;
                    }
                }
            }

            // Fallback: Product default price
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product?.DefaultPrice != null)
            {
                return new PriceResolutionResult
                {
                    Price = product.DefaultPrice.Value,
                    AppliedPriceListId = null,
                    PriceListName = null,
                    OriginalPrice = product.DefaultPrice.Value,
                    IsPriceFromList = false,
                    Source = "DefaultPrice"
                };
            }

            // Ultimate fallback: 0
            return new PriceResolutionResult
            {
                Price = 0m,
                AppliedPriceListId = null,
                PriceListName = null,
                OriginalPrice = null,
                IsPriceFromList = false,
                Source = "DefaultPrice"
            };
        }

        /// <summary>
        /// Tries to get price from a specific price list
        /// </summary>
        private async Task<PriceResolutionResult?> TryGetPriceFromPriceListAsync(
            Guid productId,
            Guid priceListId,
            CancellationToken cancellationToken)
        {
            var priceListEntry = await _context.PriceListEntries
                .Include(ple => ple.PriceList)
                .FirstOrDefaultAsync(ple => ple.PriceListId == priceListId && ple.ProductId == productId, cancellationToken);

            if (priceListEntry != null)
            {
                return new PriceResolutionResult
                {
                    Price = priceListEntry.Price,
                    AppliedPriceListId = priceListId,
                    PriceListName = priceListEntry.PriceList?.Name,
                    OriginalPrice = priceListEntry.Price,
                    IsPriceFromList = true,
                    Source = "Unknown" // Will be set by caller
                };
            }

            return null;
        }
    }
}
