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
    Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets storage locations for a specific warehouse with pagination.
    /// </summary>
    Task<PagedResult<StorageLocationDto>?> GetStorageLocationsByWarehouseAsync(Guid warehouseId, int page = 1, int pageSize = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific storage location by ID.
    /// </summary>
    Task<StorageLocationDto?> GetStorageLocationAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    Task<StorageLocationDto?> CreateStorageLocationAsync(CreateStorageLocationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a storage location.
    /// </summary>
    Task<bool> DeleteStorageLocationAsync(Guid id, CancellationToken ct = default);
}
