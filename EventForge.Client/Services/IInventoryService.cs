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

    // Document-based inventory operations
    Task<InventoryDocumentDto?> StartInventoryDocumentAsync(CreateInventoryDocumentDto createDto);
    Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto);
    Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId);
    Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId);
    Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null);
}
