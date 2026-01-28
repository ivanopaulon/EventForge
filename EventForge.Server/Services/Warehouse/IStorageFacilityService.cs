using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing storage facilities.
/// </summary>
public interface IStorageFacilityService
{
    /// <summary>
    /// Gets all storage facilities with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page number and page size)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of storage facilities</returns>
    Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a storage facility by ID.
    /// </summary>
    Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new storage facility.
    /// </summary>
    Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing storage facility.
    /// </summary>
    Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a storage facility (soft delete).
    /// </summary>
    Task<bool> DeleteStorageFacilityAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a storage facility exists.
    /// </summary>
    Task<bool> StorageFacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get storage facilities (warehouses) for export with batch processing support
    /// </summary>
    Task<IEnumerable<EventForge.DTOs.Export.WarehouseExportDto>> GetWarehousesForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default);
}