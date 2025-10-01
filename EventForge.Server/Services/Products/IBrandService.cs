using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service interface for managing product brands.
/// </summary>
public interface IBrandService
{
    /// <summary>
    /// Gets all brands with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of brands</returns>
    Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a brand by ID.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Brand DTO or null if not found</returns>
    Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new brand.
    /// </summary>
    /// <param name="createBrandDto">Brand creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created brand DTO</returns>
    Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing brand.
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="updateBrandDto">Brand update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated brand DTO or null if not found</returns>
    Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a brand (soft delete).
    /// </summary>
    /// <param name="id">Brand ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBrandAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a brand exists.
    /// </summary>
    /// <param name="brandId">Brand ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> BrandExistsAsync(Guid brandId, CancellationToken cancellationToken = default);
}
