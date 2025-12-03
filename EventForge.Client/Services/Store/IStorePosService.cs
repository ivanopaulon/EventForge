using EventForge.DTOs.Store;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service interface for managing store POS terminals.
/// </summary>
public interface IStorePosService
{
    /// <summary>
    /// Gets all store POS terminals.
    /// </summary>
    Task<List<StorePosDto>> GetAllAsync();

    /// <summary>
    /// Gets all active store POS terminals.
    /// </summary>
    Task<List<StorePosDto>> GetActiveAsync();

    /// <summary>
    /// Gets a store POS by ID.
    /// </summary>
    Task<StorePosDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new store POS.
    /// </summary>
    Task<StorePosDto?> CreateAsync(CreateStorePosDto createDto);

    /// <summary>
    /// Updates an existing store POS.
    /// </summary>
    Task<StorePosDto?> UpdateAsync(Guid id, UpdateStorePosDto updateDto);

    /// <summary>
    /// Deletes a store POS.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
