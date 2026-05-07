using Prym.DTOs.Warehouse;
using System.Globalization;

namespace Prym.Web.Services;

/// <summary>
/// Implementation of stock reconciliation service using HTTP client.
/// </summary>
public class StockReconciliationService(
    IHttpClientService httpClientService,
    ILogger<StockReconciliationService> logger) : IStockReconciliationService
{
    private const string BaseUrl = "api/v1/warehouse/stock-reconciliation";

    public async Task<StockReconciliationResultDto?> CalculateReconciliationAsync(StockReconciliationRequestDto request, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostLongRunningAsync<StockReconciliationRequestDto, StockReconciliationResultDto>(
                $"{BaseUrl}/calculate", request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating stock reconciliation");
            return null;
        }
    }

    public async Task<List<Guid>?> GetReconciliationStockIdsAsync(StockReconciliationRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var query = BuildQueryString(request);
            var url = string.IsNullOrWhiteSpace(query)
                ? $"{BaseUrl}/stock-ids"
                : $"{BaseUrl}/stock-ids?{query}";

            return await httpClientService.GetAsync<List<Guid>>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stock ids for reconciliation");
            return null;
        }
    }

    public async Task<StockReconciliationResultDto?> CalculateReconciliationBatchAsync(StockReconciliationBatchRequestDto request, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostLongRunningAsync<StockReconciliationBatchRequestDto, StockReconciliationResultDto>(
                $"{BaseUrl}/calculate-batch", request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating stock reconciliation batch");
            return null;
        }
    }

    public async Task<StockReconciliationApplyResultDto?> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostLongRunningAsync<StockReconciliationApplyRequestDto, StockReconciliationApplyResultDto>(
                $"{BaseUrl}/apply", request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying stock reconciliation corrections");
            return null;
        }
    }

    public async Task<byte[]?> ExportReconciliationAsync(StockReconciliationRequestDto request, CancellationToken ct = default)
    {
        try
        {
            var query = BuildQueryString(request);
            var url = string.IsNullOrWhiteSpace(query) ? $"{BaseUrl}/export" : $"{BaseUrl}/export?{query}";

            return await httpClientService.GetAsync<byte[]>(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting stock reconciliation data");
            return null;
        }
    }

    public async Task<RebuildMovementsResultDto?> RebuildMovementsPreviewAsync(RebuildMovementsRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<RebuildMovementsRequestDto, RebuildMovementsResultDto>(
                $"{BaseUrl}/rebuild-movements/preview", request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error previewing rebuild of missing stock movements");
            return null;
        }
    }

    public async Task<RebuildMovementsResultDto?> RebuildMovementsExecuteAsync(RebuildMovementsRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostLongRunningAsync<RebuildMovementsRequestDto, RebuildMovementsResultDto>(
                $"{BaseUrl}/rebuild-movements/execute", request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing rebuild of missing stock movements");
            return null;
        }
    }

    private static string BuildQueryString(StockReconciliationRequestDto request)
    {
        List<KeyValuePair<string, string>> queryParams = [];

        if (request.FromDate.HasValue)
            queryParams.Add(new("fromDate", request.FromDate.Value.ToString("O", CultureInfo.InvariantCulture)));
        if (request.ToDate.HasValue)
            queryParams.Add(new("toDate", request.ToDate.Value.ToString("O", CultureInfo.InvariantCulture)));
        if (request.WarehouseId.HasValue)
            queryParams.Add(new("warehouseId", request.WarehouseId.Value.ToString()));
        if (request.LocationId.HasValue)
            queryParams.Add(new("locationId", request.LocationId.Value.ToString()));
        if (request.ProductId.HasValue)
            queryParams.Add(new("productId", request.ProductId.Value.ToString()));
        if (request.StartingQuantity.HasValue)
            queryParams.Add(new("startingQuantity", request.StartingQuantity.Value.ToString(CultureInfo.InvariantCulture)));

        queryParams.Add(new("includeDocuments", request.IncludeDocuments.ToString().ToLowerInvariant()));
        queryParams.Add(new("includeInventories", request.IncludeInventories.ToString().ToLowerInvariant()));
        queryParams.Add(new("includeStockMovements", request.IncludeStockMovements.ToString().ToLowerInvariant()));
        queryParams.Add(new("onlyWithDiscrepancies", request.OnlyWithDiscrepancies.ToString().ToLowerInvariant()));
        queryParams.Add(new("discrepancyThreshold", request.DiscrepancyThreshold.ToString(CultureInfo.InvariantCulture)));

        return string.Join("&", queryParams.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }
}
