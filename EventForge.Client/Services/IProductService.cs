using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Station;
using EventForge.DTOs.UnitOfMeasures;
using Microsoft.AspNetCore.Components.Forms;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public interface IProductService
{
    Task<ProductDto?> GetProductByCodeAsync(string code);
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<PagedResult<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20);
    Task<ProductDto?> CreateProductAsync(CreateProductDto createDto);
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto);
    Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto);
    Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync();
    Task<IEnumerable<StationDto>> GetStationsAsync();
    Task<string?> UploadProductImageAsync(IBrowserFile file);

    // Product Supplier management
    Task<IEnumerable<ProductSupplierDto>?> GetProductSuppliersAsync(Guid productId);
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id);
    Task<ProductSupplierDto?> CreateProductSupplierAsync(CreateProductSupplierDto createDto);
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateDto);
    Task<bool> DeleteProductSupplierAsync(Guid id);
}
