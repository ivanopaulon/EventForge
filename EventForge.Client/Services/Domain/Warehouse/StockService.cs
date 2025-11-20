using EventForge.DTOs.Common;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;
using EventForge.DTOs.Warehouse;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Domain.Warehouse;

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
}
