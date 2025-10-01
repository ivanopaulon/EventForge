using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service interface for managing product-supplier relationships.
/// </summary>
public interface IProductSupplierService
{
    /// <summary>
    /// Gets all product-supplier relationships with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product suppliers</returns>
    Task<PagedResult<ProductSupplierDto>> GetProductSuppliersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product-supplier relationships by product ID.
    /// </summary>
    /// <param name="productId">Product ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product suppliers for the product</returns>
    Task<IEnumerable<ProductSupplierDto>> GetProductSuppliersByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product-supplier relationships by supplier ID.
    /// </summary>
    /// <param name="supplierId">Supplier ID to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product suppliers for the supplier</returns>
    Task<PagedResult<ProductSupplierDto>> GetProductSuppliersBySupplierIdAsync(Guid supplierId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product-supplier relationship by ID.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the preferred supplier for a product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preferred product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> GetPreferredSupplierAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product-supplier relationship.
    /// Enforces business rules:
    /// - Only one preferred supplier per product (auto-resets existing preferred)
    /// - Bundle products cannot have suppliers
    /// - Supplier must be Fornitore or ClienteFornitore type
    /// </summary>
    /// <param name="createProductSupplierDto">Product supplier creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product supplier DTO</returns>
    Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product-supplier relationship.
    /// Enforces business rules:
    /// - Only one preferred supplier per product (auto-resets existing preferred)
    /// - Bundle products cannot have suppliers
    /// - Supplier must be Fornitore or ClienteFornitore type
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="updateProductSupplierDto">Product supplier update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product-supplier relationship (soft delete).
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}
