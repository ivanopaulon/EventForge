using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of stock management service using HTTP client.
/// </summary>
public class StockService : IStockService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<StockService> _logger;
    private const string BaseUrl = "api/v1/warehouse/stock";

    public StockService(IHttpClientService httpClientService, ILogger<StockService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (productId.HasValue)
                queryParams.Add($"productId={productId.Value}");
            if (locationId.HasValue)
                queryParams.Add($"locationId={locationId.Value}");
            if (lotId.HasValue)
                queryParams.Add($"lotId={lotId.Value}");
            if (lowStock.HasValue)
                queryParams.Add($"lowStock={lowStock.Value}");

            var query = string.Join("&", queryParams);
            return await _httpClientService.GetAsync<PagedResult<StockDto>>($"{BaseUrl}?{query}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock entries");
            return null;
        }
    }

    public async Task<StockDto?> GetStockByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<StockDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock entry by ID: {StockId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId)
    {
        try
        {
            // Usa il nuovo endpoint dedicato creato nel server
            var stocks = await _httpClientService.GetAsync<IEnumerable<StockDto>>($"{BaseUrl}/product/{productId}");
            return stocks ?? Enumerable.Empty<StockDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock entries by product ID: {ProductId}", productId);
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
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}",
                $"detailedView={detailedView.ToString().ToLower()}"
            };

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
            return await _httpClientService.GetAsync<PagedResult<StockLocationDetail>>($"{BaseUrl}/overview?{query}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock overview");
            return null;
        }
    }

    public async Task<StockDto?> AdjustStockAsync(AdjustStockDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<AdjustStockDto, StockDto>($"{BaseUrl}/adjust", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for StockId: {StockId}", dto.StockId);
            return null;
        }
    }

    public async Task<StockDto?> CreateOrUpdateStockAsync(CreateStockDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateStockDto, StockDto>($"{BaseUrl}", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating stock for ProductId: {ProductId}", dto.ProductId);
            return null;
        }
    }

    public async Task<StockDto?> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateOrUpdateStockDto, StockDto>($"{BaseUrl}/create-or-update", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating stock - StockId: {StockId}, ProductId: {ProductId}", dto.StockId, dto.ProductId);
            return null;
        }
    }
}
