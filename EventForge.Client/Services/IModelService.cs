using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing models.
/// </summary>
public interface IModelService
{
    /// <summary>
    /// Gets all models with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of models</returns>
    Task<PagedResult<ModelDto>> GetModelsAsync(int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets models by brand ID with optional pagination.
    /// </summary>
    /// <param name="brandId">Brand ID to filter by</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of models for the brand</returns>
    Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>Model DTO or null if not found</returns>
    Task<ModelDto?> GetModelByIdAsync(Guid id);

    /// <summary>
    /// Creates a new model.
    /// </summary>
    /// <param name="createModelDto">Model creation data</param>
    /// <returns>Created model DTO</returns>
    Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto);

    /// <summary>
    /// Updates an existing model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="updateModelDto">Model update data</param>
    /// <returns>Updated model DTO or null if not found</returns>
    Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto);

    /// <summary>
    /// Deletes a model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteModelAsync(Guid id);
}
