using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing inventory operations.
/// </summary>
public interface IInventoryService
{
    Task<PagedResult<InventoryEntryDto>?> GetInventoryEntriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto, CancellationToken ct = default);

    // Document-based inventory operations
    Task<InventoryDocumentDto?> StartInventoryDocumentAsync(CreateInventoryDocumentDto createDto, CancellationToken ct = default);
    Task<InventoryDocumentDto?> UpdateInventoryDocumentAsync(Guid documentId, UpdateInventoryDocumentDto updateDto, CancellationToken ct = default);
    Task<InventoryDocumentDto?> AddInventoryDocumentRowAsync(Guid documentId, AddInventoryDocumentRowDto rowDto, CancellationToken ct = default);
    Task<InventoryDocumentDto?> UpdateInventoryDocumentRowAsync(Guid documentId, Guid rowId, UpdateInventoryDocumentRowDto rowDto, CancellationToken ct = default);
    Task<InventoryDocumentDto?> DeleteInventoryDocumentRowAsync(Guid documentId, Guid rowId, CancellationToken ct = default);
    Task<InventoryDocumentDto?> FinalizeInventoryDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<InventoryDocumentDto?> GetInventoryDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<PagedResult<InventoryDocumentDto>?> GetInventoryDocumentsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, bool includeRows = false, CancellationToken ct = default);
    Task<InventoryDocumentDto?> GetMostRecentOpenInventoryDocumentAsync(CancellationToken ct = default);

    // Diagnostic and optimization operations
    Task<InventoryValidationResultDto?> ValidateInventoryDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<PagedResult<InventoryDocumentRowDto>?> GetInventoryDocumentRowsAsync(Guid documentId, int page = 1, int pageSize = 50, CancellationToken ct = default);

    // Active inventory management methods
    Task<List<InventoryDocumentDto>?> GetOpenInventoryDocumentsAsync(CancellationToken ct = default);
    /// <summary>
    /// Returns lightweight headers of all Open inventory documents.
    /// Uses the open-headers endpoint that never loads rows — safe for any number of documents.
    /// </summary>
    Task<List<InventoryDocumentHeaderDto>?> GetOpenInventoryDocumentHeadersAsync(CancellationToken ct = default);
    Task<bool> CancelInventoryDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<List<InventoryDocumentDto>?> FinalizeAllOpenInventoriesAsync(CancellationToken ct = default);
    Task<int> CancelAllOpenInventoriesAsync(CancellationToken ct = default);
    Task<InventoryDocumentDto?> MergeInventoryDocumentsAsync(List<Guid> sourceDocumentIds, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Returns a preview of what would happen if the specified documents were merged.
    /// Does NOT modify any data.
    /// </summary>
    Task<MergeInventoryDocumentsPreviewDto?> PreviewMergeInventoryDocumentsAsync(List<Guid> documentIds, CancellationToken ct = default);

    /// <summary>
    /// Merges inventory documents using extended options (target document, notes).
    /// Source documents are soft-deleted. Result document is finalized.
    /// </summary>
    Task<MergeInventoryDocumentsResultDto?> MergeInventoryDocumentsExtendedAsync(MergeInventoryDocumentsDto mergeDto, CancellationToken ct = default);

    // Inventory Diagnostics
    Task<InventoryDiagnosticReportDto?> DiagnoseInventoryDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<InventoryRepairResultDto?> AutoRepairInventoryDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, CancellationToken ct = default);
    Task<bool> RepairInventoryRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, CancellationToken ct = default);
    Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, CancellationToken ct = default);
}
