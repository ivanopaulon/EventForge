using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Station;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.Warehouse;
using Microsoft.AspNetCore.Components.Forms;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public interface IProductService
{
    Task<ProductDto?> GetProductByCodeAsync(string code, CancellationToken ct = default);
    Task<ProductWithCodeDto?> GetProductWithCodeByCodeAsync(string code, CancellationToken ct = default);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);

    // NEW: dettagli prodotto (include codes/units/bundle-items)
    Task<ProductDto?> GetProductDetailAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default);
    Task<ProductDto?> CreateProductAsync(CreateProductDto createDto, CancellationToken ct = default);
    Task<ProductDetailDto?> CreateProductWithCodesAndUnitsAsync(CreateProductWithCodesAndUnitsDto createDto, CancellationToken ct = default);
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateDto, CancellationToken ct = default);

    // Added: delete product (returns true on success)
    Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default);

    Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default);
    Task<IEnumerable<StationDto>> GetStationsAsync(CancellationToken ct = default);
    Task<string?> UploadProductImageAsync(IBrowserFile file, CancellationToken ct = default);
    Task<ProductDto?> UploadProductImageDocumentAsync(Guid productId, IBrowserFile file, CancellationToken ct = default);

    // Product Supplier management
    Task<IEnumerable<ProductSupplierDto>?> GetProductSuppliersAsync(Guid productId, CancellationToken ct = default);
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductSupplierDto?> CreateProductSupplierAsync(CreateProductSupplierDto createDto, CancellationToken ct = default);
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteProductSupplierAsync(Guid id, CancellationToken ct = default);
    Task<bool> RemoveProductSupplierAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ProductWithAssociationDto>?> GetProductsWithSupplierAssociationAsync(Guid supplierId, CancellationToken ct = default);
    Task<PagedResult<ProductSupplierDto>?> GetProductsBySupplierAsync(Guid supplierId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<int> BulkUpdateProductSupplierAssociationsAsync(Guid supplierId, IEnumerable<Guid> productIds, CancellationToken ct = default);

    // Product Code management
    Task<IEnumerable<ProductCodeDto>?> GetProductCodesAsync(Guid productId, CancellationToken ct = default);
    Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto, CancellationToken ct = default);
    Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteProductCodeAsync(Guid id, CancellationToken ct = default);

    // Product Unit management
    Task<IEnumerable<ProductUnitDto>?> GetProductUnitsAsync(Guid productId, CancellationToken ct = default);
    Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductUnitDto?> CreateProductUnitAsync(CreateProductUnitDto createDto, CancellationToken ct = default);
    Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteProductUnitAsync(Guid id, CancellationToken ct = default);

    // Product Bundle Item management
    Task<IEnumerable<ProductBundleItemDto>?> GetProductBundleItemsAsync(Guid bundleProductId, CancellationToken ct = default);
    Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductBundleItemDto?> CreateProductBundleItemAsync(CreateProductBundleItemDto createDto, CancellationToken ct = default);
    Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteProductBundleItemAsync(Guid id, CancellationToken ct = default);

    // Product Document Movements and Stock Trend
    Task<PagedResult<ProductDocumentMovementDto>?> GetProductDocumentMovementsAsync(
        Guid productId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? businessPartyName = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);
    Task<StockTrendDto?> GetProductStockTrendAsync(Guid productId, int? year = null, CancellationToken ct = default);

    // Product Price Trend
    Task<PriceTrendDto?> GetProductPriceTrendAsync(Guid productId, int? year = null, CancellationToken ct = default);

    // Product Recent Transactions
    Task<IEnumerable<RecentProductTransactionDto>?> GetRecentProductTransactionsAsync(Guid productId, string type = "purchase", Guid? partyId = null, int top = 3, CancellationToken ct = default);

    // Unified Product Search
    Task<ProductSearchResultDto?> SearchProductsAsync(string query, int maxResults = 20, CancellationToken ct = default);

    // Bulk Operations
    Task<EventForge.DTOs.Bulk.BulkUpdateResultDto?> BulkUpdatePricesAsync(EventForge.DTOs.Bulk.BulkUpdatePricesDto bulkUpdateDto, CancellationToken ct = default);
}
