using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service interface for managing product models.
/// </summary>
public interface IModelService
{
    /// <summary>
    /// Gets all models with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models</returns>
    Task<PagedResult<ModelDto>> GetModelsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets models by brand ID with pagination.
    /// </summary>
    /// <param name="brandId">Brand ID to filter by</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models for the brand</returns>
    Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model DTO or null if not found</returns>
    Task<ModelDto?> GetModelByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new model.
    /// </summary>
    /// <param name="createModelDto">Model creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created model DTO</returns>
    Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="updateModelDto">Model update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated model DTO or null if not found</returns>
    Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a model (soft delete).
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteModelAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model exists.
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ModelExistsAsync(Guid modelId, CancellationToken cancellationToken = default);
}
