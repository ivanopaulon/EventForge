using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Station;
using EventForge.DTOs.UnitOfMeasures;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of product management service using HTTP client.
/// </summary>
public class ProductService : IProductService
{
    private readonly IHttpClientService _httpClientService;
    private readonly IHttpClientFactory _httpClientFactory; // Keep for image upload only
    private readonly ILogger<ProductService> _logger;
    private const string BaseUrl = "api/v1/product-management/products";

    public ProductService(IHttpClientService httpClientService, IHttpClientFactory httpClientFactory, ILogger<ProductService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductDto>($"{BaseUrl}/by-code/{Uri.EscapeDataString(code)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by code {Code}", code);
            return null;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product by ID {Id}", id);
            return null;
        }
    }

    public async Task<PagedResult<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<ProductDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return null;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateProductDto, ProductDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return null;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateProductDto, ProductDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return null;
        }
    }

    public async Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateProductCodeDto, ProductCodeDto>($"{BaseUrl}/{createDto.ProductId}/codes", createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product code");
            return null;
        }
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync()
    {
        try
        {
            var pagedResult = await _httpClientService.GetAsync<PagedResult<UMDto>>("api/v1/product-management/units?page=1&pageSize=100");
            return pagedResult?.Items ?? new List<UMDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving units of measure");
            return new List<UMDto>();
        }
    }

    public async Task<IEnumerable<StationDto>> GetStationsAsync()
    {
        try
        {
            var pagedResult = await _httpClientService.GetAsync<PagedResult<StationDto>>("api/v1/stations?page=1&pageSize=100");
            return pagedResult?.Items ?? new List<StationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stations");
            return new List<StationDto>();
        }
    }

    public async Task<string?> UploadProductImageAsync(IBrowserFile file)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await httpClient.PostAsync("api/v1/product-management/products/upload-image", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImageUploadResultDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result?.ImageUrl;
            }

            _logger.LogError("Failed to upload product image. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading product image");
            return null;
        }
    }

    // Product Supplier management

    public async Task<IEnumerable<ProductSupplierDto>?> GetProductSuppliersAsync(Guid productId)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductSupplierDto>>($"{BaseUrl}/{productId}/suppliers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product suppliers for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductSupplierDto>($"api/v1/product-management/product-suppliers/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product supplier {Id}", id);
            return null;
        }
    }

    public async Task<ProductSupplierDto?> CreateProductSupplierAsync(CreateProductSupplierDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateProductSupplierDto, ProductSupplierDto>("api/v1/product-management/product-suppliers", createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product supplier");
            return null;
        }
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateProductSupplierDto, ProductSupplierDto>($"api/v1/product-management/product-suppliers/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product supplier {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteProductSupplierAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/product-management/product-suppliers/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product supplier {Id}", id);
            return false;
        }
    }
}

public class ImageUploadResultDto
{
    public string ImageUrl { get; set; } = string.Empty;
}
