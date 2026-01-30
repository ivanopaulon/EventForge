using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Station;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.Warehouse;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of product management service using HTTP client.
/// </summary>
public class ProductService : IProductService
{
    private readonly IHttpClientService _httpClientService;
    private readonly IHttpClientFactory _httpClientFactory; // Keep for image upload only
    private readonly IAuthService _authService;
    private readonly ILogger<ProductService> _logger;
    private const string BaseUrl = "api/v1/product-management/products";

    public ProductService(IHttpClientService httpClientService, IHttpClientFactory httpClientFactory, IAuthService authService, ILogger<ProductService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken ct = default)
    {
        try
        {
            // Legacy endpoint for backward compatibility - returns only product
            // New code should use GetProductWithCodeByCodeAsync
            var result = await GetProductWithCodeByCodeAsync(code, ct);
            return result?.Product;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductWithCodeDto?> GetProductWithCodeByCodeAsync(string code, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductWithCodeDto>($"{BaseUrl}/by-code/{Uri.EscapeDataString(code)}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductDto>($"{BaseUrl}/{id}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // NEW: request detailed product payload (server-side exposes a detailed DTO)
    public async Task<ProductDto?> GetProductDetailAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            // Endpoint path: assume server exposes /{id}/detail
            return await _httpClientService.GetAsync<ProductDto>($"{BaseUrl}/{id}/detail", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PagedResult<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            }
            return await _httpClientService.GetAsync<PagedResult<ProductDto>>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductDto createDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating product with name: {Name}, code: {Code}", createDto.Name, createDto.Code);
        var result = await _httpClientService.PostAsync<CreateProductDto, ProductDto>(BaseUrl, createDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product created successfully with ID: {ProductId}", result.Id);
        }

        return result;
    }

    public async Task<ProductDetailDto?> CreateProductWithCodesAndUnitsAsync(CreateProductWithCodesAndUnitsDto createDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating product with codes and units: {Name}", createDto.Name);
        var result = await _httpClientService.PostAsync<CreateProductWithCodesAndUnitsDto, ProductDetailDto>(
            $"{BaseUrl}/create-with-codes-units",
            createDto,
            ct);

        if (result != null)
        {
            _logger.LogInformation("Product with codes and units created successfully with ID: {ProductId}", result.Id);
        }

        return result;
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating product {ProductId} with name: {Name}, IsVatIncluded: {IsVatIncluded}",
            id, updateDto.Name, updateDto.IsVatIncluded);
        var result = await _httpClientService.PutAsync<UpdateProductDto, ProductDto>($"{BaseUrl}/{id}", updateDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product {ProductId} updated successfully", id);
        }

        return result;
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting product {ProductId}", id);
        await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
        _logger.LogInformation("Product {ProductId} deleted successfully", id);
        return true;
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default)
    {
        try
        {
            var pagedResult = await _httpClientService.GetAsync<PagedResult<UMDto>>("api/v1/product-management/units?page=1&pageSize=100", ct);
            return pagedResult?.Items ?? new List<UMDto>();
        }
        catch (HttpRequestException)
        {
            return new List<UMDto>();
        }
    }

    public async Task<IEnumerable<StationDto>> GetStationsAsync(CancellationToken ct = default)
    {
        try
        {
            var pagedResult = await _httpClientService.GetAsync<PagedResult<StationDto>>("api/v1/stations?page=1&pageSize=100", ct);
            return pagedResult?.Items ?? new List<StationDto>();
        }
        catch (HttpRequestException)
        {
            return new List<StationDto>();
        }
    }

    public async Task<string?> UploadProductImageAsync(IBrowserFile file, CancellationToken ct = default)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/product-management/products/upload-image")
            {
                Content = content
            };

            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.SendAsync(request, ct);

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

    public async Task<ProductDto?> UploadProductImageDocumentAsync(Guid productId, IBrowserFile file, CancellationToken ct = default)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/product-management/products/{productId}/image")
            {
                Content = content
            };

            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result;
            }

            _logger.LogError("Failed to upload product image document. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading product image document for product {ProductId}", productId);
            return null;
        }
    }

    // Product Supplier management

    public async Task<IEnumerable<ProductSupplierDto>?> GetProductSuppliersAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductSupplierDto>>($"{BaseUrl}/{productId}/suppliers", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductSupplierDto>($"api/v1/product-management/product-suppliers/{id}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductSupplierDto?> CreateProductSupplierAsync(CreateProductSupplierDto createDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating product supplier for product {ProductId}", createDto.ProductId);
        var result = await _httpClientService.PostAsync<CreateProductSupplierDto, ProductSupplierDto>("api/v1/product-management/product-suppliers", createDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product supplier created successfully with ID: {ProductSupplierId}", result.Id);
        }

        return result;
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating product supplier {ProductSupplierId}", id);
        var result = await _httpClientService.PutAsync<UpdateProductSupplierDto, ProductSupplierDto>($"api/v1/product-management/product-suppliers/{id}", updateDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product supplier {ProductSupplierId} updated successfully", id);
        }

        return result;
    }

    public async Task<bool> DeleteProductSupplierAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting product supplier {ProductSupplierId}", id);
        await _httpClientService.DeleteAsync($"api/v1/product-management/product-suppliers/{id}", ct);
        _logger.LogInformation("Product supplier {ProductSupplierId} deleted successfully", id);
        return true;
    }

    public async Task<bool> RemoveProductSupplierAsync(Guid id, CancellationToken ct = default)
    {
        return await DeleteProductSupplierAsync(id, ct);
    }

    public async Task<IEnumerable<ProductWithAssociationDto>?> GetProductsWithSupplierAssociationAsync(Guid supplierId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductWithAssociationDto>>($"api/v1/product-management/suppliers/{supplierId}/products", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PagedResult<ProductSupplierDto>?> GetProductsBySupplierAsync(Guid supplierId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<ProductSupplierDto>>($"api/v1/product-management/suppliers/{supplierId}/supplied-products?page={page}&pageSize={pageSize}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<int> BulkUpdateProductSupplierAssociationsAsync(Guid supplierId, IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<IEnumerable<Guid>, int>($"api/v1/product-management/suppliers/{supplierId}/products/bulk-update", productIds, ct);
            return result;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    // Product Code management

    public async Task<IEnumerable<ProductCodeDto>?> GetProductCodesAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductCodeDto>>($"{BaseUrl}/{productId}/codes", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductCodeDto>($"api/v1/product-management/product-codes/{id}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating product code for product {ProductId}", createDto.ProductId);
        var result = await _httpClientService.PostAsync<CreateProductCodeDto, ProductCodeDto>($"{BaseUrl}/{createDto.ProductId}/codes", createDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product code created successfully with ID: {ProductCodeId}", result.Id);
        }

        return result;
    }

    public async Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating product code {ProductCodeId}", id);
        var result = await _httpClientService.PutAsync<UpdateProductCodeDto, ProductCodeDto>($"api/v1/product-management/product-codes/{id}", updateDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product code {ProductCodeId} updated successfully", id);
        }

        return result;
    }

    public async Task<bool> DeleteProductCodeAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting product code {ProductCodeId}", id);
        await _httpClientService.DeleteAsync($"api/v1/product-management/product-codes/{id}", ct);
        _logger.LogInformation("Product code {ProductCodeId} deleted successfully", id);
        return true;
    }

    // Product Unit management

    public async Task<IEnumerable<ProductUnitDto>?> GetProductUnitsAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductUnitDto>>($"{BaseUrl}/{productId}/units", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            // Endpoint corretto (coerente con Update/Delete che usano "products/units/{id}")
            return await _httpClientService.GetAsync<ProductUnitDto>($"api/v1/product-management/products/units/{id}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductUnitDto?> CreateProductUnitAsync(CreateProductUnitDto createDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating product unit for product {ProductId}", createDto.ProductId);
        var result = await _httpClientService.PostAsync<CreateProductUnitDto, ProductUnitDto>($"{BaseUrl}/{createDto.ProductId}/units", createDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product unit created successfully with ID: {ProductUnitId}", result.Id);
        }

        return result;
    }

    public async Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateDto, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating product unit {ProductUnitId}", id);
        var result = await _httpClientService.PutAsync<UpdateProductUnitDto, ProductUnitDto>($"api/v1/product-management/products/units/{id}", updateDto, ct);

        if (result != null)
        {
            _logger.LogInformation("Product unit {ProductUnitId} updated successfully", id);
        }

        return result;
    }

    public async Task<bool> DeleteProductUnitAsync(Guid id, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting product unit {ProductUnitId}", id);
        await _httpClientService.DeleteAsync($"api/v1/product-management/products/units/{id}", ct);
        _logger.LogInformation("Product unit {ProductUnitId} deleted successfully", id);
        return true;
    }

    // Product Bundle Item management

    public async Task<IEnumerable<ProductBundleItemDto>?> GetProductBundleItemsAsync(Guid bundleProductId, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<ProductBundleItemDto>>($"{BaseUrl}/{bundleProductId}/bundle-items", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductBundleItemDto>($"api/v1/product-management/product-bundle-items/{id}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductBundleItemDto?> CreateProductBundleItemAsync(CreateProductBundleItemDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateProductBundleItemDto, ProductBundleItemDto>("api/v1/product-management/product-bundle-items", createDto, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateProductBundleItemDto, ProductBundleItemDto>($"api/v1/product-management/product-bundle-items/{id}", updateDto, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteProductBundleItemAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"api/v1/product-management/product-bundle-items/{id}", ct);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<PagedResult<ProductDocumentMovementDto>?> GetProductDocumentMovementsAsync(
        Guid productId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? businessPartyName = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (fromDate.HasValue)
                queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue)
                queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            if (!string.IsNullOrEmpty(businessPartyName))
                queryParams.Add($"businessPartyName={Uri.EscapeDataString(businessPartyName)}");

            var queryString = string.Join("&", queryParams);
            return await _httpClientService.GetAsync<PagedResult<ProductDocumentMovementDto>>(
                $"{BaseUrl}/{productId}/document-movements?{queryString}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<StockTrendDto?> GetProductStockTrendAsync(Guid productId, int? year = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/{productId}/stock-trend";
            if (year.HasValue)
                url += $"?year={year.Value}";

            return await _httpClientService.GetAsync<StockTrendDto>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PriceTrendDto?> GetProductPriceTrendAsync(Guid productId, int? year = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/{productId}/price-trend";
            if (year.HasValue)
                url += $"?year={year.Value}";

            return await _httpClientService.GetAsync<PriceTrendDto>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<RecentProductTransactionDto>?> GetRecentProductTransactionsAsync(
        Guid productId,
        string type = "purchase",
        Guid? partyId = null,
        int top = 3,
        CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/{productId}/recent-transactions?type={type}&top={top}";
            if (partyId.HasValue)
                url += $"&partyId={partyId.Value}";

            return await _httpClientService.GetAsync<IEnumerable<RecentProductTransactionDto>>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ProductSearchResultDto?> SearchProductsAsync(string query, int maxResults = 20, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var url = $"{BaseUrl}/search?q={Uri.EscapeDataString(query)}&maxResults={maxResults}";
            return await _httpClientService.GetAsync<ProductSearchResultDto>(url, ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<EventForge.DTOs.Bulk.BulkUpdateResultDto?> BulkUpdatePricesAsync(EventForge.DTOs.Bulk.BulkUpdatePricesDto bulkUpdateDto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting bulk price update for {Count} products", bulkUpdateDto.ProductIds.Count);
            var result = await _httpClientService.PostAsync<EventForge.DTOs.Bulk.BulkUpdatePricesDto, EventForge.DTOs.Bulk.BulkUpdateResultDto>(
                "api/v1/product-management/bulk-update-prices", 
                bulkUpdateDto, 
                ct);

            if (result != null)
            {
                _logger.LogInformation("Bulk price update completed. Success: {SuccessCount}, Failed: {FailedCount}", 
                    result.SuccessCount, result.FailedCount);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk price update");
            return null;
        }
    }
}

public class ImageUploadResultDto
{
    public string ImageUrl { get; set; } = string.Empty;
}
