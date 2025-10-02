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
    /// <returns>Paginated list of brands</returns>
    Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets a brand by ID.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <returns>Brand DTO or null if not found</returns>
    Task<BrandDto?> GetBrandByIdAsync(Guid id);

    /// <summary>
    /// Creates a new brand.
    /// </summary>
    /// <param name="createBrandDto">Brand creation data</param>
    /// <returns>Created brand DTO</returns>
    Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto);

    /// <summary>
    /// Updates an existing brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="updateBrandDto">Brand update data</param>
    /// <returns>Updated brand DTO or null if not found</returns>
    Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto);

    /// <summary>
    /// Deletes a brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBrandAsync(Guid id);
}
