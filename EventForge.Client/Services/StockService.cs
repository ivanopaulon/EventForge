using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of stock management service using HTTP client.
/// </summary>
public class StockService(
    IHttpClientService httpClientService,
    ILogger<StockService> logger) : IStockService
{
    private const string BaseUrl = "api/v1/warehouse/stock";

    public async Task<PagedResult<StockDto>?> GetStockAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null)
    {
        try
        {
            List<string> queryParams =
            [
                $"page={page}",
                $"pageSize={pageSize}"
            ];

            if (productId.HasValue)
                queryParams.Add($"productId={productId.Value}");
            if (locationId.HasValue)
                queryParams.Add($"locationId={locationId.Value}");
            if (lotId.HasValue)
                queryParams.Add($"lotId={lotId.Value}");
            if (lowStock.HasValue)
                queryParams.Add($"lowStock={lowStock.Value}");

            var query = string.Join("&", queryParams);
            return await httpClientService.GetAsync<PagedResult<StockDto>>($"{BaseUrl}?{query}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stock entries");
            return null;
        }
    }

    public async Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<StockDto>($"{BaseUrl}/{id}", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stock entry by ID: {StockId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            // Usa il nuovo endpoint dedicato creato nel server
            var stocks = await httpClientService.GetAsync<IEnumerable<StockDto>>($"{BaseUrl}/product/{productId}", ct);

            return stocks ?? Enumerable.Empty<StockDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stock entries by product ID: {ProductId}", productId);
            return Enumerable.Empty<StockDto>();
        }
    }

    public async Task<PagedResult<StockLocationDetail>?> GetStockOverviewAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        Guid? warehouseId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null,
        bool? criticalStock = null,
        bool? outOfStock = null,
        bool? inStockOnly = null,
        bool? showAllProducts = null,
        bool detailedView = false)
    {
        try
        {
            List<string> queryParams =
            [
                $"page={page}",
                $"pageSize={pageSize}",
                $"detailedView={detailedView.ToString().ToLower()}"
            ];

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"search={Uri.EscapeDataString(searchTerm)}");
            if (warehouseId.HasValue)
                queryParams.Add($"warehouseId={warehouseId.Value}");
            if (locationId.HasValue)
                queryParams.Add($"locationId={locationId.Value}");
            if (lotId.HasValue)
                queryParams.Add($"lotId={lotId.Value}");
            if (lowStock.HasValue)
                queryParams.Add($"lowStock={lowStock.Value}");
            if (criticalStock.HasValue)
                queryParams.Add($"criticalStock={criticalStock.Value}");
            if (outOfStock.HasValue)
                queryParams.Add($"outOfStock={outOfStock.Value}");
            if (inStockOnly.HasValue)
                queryParams.Add($"inStockOnly={inStockOnly.Value}");
            if (showAllProducts.HasValue)
                queryParams.Add($"showAllProducts={showAllProducts.Value}");

            var query = string.Join("&", queryParams);
            return await httpClientService.GetAsync<PagedResult<StockLocationDetail>>($"{BaseUrl}/overview?{query}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving stock overview");
            return null;
        }
    }

    public async Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<AdjustStockDto, StockDto>($"{BaseUrl}/adjust", dto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adjusting stock for StockId: {StockId}", dto.StockId);
            return null;
        }
    }

    public async Task<StockDto?> CreateOrUpdateStockAsync(CreateStockDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateStockDto, StockDto>($"{BaseUrl}", dto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating/updating stock for ProductId: {ProductId}", dto.ProductId);
            return null;
        }
    }

    public async Task<StockDto?> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateOrUpdateStockDto, StockDto>($"{BaseUrl}/create-or-update", dto, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating/updating stock - StockId: {StockId}, ProductId: {ProductId}", dto.StockId, dto.ProductId);
            return null;
        }
    }
}
