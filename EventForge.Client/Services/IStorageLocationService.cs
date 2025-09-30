using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing storage locations.
/// </summary>
public interface IStorageLocationService
{
    Task<PagedResult<StorageLocationDto>?> GetStorageLocationsAsync(int page = 1, int pageSize = 100);
}
