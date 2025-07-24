using EventForge.DTOs.Products;

namespace EventForge.Services.Products;

/// <summary>
/// Service interface for managing products and related entities.
/// </summary>
public interface IProductService
{
    // Product CRUD operations

    /// <summary>
    /// Gets all products with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    Task<PagedResult<ProductDto>> GetProductsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

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
}