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
    Task<InventoryDocumentDto?> UpdateInventoryDocumentAsync(Guid documentId, UpdateInventoryDocumentDto updateDto);
    Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto);
    Task<InventoryDocumentDto?> UpdateInventoryDocumentRowAsync(Guid documentId, Guid rowId, UpdateInventoryDocumentRowDto rowDto);
    Task<InventoryDocumentDto?> DeleteInventoryDocumentRowAsync(Guid documentId, Guid rowId);
    Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId);
    Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId);
    Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, bool includeRows = false);
    Task<InventoryDocumentDto?> GetMostRecentOpenInventoryDocumentAsync();

    // Diagnostic and optimization operations
    Task<InventoryValidationResultDto?> ValidateInventoryDocumentAsync(Guid documentId);
    Task<PagedResult<InventoryDocumentRowDto>?> GetInventoryDocumentRowsAsync(Guid documentId, int page = 1, int pageSize = 50);

    // Active inventory management methods
    Task<List<InventoryDocumentDto>?> GetOpenInventoryDocumentsAsync();
    Task<bool> CancelInventoryDocumentAsync(Guid documentId);
    Task<List<InventoryDocumentDto>?> FinalizeAllOpenInventoriesAsync();
    Task<int> CancelAllOpenInventoriesAsync();
    Task<InventoryDocumentDto?> MergeInventoryDocumentsAsync(List<Guid> sourceDocumentIds, string? notes = null);

    // Inventory Diagnostics
    Task<InventoryDiagnosticReportDto?> DiagnoseInventoryDocumentAsync(Guid documentId);
    Task<InventoryRepairResultDto?> AutoRepairInventoryDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options);
    Task<bool> RepairInventoryRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData);
    Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds);
}
