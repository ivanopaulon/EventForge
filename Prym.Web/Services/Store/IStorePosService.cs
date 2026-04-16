using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service interface for managing store POS terminals.
/// </summary>
public interface IStorePosService
{
    /// <summary>
    /// Gets all store POS terminals.
    /// </summary>
    Task<List<StorePosDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all active store POS terminals.
    /// </summary>
    Task<List<StorePosDto>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a store POS by ID.
    /// </summary>
    Task<StorePosDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new store POS.
    /// </summary>
    Task<StorePosDto?> CreateAsync(CreateStorePosDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing store POS.
    /// </summary>
    Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a store POS.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets store POS terminals with pagination.
    /// </summary>
    Task<PagedResult<StorePosDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}
