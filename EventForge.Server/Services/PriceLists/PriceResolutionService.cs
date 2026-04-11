using EventForge.DTOs.PriceLists;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.PriceLists
{
    /// <summary>
    /// Service for resolving product prices based on price lists and business rules
    /// </summary>
        public class PriceResolutionService(
        EventForgeDbContext context,
        ILogger<PriceResolutionService> logger) : IPriceResolutionService
    {

        /// <inheritdoc/>
        public async Task<PriceResolutionResult> ResolvePriceAsync(
            Guid productId,
            Guid? documentHeaderId = null,
            Guid? businessPartyId = null,
            Guid? forcedPriceListId = null,
            PriceListDirection? direction = null,
            decimal quantity = 1m,
            Guid? unitOfMeasureId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try priority 1: Forced price list ID parameter
                if (forcedPriceListId.HasValue)
                {
                    var forcedResult = await TryGetPriceFromPriceListAsync(productId, forcedPriceListId.Value, quantity, unitOfMeasureId, cancellationToken);
                    if (forcedResult is not null)
                    {
                        forcedResult.Source = "ParameterList";
                        return forcedResult;
                    }
                }

                // Try priority 2: Document header price list
                if (documentHeaderId.HasValue)
                {
                    var documentHeader = await context.DocumentHeaders
                        .AsNoTracking()
                        .Include(d => d.PriceList)
                        .Include(d => d.DocumentType)
                        .FirstOrDefaultAsync(d => d.Id == documentHeaderId.Value, cancellationToken);

                    if (documentHeader?.PriceListId.HasValue == true)
                    {
                        var docResult = await TryGetPriceFromPriceListAsync(productId, documentHeader.PriceListId.Value, quantity, unitOfMeasureId, cancellationToken);
                        if (docResult is not null)
                        {
                            docResult.Source = "DocumentList";
                            return docResult;
                        }
                    }

                    // If direction is not provided, try to infer from document type
                    if (direction is null && documentHeader?.DocumentType is not null)
                    {
                        direction = documentHeader.DocumentType.IsStockIncrease
                            ? PriceListDirection.Input
                            : PriceListDirection.Output;
                    }
                }

                // Try priority 3: Business Party default price list
                if (businessPartyId.HasValue && direction.HasValue)
                {
                    var businessParty = await context.BusinessParties
                        .AsNoTracking()
                        .Include(bp => bp.DefaultSalesPriceList)
                        .Include(bp => bp.DefaultPurchasePriceList)
                        .FirstOrDefaultAsync(bp => bp.Id == businessPartyId.Value, cancellationToken);

                    if (businessParty is not null)
                    {
                        Guid? partyPriceListId = direction.Value == PriceListDirection.Output
                            ? businessParty.DefaultSalesPriceListId
                            : businessParty.DefaultPurchasePriceListId;

                        if (partyPriceListId.HasValue)
                        {
                            var partyResult = await TryGetPriceFromPriceListAsync(productId, partyPriceListId.Value, quantity, unitOfMeasureId, cancellationToken);
                            if (partyResult is not null)
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
                    var generalPriceList = await context.PriceLists
                        .AsNoTracking()
                        .Where(pl => pl.Direction == direction.Value
                            && pl.Status == Data.Entities.PriceList.PriceListStatus.Active
                            && (pl.ValidFrom == null || pl.ValidFrom <= DateTime.UtcNow)
                            && (pl.ValidTo == null || pl.ValidTo >= DateTime.UtcNow))
                        .OrderBy(pl => pl.Priority)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (generalPriceList is not null)
                    {
                        var generalResult = await TryGetPriceFromPriceListAsync(productId, generalPriceList.Id, quantity, unitOfMeasureId, cancellationToken);
                        if (generalResult is not null)
                        {
                            generalResult.Source = "GeneralList";
                            return generalResult;
                        }
                    }
                }

                // Fallback: Product default price
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

                if (product?.DefaultPrice is not null)
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
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<BatchPriceResolutionResponse> ResolvePricesBatchAsync(
            BatchPriceResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = new BatchPriceResolutionResponse
                {
                    TotalProcessed = request.Items.Count
                };

                var tasks = request.Items.Select(async item =>
                {
                    try
                    {
                        var result = await ResolvePriceAsync(
                            item.ProductId,
                            item.DocumentHeaderId,
                            item.BusinessPartyId,
                            item.ForcedPriceListId,
                            item.Direction,
                            item.Quantity,
                            item.UnitOfMeasureId,
                            cancellationToken);
                        return (item.Key, Result: result, Error: (BatchPriceResolutionError?)null);
                    }
                    catch (Exception ex)
                    {
                        var error = new BatchPriceResolutionError
                        {
                            Key = item.Key,
                            ProductId = item.ProductId,
                            ErrorMessage = ex.Message
                        };
                        return (item.Key, Result: (PriceResolutionResult?)null, Error: (BatchPriceResolutionError?)error);
                    }
                });

                var results = await Task.WhenAll(tasks);

                foreach (var (key, result, error) in results)
                {
                    if (error is not null)
                    {
                        response.Errors.Add(error);
                    }
                    else if (result is not null)
                    {
                        response.Results[key] = result;
                    }
                }

                response.TotalSucceeded = response.Results.Count;
                response.TotalFailed = response.Errors.Count;

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Tries to get price from a specific price list, filtering by quantity brackets (MinQuantity/MaxQuantity)
        /// and optionally by unit of measure (UoM).
        /// <para>
        /// UoM resolution strategy:
        /// <list type="bullet">
        ///   <item><description>When <paramref name="unitOfMeasureId"/> is specified: UoM-specific entries are tried first, then entries with no UoM constraint as fallback.</description></item>
        ///   <item><description>When <paramref name="unitOfMeasureId"/> is not specified: entries with no UoM constraint are tried first, then any entry as final fallback.</description></item>
        /// </list>
        /// </para>
        /// When multiple entries match, the most specific bracket (highest MinQuantity) is selected.
        /// The result includes the applied quantity bracket and UoM from the matched entry.
        /// </summary>
        private async Task<PriceResolutionResult?> TryGetPriceFromPriceListAsync(
            Guid productId,
            Guid priceListId,
            decimal quantity,
            Guid? unitOfMeasureId,
            CancellationToken cancellationToken)
        {
            // Build base query: match product, price list and quantity bracket
            var baseQuery = context.PriceListEntries
                .AsNoTracking()
                .Include(ple => ple.PriceList)
                .Where(ple => ple.PriceListId == priceListId && ple.ProductId == productId
                    && ple.MinQuantity <= quantity && (ple.MaxQuantity == 0 || ple.MaxQuantity >= quantity));

            PriceListEntry? priceListEntry = null;

            if (unitOfMeasureId.HasValue)
            {
                // Priority 1: entry matching the requested UoM
                priceListEntry = await baseQuery
                    .Where(ple => ple.UnitOfMeasureId == unitOfMeasureId.Value)
                    .OrderByDescending(ple => ple.MinQuantity)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Priority 2 (fallback when UoM specified but not found, or primary when no UoM requested):
            // entries with no UoM constraint
            priceListEntry ??= await baseQuery
                .Where(ple => ple.UnitOfMeasureId == null)
                .OrderByDescending(ple => ple.MinQuantity)
                .FirstOrDefaultAsync(cancellationToken);

            // Priority 3 (only when no UoM was requested): accept any UoM-specific entry
            if (priceListEntry is null && !unitOfMeasureId.HasValue)
            {
                priceListEntry = await baseQuery
                    .OrderByDescending(ple => ple.MinQuantity)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (priceListEntry is not null)
            {
                return new PriceResolutionResult
                {
                    Price = priceListEntry.Price,
                    AppliedPriceListId = priceListId,
                    PriceListName = priceListEntry.PriceList?.Name,
                    OriginalPrice = priceListEntry.Price,
                    IsPriceFromList = true,
                    Source = "Unknown", // Will be set by caller
                    AppliedUnitOfMeasureId = priceListEntry.UnitOfMeasureId,
                    AppliedMinQuantity = priceListEntry.MinQuantity,
                    AppliedMaxQuantity = priceListEntry.MaxQuantity
                };
            }

            return null;
        }
    
    }
}
