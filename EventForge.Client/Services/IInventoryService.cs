using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing inventory operations.
/// </summary>
public interface IInventoryService
{
    Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto);
}
