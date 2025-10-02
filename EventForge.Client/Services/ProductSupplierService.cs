using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing product suppliers.
/// </summary>
public class ProductSupplierService : IProductSupplierService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ProductSupplierService> _logger;
    private const string BaseUrl = "api/v1/product-management/product-suppliers";

    public ProductSupplierService(IHttpClientService httpClientService, ILogger<ProductSupplierService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ProductSupplierDto>> GetProductSuppliersAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<ProductSupplierDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<ProductSupplierDto> { Items = new List<ProductSupplierDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product suppliers");
            throw;
        }
    }

    public async Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<ProductSupplierDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product supplier with ID {Id}", id);
            throw;
        }
    }

    public async Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateProductSupplierDto, ProductSupplierDto>(BaseUrl, createProductSupplierDto);
            return result ?? throw new InvalidOperationException("Failed to create product supplier");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product supplier");
            throw;
        }
    }

    public async Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateProductSupplierDto, ProductSupplierDto>($"{BaseUrl}/{id}", updateProductSupplierDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product supplier with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductSupplierAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product supplier with ID {Id}", id);
            throw;
        }
    }
}
