using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing storage facilities (warehouses).
/// </summary>
public interface IWarehouseService
{
    /// <summary>
    /// Gets all storage facilities with pagination.
    /// </summary>
    Task<PagedResult<StorageFacilityDto>?> GetStorageFacilitiesAsync(int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets a specific storage facility by ID.
    /// </summary>
    Task<StorageFacilityDto?> GetStorageFacilityAsync(Guid id);

    /// <summary>
    /// Creates a new storage facility.
    /// </summary>
    Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto);

    /// <summary>
    /// Updates an existing storage facility.
    /// </summary>
    Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto dto);

    /// <summary>
    /// Deletes a storage facility.
    /// </summary>
    Task<bool> DeleteStorageFacilityAsync(Guid id);
}
