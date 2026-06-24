using Prym.DTOs.Common;
using Prym.DTOs.PriceHistory;

namespace Prym.Web.Services;

public interface ISupplierPriceHistoryService
{
    Task<PriceHistoryResponse?> GetPriceHistoryAsync(Guid supplierId, Guid productId, PriceHistoryRequest request, CancellationToken ct = default);
    Task<PriceHistoryResponse?> GetSupplierPriceHistoryAsync(Guid supplierId, PriceHistoryRequest request, CancellationToken ct = default);
    Task<PriceHistoryResponse?> GetProductAllSuppliersPriceHistoryAsync(Guid productId, PriceHistoryRequest request, CancellationToken ct = default);
    Task<PriceHistoryStatistics?> GetStatisticsAsync(Guid supplierId, Guid? productId = null, CancellationToken ct = default);
    Task<List<PriceTrendDataPoint>> GetTrendDataAsync(Guid supplierId, Guid productId, DateTime fromDate, DateTime toDate, CancellationToken ct = default);
}

public class SupplierPriceHistoryService(
    IHttpClientService httpClientService,
    ILogger<SupplierPriceHistoryService> logger) : ISupplierPriceHistoryService
{
    private const string BaseUrl = "api/v1/price-history";

    public async Task<PriceHistoryResponse?> GetPriceHistoryAsync(
        Guid supplierId,
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var queryString = BuildQueryString(request);
            return await httpClientService.GetAsync<PriceHistoryResponse>(
                $"{BaseUrl}/suppliers/{supplierId}/products/{productId}?{queryString}",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving price history for supplier {SupplierId} and product {ProductId}", supplierId, productId);
            throw;
        }
    }

    public async Task<PriceHistoryResponse?> GetSupplierPriceHistoryAsync(
        Guid supplierId,
        PriceHistoryRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var queryString = BuildQueryString(request);
            return await httpClientService.GetAsync<PriceHistoryResponse>(
                $"{BaseUrl}/suppliers/{supplierId}?{queryString}",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving price history for supplier {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<PriceHistoryResponse?> GetProductAllSuppliersPriceHistoryAsync(
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var queryString = BuildQueryString(request);
            return await httpClientService.GetAsync<PriceHistoryResponse>(
                $"{BaseUrl}/products/{productId}/all-suppliers?{queryString}",
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving price history across suppliers for product {ProductId}", productId);
            throw;
        }
    }

    public async Task<PriceHistoryStatistics?> GetStatisticsAsync(
        Guid supplierId,
        Guid? productId = null,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/suppliers/{supplierId}/statistics";
            if (productId.HasValue)
            {
                url += $"?productId={productId.Value}";
            }

            return await httpClientService.GetAsync<PriceHistoryStatistics>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving price history statistics for supplier {SupplierId}", supplierId);
            throw;
        }
    }

    public async Task<List<PriceTrendDataPoint>> GetTrendDataAsync(
        Guid supplierId,
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<PriceTrendDataPoint>>(
                       $"{BaseUrl}/suppliers/{supplierId}/products/{productId}/trend?fromDate={Uri.EscapeDataString(fromDate.ToString("O"))}&toDate={Uri.EscapeDataString(toDate.ToString("O"))}",
                       ct)
                   ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving trend data for supplier {SupplierId} and product {ProductId}", supplierId, productId);
            throw;
        }
    }

    private static string BuildQueryString(PriceHistoryRequest request)
    {
        List<string> queryParams =
        [
            $"page={request.Page}",
            $"pageSize={request.PageSize}"
        ];

        if (request.FromDate.HasValue)
            queryParams.Add($"fromDate={Uri.EscapeDataString(request.FromDate.Value.ToString("O"))}");

        if (request.ToDate.HasValue)
            queryParams.Add($"toDate={Uri.EscapeDataString(request.ToDate.Value.ToString("O"))}");

        if (!string.IsNullOrWhiteSpace(request.ChangeSource))
            queryParams.Add($"changeSource={Uri.EscapeDataString(request.ChangeSource)}");

        if (request.MinChangePercentage.HasValue)
            queryParams.Add($"minChangePercentage={request.MinChangePercentage.Value}");

        if (!string.IsNullOrWhiteSpace(request.SortBy))
            queryParams.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

        if (!string.IsNullOrWhiteSpace(request.SortDirection))
            queryParams.Add($"sortDirection={Uri.EscapeDataString(request.SortDirection)}");

        return string.Join("&", queryParams);
    }
}
