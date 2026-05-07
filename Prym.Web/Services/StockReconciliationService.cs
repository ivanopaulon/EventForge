using Prym.DTOs.Warehouse;

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
        List<string> queryParams = [];

        if (request.FromDate.HasValue)
            queryParams.Add($"fromDate={request.FromDate.Value:O}");
        if (request.ToDate.HasValue)
            queryParams.Add($"toDate={request.ToDate.Value:O}");
        if (request.WarehouseId.HasValue)
            queryParams.Add($"warehouseId={request.WarehouseId.Value}");
        if (request.LocationId.HasValue)
            queryParams.Add($"locationId={request.LocationId.Value}");
        if (request.ProductId.HasValue)
            queryParams.Add($"productId={request.ProductId.Value}");
        if (request.StartingQuantity.HasValue)
            queryParams.Add($"startingQuantity={request.StartingQuantity.Value}");

        queryParams.Add($"includeDocuments={request.IncludeDocuments.ToString().ToLowerInvariant()}");
        queryParams.Add($"includeInventories={request.IncludeInventories.ToString().ToLowerInvariant()}");
        queryParams.Add($"includeStockMovements={request.IncludeStockMovements.ToString().ToLowerInvariant()}");
        queryParams.Add($"onlyWithDiscrepancies={request.OnlyWithDiscrepancies.ToString().ToLowerInvariant()}");
        queryParams.Add($"discrepancyThreshold={request.DiscrepancyThreshold}");

        return string.Join("&", queryParams);
    }
}
