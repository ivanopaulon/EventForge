using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing inventory operations.
/// </summary>
public interface IInventoryService
{
    Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20);
    Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto);
}
