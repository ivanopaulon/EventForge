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
    /// <returns>Paginated list of product-supplier relationships</returns>
    Task<PagedResult<ProductSupplierDto>> GetProductSuppliersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all supplier relationships for a specific product.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of supplier relationships for the product</returns>
    Task<IEnumerable<ProductSupplierDto>> GetSuppliersByProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all product relationships for a specific supplier.
    /// </summary>
    /// <param name="supplierId">Supplier ID (BusinessParty)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of product relationships for the supplier</returns>
    Task<IEnumerable<ProductSupplierDto>> GetProductsBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product-supplier relationship by ID.
    /// </summary>
    /// <param name="id">Product-supplier relationship ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product-supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product-supplier relationship with business rule validation.
    /// Validates: Bundle restriction, Supplier type, Preferred supplier uniqueness
    /// </summary>
    /// <param name="createProductSupplierDto">Product-supplier creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product-supplier DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
    Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product-supplier relationship with business rule validation.
    /// Validates: Bundle restriction, Supplier type, Preferred supplier uniqueness
    /// </summary>
    /// <param name="id">Product-supplier relationship ID</param>
    /// <param name="updateProductSupplierDto">Product-supplier update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product-supplier DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product-supplier relationship (soft delete).
    /// </summary>
    /// <param name="id">Product-supplier relationship ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteProductSupplierAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product-supplier relationship exists.
    /// </summary>
    /// <param name="productSupplierId">Product-supplier relationship ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ProductSupplierExistsAsync(Guid productSupplierId, CancellationToken cancellationToken = default);
}
