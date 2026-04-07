using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing brands.
/// </summary>
public interface IBrandService
{
    /// <summary>
    /// Gets all brands with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of brands</returns>
    Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets all brands as a simple list.
    /// Note: Returns all brands since BrandDto doesn't have an IsActive property.
    /// Cached for performance optimization.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all brands</returns>
    Task<IEnumerable<BrandDto>> GetActiveBrandsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a brand by ID.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Brand DTO or null if not found</returns>
    Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new brand.
    /// </summary>
    /// <param name="createBrandDto">Brand creation data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created brand DTO</returns>
    Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="updateBrandDto">Brand update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated brand DTO or null if not found</returns>
    Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBrandAsync(Guid id, CancellationToken ct = default);
}
