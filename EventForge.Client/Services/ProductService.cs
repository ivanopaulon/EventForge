using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of product management service using HTTP client.
/// </summary>
public class ProductService : IProductService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductService> _logger;
    private const string BaseUrl = "api/v1/product-management/products";

    public ProductService(IHttpClientFactory httpClientFactory, ILogger<ProductService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/by-code/{Uri.EscapeDataString(code)}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve product by code {Code}. Status: {StatusCode}", code, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by code {Code}", code);
            return null;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve product by ID {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by ID {Id}", id);
            return null;
        }
    }

    public async Task<PagedResult<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResult<ProductDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to retrieve products. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return null;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create product. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return null;
        }
    }

    public async Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/{createDto.ProductId}/codes", createDto);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductCodeDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create product code. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product code");
            return null;
        }
    }
}
