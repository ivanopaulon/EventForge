using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing storage locations within warehouses.
/// </summary>
public interface IStorageLocationService
{
    /// <summary>
    /// Gets all storage locations with pagination and warehouse filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page number and page size)</param>
    /// <param name="warehouseId">Optional warehouse ID to filter locations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of storage locations</returns>
    Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a storage location by ID.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage location or null if not found</returns>
    Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all storage locations for a specific warehouse.
    /// </summary>
    /// <param name="warehouseId">Warehouse ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of storage locations</returns>
    Task<IEnumerable<StorageLocationDto>> GetLocationsByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available storage locations (with remaining capacity).
    /// </summary>
    /// <param name="warehouseId">Optional warehouse ID to filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available storage locations</returns>
    Task<IEnumerable<StorageLocationDto>> GetAvailableLocationsAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new storage location.
    /// </summary>
    /// <param name="createDto">Storage location creation data</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created storage location</returns>
    Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="updateDto">Storage location update data</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated storage location or null if not found</returns>
    Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="rowVersion">Row version for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStorageLocationAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the occupancy of a storage location.
    /// </summary>
    /// <param name="id">Storage location ID</param>
    /// <param name="newOccupancy">New occupancy value</param>
    /// <param name="currentUser">Current user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated storage location or null if not found</returns>
    Task<StorageLocationDto?> UpdateOccupancyAsync(Guid id, int newOccupancy, string currentUser, CancellationToken cancellationToken = default);
}