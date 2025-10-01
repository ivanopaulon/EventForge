using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing storage locations.
/// </summary>
public interface IStorageLocationService
{
    /// <summary>
    /// Gets all storage locations with pagination.
    /// </summary>
    Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100);
    
    /// <summary>
    /// Gets a specific storage location by ID.
    /// </summary>
    Task<StorageLocationDto?> GetStorageLocationAsync(Guid id);
    
    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    Task<StorageLocationDto?> CreateStorageLocationAsync(CreateStorageLocationDto dto);
    
    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto dto);
    
    /// <summary>
    /// Deletes a storage location.
    /// </summary>
    Task<bool> DeleteStorageLocationAsync(Guid id);
}
