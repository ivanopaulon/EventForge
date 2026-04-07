using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;

namespace EventForge.Client.Services;

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
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of units of measure</returns>
    Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets all active units of measure as a simple list.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active units of measure</returns>
    Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unit of measure DTO or null if not found</returns>
    Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="createUMDto">Unit of measure creation data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created unit of measure DTO</returns>
    Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="updateUMDto">Unit of measure update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated unit of measure DTO or null if not found</returns>
    Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteUMAsync(Guid id, CancellationToken ct = default);
}
