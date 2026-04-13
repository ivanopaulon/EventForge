using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing storage facilities (warehouses).
/// </summary>
public interface IWarehouseService
{
    /// <summary>
    /// Gets all storage facilities with pagination.
    /// </summary>
    Task<PagedResult<StorageFacilityDto>?> GetStorageFacilitiesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific storage facility by ID.
    /// </summary>
    Task<StorageFacilityDto?> GetStorageFacilityAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new storage facility.
    /// </summary>
    Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing storage facility.
    /// </summary>
    Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a storage facility.
    /// </summary>
    Task<bool> DeleteStorageFacilityAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Performs a bulk transfer of items between warehouses.
    /// </summary>
    Task<Prym.DTOs.Bulk.BulkTransferResultDto?> BulkTransferAsync(Prym.DTOs.Bulk.BulkTransferDto bulkTransferDto, CancellationToken ct = default);
}
