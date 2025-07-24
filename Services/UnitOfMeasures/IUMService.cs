using EventForge.DTOs.UnitOfMeasures;

namespace EventForge.Services.UnitOfMeasures;

/// <summary>
/// Service interface for managing units of measure.
/// </summary>
public interface IUMService
{
    /// <summary>
    /// Gets all units of measure with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of units of measure</returns>
    Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unit of measure DTO or null if not found</returns>
    Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="createUMDto">Unit of measure creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created unit of measure DTO</returns>
    Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="updateUMDto">Unit of measure update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated unit of measure DTO or null if not found</returns>
    Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a unit of measure (soft delete).
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteUMAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a unit of measure exists.
    /// </summary>
    /// <param name="umId">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> UMExistsAsync(Guid umId, CancellationToken cancellationToken = default);
}