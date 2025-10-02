using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing product suppliers.
/// </summary>
public interface IProductSupplierService
{
    /// <summary>
    /// Gets all product suppliers with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of product suppliers</returns>
    Task<PagedResult<ProductSupplierDto>> GetProductSuppliersAsync(int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets a product supplier by ID.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <returns>Product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> GetProductSupplierByIdAsync(Guid id);

    /// <summary>
    /// Creates a new product supplier.
    /// </summary>
    /// <param name="createProductSupplierDto">Product supplier creation data</param>
    /// <returns>Created product supplier DTO</returns>
    Task<ProductSupplierDto> CreateProductSupplierAsync(CreateProductSupplierDto createProductSupplierDto);

    /// <summary>
    /// Updates an existing product supplier.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <param name="updateProductSupplierDto">Product supplier update data</param>
    /// <returns>Updated product supplier DTO or null if not found</returns>
    Task<ProductSupplierDto?> UpdateProductSupplierAsync(Guid id, UpdateProductSupplierDto updateProductSupplierDto);

    /// <summary>
    /// Deletes a product supplier.
    /// </summary>
    /// <param name="id">Product supplier ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteProductSupplierAsync(Guid id);
}
