using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of stock reconciliation service using HTTP client.
/// </summary>
public class StockReconciliationService : IStockReconciliationService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<StockReconciliationService> _logger;
    private const string BaseUrl = "api/v1/warehouse/stock-reconciliation";

    public StockReconciliationService(IHttpClientService httpClientService, ILogger<StockReconciliationService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StockReconciliationResultDto?> CalculateReconciliationAsync(StockReconciliationRequestDto request)
    {
        try
        {
            return await _httpClientService.PostAsync<StockReconciliationRequestDto, StockReconciliationResultDto>(
                $"{BaseUrl}/calculate", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stock reconciliation");
            return null;
        }
    }

    public async Task<StockReconciliationApplyResultDto?> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request)
    {
        try
        {
            return await _httpClientService.PostAsync<StockReconciliationApplyRequestDto, StockReconciliationApplyResultDto>(
                $"{BaseUrl}/apply", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying stock reconciliation corrections");
            return null;
        }
    }

    public async Task<byte[]?> ExportReconciliationAsync(StockReconciliationRequestDto request)
    {
        try
        {
            var queryParams = new List<string>();

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

            var query = string.Join("&", queryParams);
            var url = queryParams.Count > 0 ? $"{BaseUrl}/export?{query}" : $"{BaseUrl}/export";

            return await _httpClientService.GetAsync<byte[]>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting stock reconciliation data");
            return null;
        }
    }
}
