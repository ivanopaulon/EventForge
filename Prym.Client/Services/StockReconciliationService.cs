using Prym.DTOs.Warehouse;

namespace Prym.Client.Services;

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
            return await httpClientService.PostAsync<StockReconciliationRequestDto, StockReconciliationResultDto>(
                $"{BaseUrl}/calculate", request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating stock reconciliation");
            return null;
        }
    }

    public async Task<StockReconciliationApplyResultDto?> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<StockReconciliationApplyRequestDto, StockReconciliationApplyResultDto>(
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

            queryParams.Add($"includeDocuments={request.IncludeDocuments.ToString().ToLower()}");
            queryParams.Add($"includeInventories={request.IncludeInventories.ToString().ToLower()}");
            queryParams.Add($"onlyWithDiscrepancies={request.OnlyWithDiscrepancies.ToString().ToLower()}");
            queryParams.Add($"discrepancyThreshold={request.DiscrepancyThreshold}");

            var query = string.Join("&", queryParams);
            var url = queryParams.Count > 0 ? $"{BaseUrl}/export?{query}" : $"{BaseUrl}/export";

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
}
