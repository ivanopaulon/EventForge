using EventForge.DTOs.Warehouse;

namespace EventForge.Services.Warehouse;

/// <summary>
/// Service interface for managing storage facilities.
/// </summary>
public interface IStorageFacilityService
{
    /// <summary>
    /// Gets all storage facilities with optional pagination.
    /// </summary>
    Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

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
}