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
    Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync();
    Task<IEnumerable<StationDto>> GetStationsAsync();
    Task<string?> UploadProductImageAsync(IBrowserFile file);
    Task<ProductDto?> UploadProductImageDocumentAsync(Guid productId, IBrowserFile file);

    // Product Supplier management
    Task<IEnumerable<ProductSupplierDto>?> GetProductSuppliersAsync(Guid productId);
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id);
    Task<ProductSupplierDto?> CreateProductSupplierAsync(CreateProductSupplierDto createDto);
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateDto);
    Task<bool> DeleteProductSupplierAsync(Guid id);

    // Product Code management
    Task<IEnumerable<ProductCodeDto>?> GetProductCodesAsync(Guid productId);
    Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id);
    Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto);
    Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateDto);
    Task<bool> DeleteProductCodeAsync(Guid id);

    // Product Unit management
    Task<IEnumerable<ProductUnitDto>?> GetProductUnitsAsync(Guid productId);
    Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id);
    Task<ProductUnitDto?> CreateProductUnitAsync(CreateProductUnitDto createDto);
    Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateDto);
    Task<bool> DeleteProductUnitAsync(Guid id);

    // Product Bundle Item management
    Task<IEnumerable<ProductBundleItemDto>?> GetProductBundleItemsAsync(Guid bundleProductId);
    Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id);
    Task<ProductBundleItemDto?> CreateProductBundleItemAsync(CreateProductBundleItemDto createDto);
    Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateDto);
    Task<bool> DeleteProductBundleItemAsync(Guid id);
}
