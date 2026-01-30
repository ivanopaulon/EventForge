using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side implementation for resolving product prices based on price lists.
/// </summary>
public class PriceResolutionService : IPriceResolutionService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PriceResolutionService> _logger;
    private const string BaseUrl = "api/v1/PriceLists/resolve-price";

    public PriceResolutionService(
        IHttpClientService httpClientService,
        ILogger<PriceResolutionService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PriceResolutionResult> ResolvePriceAsync(
        Guid productId,
        Guid? documentHeaderId = null,
        Guid? businessPartyId = null,
        Guid? forcedPriceListId = null,
        PriceListDirection? direction = null)
    {
        try
        {
            var queryString = BuildQueryString(productId, documentHeaderId, businessPartyId, forcedPriceListId, direction);
            var result = await _httpClientService.GetAsync<PriceResolutionResult>($"{BaseUrl}?{queryString}");

            return result ?? new PriceResolutionResult
            {
                Price = 0m,
                IsPriceFromList = false,
                Source = "DefaultPrice"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving price for product {ProductId}", productId);

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
        PriceListDirection? direction)
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

        return query;
    }
}
