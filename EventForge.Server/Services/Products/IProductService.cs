using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service interface for managing products and related entities.
/// </summary>
public interface IProductService
{
    // Product CRUD operations

    /// <summary>
    /// Gets all products with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="searchTerm">Optional search term to filter products by code, name, or description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    Task<PagedResult<ProductDto>> GetProductsAsync(PaginationParameters pagination, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product DTO or null if not found</returns>
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed product information including codes, units, and bundle items.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed product DTO or null if not found</returns>
    Task<ProductDetailDto?> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="createProductDto">Product creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product DTO</returns>
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product with multiple codes and units of measure in a single transaction.
    /// Used for quick product creation during inventory procedures.
    /// </summary>
    /// <param name="createDto">Product creation data with codes and units</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product DTO with full details</returns>
    Task<ProductDetailDto> CreateProductWithCodesAndUnitsAsync(CreateProductWithCodesAndUnitsDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateProductDto">Product update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product DTO or null if not found</returns>
    Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteProductAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Product Code management operations

    /// <summary>
    /// Gets all codes for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product codes</returns>
    Task<IEnumerable<ProductCodeDto>> GetProductCodesAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product code by ID.
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product code DTO or null if not found</returns>
    Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by barcode/code value.
    /// </summary>
    /// <param name="codeValue">Barcode or code value to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product DTO or null if not found</returns>
    Task<ProductDto?> GetProductByCodeAsync(string codeValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product with its code context by barcode/code value.
    /// Includes the ProductCode information (with ProductUnitId) that was matched.
    /// </summary>
    /// <param name="codeValue">Barcode or code value to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product with code context or null if not found</returns>
    Task<ProductWithCodeDto?> GetProductWithCodeByCodeAsync(string codeValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new code to a product.
    /// </summary>
    /// <param name="createProductCodeDto">Product code creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product code DTO</returns>
    Task<ProductCodeDto> AddProductCodeAsync(CreateProductCodeDto createProductCodeDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product code.
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="updateProductCodeDto">Product code update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product code DTO or null if not found</returns>
    Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateProductCodeDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a code from a product (soft delete).
    /// </summary>
    /// <param name="id">Product code ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveProductCodeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Product Unit management operations

    /// <summary>
    /// Gets all units for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product units</returns>
    Task<IEnumerable<ProductUnitDto>> GetProductUnitsAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product unit by ID.
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product unit DTO or null if not found</returns>
    Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new unit to a product.
    /// </summary>
    /// <param name="createProductUnitDto">Product unit creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product unit DTO</returns>
    Task<ProductUnitDto> AddProductUnitAsync(CreateProductUnitDto createProductUnitDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product unit.
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="updateProductUnitDto">Product unit update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product unit DTO or null if not found</returns>
    Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateProductUnitDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a unit from a product (soft delete).
    /// </summary>
    /// <param name="id">Product unit ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveProductUnitAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Product Bundle Item management operations

    /// <summary>
    /// Gets all bundle items for a product.
    /// </summary>
    /// <param name="bundleProductId">Bundle product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bundle items</returns>
    Task<IEnumerable<ProductBundleItemDto>> GetProductBundleItemsAsync(Guid bundleProductId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a bundle item by ID.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bundle item DTO or null if not found</returns>
    Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new bundle item to a product.
    /// </summary>
    /// <param name="createProductBundleItemDto">Bundle item creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bundle item DTO</returns>
    Task<ProductBundleItemDto> AddProductBundleItemAsync(CreateProductBundleItemDto createProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing bundle item.
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="updateProductBundleItemDto">Bundle item update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bundle item DTO or null if not found</returns>
    Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateProductBundleItemDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a bundle item from a product (soft delete).
    /// </summary>
    /// <param name="id">Bundle item ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveProductBundleItemAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product exists.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the image URL for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="imageUrl">New image URL</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product DTO or null if not found</returns>
    Task<ProductDto?> UpdateProductImageAsync(Guid productId, string imageUrl, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image file as a DocumentReference and links it to a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="file">Image file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product DTO or null if not found</returns>
    Task<ProductDto?> UploadProductImageAsync(Guid productId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the image DocumentReference for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DocumentReference DTO or null if not found</returns>
    Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetProductImageDocumentAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the image DocumentReference for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteProductImageAsync(Guid productId, CancellationToken cancellationToken = default);

    // Product Supplier management operations

    /// <summary>
    /// Gets all suppliers for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product suppliers</returns>
    Task<IEnumerable<ProductSupplierDto>> GetProductSuppliersAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product supplier by ID.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new supplier to a product.
    /// </summary>
    /// <param name="createProductSupplierDto">Product supplier creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product supplier DTO</returns>
    Task<ProductSupplierDto> AddProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product supplier.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="updateProductSupplierDto">Product supplier update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a supplier from a product (soft delete).
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products with their association status for a specific supplier.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with association status</returns>
    Task<IEnumerable<ProductWithAssociationDto>> GetProductsWithSupplierAssociationAsync(Guid supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates product-supplier associations.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="productIds">List of product IDs to associate</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of associations created</returns>
    Task<int> BulkUpdateProductSupplierAssociationsAsync(Guid supplierId, IEnumerable<Guid> productIds, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by supplier with pagination, enriched with latest purchase data.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product suppliers with enriched data</returns>
    Task<PagedResult<ProductSupplierDto>> GetProductsBySupplierAsync(Guid supplierId, PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent product transactions (purchases or sales) for price suggestions.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="type">Transaction type: "purchase" or "sale"</param>
    /// <param name="partyId">Optional business party ID to filter by</param>
    /// <param name="top">Number of recent transactions to return (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent product transactions</returns>
    Task<IEnumerable<RecentProductTransactionDto>> GetRecentProductTransactionsAsync(Guid productId, string type = "purchase", Guid? partyId = null, int top = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs unified product search with exact code match priority.
    /// First searches for exact match on ProductCodes.Code and Product.Code (case-insensitive).
    /// If no exact match found, performs text search on Product.Name, ShortDescription, Description, and Brand.Name.
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search result DTO with exact match and/or text search results</returns>
    Task<ProductSearchResultDto> SearchProductsAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products for export with batch processing support
    /// </summary>
    Task<IEnumerable<EventForge.DTOs.Export.ProductExportDto>> GetProductsForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);
}