using Prym.DTOs.Common;
using Prym.DTOs.PriceLists;

namespace Prym.Web.Services;

/// <summary>
/// Client-side implementation for resolving product prices based on price lists.
/// </summary>
public class PriceResolutionService(
    IHttpClientService httpClientService,
    ILogger<PriceResolutionService> logger) : IPriceResolutionService
{
    private const string BaseUrl = "api/v1/pricelists/resolve-price";

    public async Task<PriceResolutionResult> ResolvePriceAsync(
        Guid productId,
        Guid? documentHeaderId = null,
        Guid? businessPartyId = null,
        Guid? forcedPriceListId = null,
        PriceListDirection? direction = null,
        decimal quantity = 1m,
        Guid? unitOfMeasureId = null,
        CancellationToken ct = default)
    {
        try
        {
            var queryString = BuildQueryString(productId, documentHeaderId, businessPartyId, forcedPriceListId, direction, quantity, unitOfMeasureId);
            var result = await httpClientService.GetAsync<PriceResolutionResult>($"{BaseUrl}?{queryString}", ct);

            return result ?? new PriceResolutionResult
            {
                Price = 0m,
                IsPriceFromList = false,
                Source = "DefaultPrice"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving price for product {ProductId}", productId);

            // Return fallback result instead of throwing to prevent UI disruption
            return new PriceResolutionResult
            {
                Price = 0m,
                IsPriceFromList = false,
                Source = "Error"
            };
        }
    }

    private static string BuildQueryString(
        Guid productId,
        Guid? documentHeaderId,
        Guid? businessPartyId,
        Guid? forcedPriceListId,
        PriceListDirection? direction,
        decimal quantity,
        Guid? unitOfMeasureId)
    {
        var query = $"productId={productId}";

        if (documentHeaderId.HasValue)
            query += $"&documentHeaderId={documentHeaderId.Value}";

        if (businessPartyId.HasValue)
            query += $"&businessPartyId={businessPartyId.Value}";

        if (forcedPriceListId.HasValue)
            query += $"&forcedPriceListId={forcedPriceListId.Value}";

        if (direction.HasValue)
            query += $"&direction={direction.Value}";

        if (quantity != 1m)
            query += $"&quantity={quantity}";

        if (unitOfMeasureId.HasValue)
            query += $"&unitOfMeasureId={unitOfMeasureId.Value}";

        return query;
    }

    public async Task<BatchPriceResolutionResponse?> ResolvePricesBatchAsync(BatchPriceResolutionRequest request, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<BatchPriceResolutionRequest, BatchPriceResolutionResponse>(
                "api/v1/pricelists/resolve-prices", request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving batch prices for {Count} items", request.Items.Count);
            return null;
        }
    }
}
