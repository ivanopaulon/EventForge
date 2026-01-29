using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Export;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Unified facade for warehouse management operations.
/// Consolidates access to storage, stock, inventory, and related services to reduce controller dependencies.
/// </summary>
public interface IWarehouseFacade
{
    #region Storage Facility Operations

    /// <summary>
    /// Creates a new storage facility (warehouse).
    /// </summary>
    Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a storage facility by its unique identifier.
    /// </summary>
    Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of storage facilities.
    /// </summary>
    Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves warehouses formatted for export operations.
    /// </summary>
    Task<IEnumerable<WarehouseExportDto>> GetWarehousesForExportAsync(PaginationParameters pagination, CancellationToken ct = default);

    #endregion

    #region Storage Location Operations

    /// <summary>
    /// Retrieves a storage location by its unique identifier.
    /// </summary>
    Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new storage location within a facility.
    /// </summary>
    Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of storage locations, optionally filtered by warehouse.
    /// </summary>
    Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Lot Operations

    /// <summary>
    /// Blocks a lot from being used with a specified reason.
    /// </summary>
    Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new lot for batch/serial tracking.
    /// </summary>
    Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lot by its unique identifier.
    /// </summary>
    Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves lots that are expiring within the specified number of days.
    /// </summary>
    Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lot by its code.
    /// </summary>
    Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lot by its unique identifier.
    /// </summary>
    Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of lots with optional filtering.
    /// </summary>
    Task<PagedResult<LotDto>> GetLotsAsync(PaginationParameters pagination, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks a previously blocked lot.
    /// </summary>
    Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing lot.
    /// </summary>
    Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the quality status of a lot.
    /// </summary>
    Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Operations

    /// <summary>
    /// Updates the last inventory date for a stock entry.
    /// </summary>
    Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates stock with the specified details.
    /// </summary>
    Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates stock with the specified details (alternative overload).
    /// </summary>
    Task<StockDto> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of stock entries with optional filtering.
    /// </summary>
    Task<PagedResult<StockDto>> GetStockAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts stock quantity with a reason.
    /// </summary>
    Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a comprehensive stock overview with filtering and detailed view options.
    /// </summary>
    Task<PagedResult<StockLocationDetail>> GetStockOverviewAsync(int page = 1, int pageSize = 20, string? searchTerm = null, Guid? warehouseId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, bool? criticalStock = null, bool? outOfStock = null, bool? inStockOnly = null, bool? showAllProducts = null, bool detailedView = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves stock for a product at a specific location.
    /// </summary>
    Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a stock entry by its unique identifier.
    /// </summary>
    Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all stock entries for a specific product.
    /// </summary>
    Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    #endregion

    #region Serial Operations

    /// <summary>
    /// Creates a new serial number for tracking individual items.
    /// </summary>
    Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of serial numbers with optional filtering.
    /// </summary>
    Task<PagedResult<SerialDto>> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, Guid? locationId = null, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a serial number.
    /// </summary>
    Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a serial number by its unique identifier.
    /// </summary>
    Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Movement Operations

    /// <summary>
    /// Processes a stock adjustment movement.
    /// </summary>
    Task<StockMovementDto> ProcessAdjustmentMovementAsync(Guid productId, Guid locationId, decimal adjustmentQuantity, string reason, Guid? lotId = null, string? notes = null, string? currentUser = null, DateTime? movementDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves inventory data formatted for export operations.
    /// </summary>
    Task<IEnumerable<InventoryExportDto>> GetInventoryForExportAsync(PaginationParameters pagination, CancellationToken ct = default);

    #endregion

    #region Document Operations

    /// <summary>
    /// Closes a document and finalizes its state.
    /// </summary>
    Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a document header by its unique identifier, optionally including rows.
    /// </summary>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new row to a document.
    /// </summary>
    Task<DocumentRowDto> AddDocumentRowAsync(CreateDocumentRowDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    Task<DocumentHeaderDto> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of document headers with optional filtering.
    /// </summary>
    Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates the inventory document type for the specified tenant.
    /// </summary>
    Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates the system business party for the specified tenant.
    /// </summary>
    Task<Guid> GetOrCreateSystemBusinessPartyAsync(Guid tenantId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Operations

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Bulk Operations

    /// <summary>
    /// Seeds inventory with bulk data for testing or initialization.
    /// </summary>
    Task<InventorySeedResultDto> SeedInventoryAsync(InventorySeedRequestDto request, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Diagnostic Operations

    /// <summary>
    /// Removes problematic rows from an inventory document.
    /// </summary>
    Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnoses an inventory document for issues.
    /// </summary>
    Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically repairs an inventory document based on specified options.
    /// </summary>
    Task<InventoryRepairResultDto> AutoRepairDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Repairs a specific row in an inventory document.
    /// </summary>
    Task<bool> RepairRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Reconciliation Operations

    /// <summary>
    /// Exports a stock reconciliation report.
    /// </summary>
    Task<byte[]> ExportReconciliationReportAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies stock reconciliation adjustments.
    /// </summary>
    Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates reconciled stock based on the request parameters.
    /// </summary>
    Task<StockReconciliationResultDto> CalculateReconciledStockAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default);

    #endregion

    #region Export Operations

    /// <summary>
    /// Exports data to CSV format.
    /// </summary>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Exports data to Excel format with a specified sheet name.
    /// </summary>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken ct = default) where T : class;

    #endregion

    #region Inventory Row Management Operations

    /// <summary>
    /// Gets unit of measure symbol for a product.
    /// </summary>
    Task<string?> GetUnitOfMeasureSymbolAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets VAT rate details for a product.
    /// </summary>
    Task<(decimal Percentage, string? Description)?> GetVatRateDetailsAsync(Guid vatRateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inventory document row by merging quantities or creating new row.
    /// </summary>
    Task<DocumentRowDto> UpdateOrMergeInventoryRowAsync(Guid documentId, Guid existingRowId, decimal newQuantity, string? additionalNotes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an inventory document header fields.
    /// </summary>
    Task<bool> UpdateDocumentHeaderFieldsAsync(Guid documentId, DateTime date, Guid? warehouseId, string? notes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an inventory row with product, quantity, location, and notes.
    /// </summary>
    Task<bool> UpdateInventoryRowAsync(Guid rowId, Guid? productId, decimal quantity, Guid? locationId, string? notes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an inventory row.
    /// </summary>
    Task<bool> DeleteInventoryRowAsync(Guid rowId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all product IDs exist in the database.
    /// </summary>
    Task<List<Guid>> ValidateProductsExistAsync(List<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all location IDs exist in the database.
    /// </summary>
    Task<List<Guid>> ValidateLocationsExistAsync(List<Guid> locationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an inventory document by updating its status.
    /// </summary>
    Task<bool> CancelInventoryDocumentAsync(Guid documentId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory document rows with pagination for a specific document.
    /// </summary>
    Task<(List<DocumentRowDto> Rows, int TotalCount)> GetDocumentRowsPagedAsync(Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels multiple inventory documents in batch.
    /// </summary>
    Task<int> CancelInventoryDocumentsBatchAsync(List<Guid> documentIds, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads inventory documents with rows for merge operation.
    /// </summary>
    Task<List<(Guid Id, DocumentStatus Status, Guid? SourceWarehouseId, List<DocumentRowDto> Rows, string Number, string? Notes)>> LoadDocumentsForMergeAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple document statuses in batch and saves changes.
    /// </summary>
    Task UpdateDocumentStatusesBatchAsync(List<(Guid DocumentId, DocumentStatus Status, string Notes)> updates, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Validation Operations

    /// <summary>
    /// Counts total rows for an inventory document.
    /// </summary>
    Task<int> CountDocumentRowsAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rows with null ProductId or LocationId for validation.
    /// </summary>
    Task<List<(Guid Id, Guid? ProductId, Guid? LocationId)>> GetRowsWithNullDataAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unique product and location IDs from document rows.
    /// </summary>
    Task<(List<Guid> ProductIds, List<Guid> LocationIds)> GetUniqueProductAndLocationIdsAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Operations

    /// <summary>
    /// Begins a database transaction with the specified isolation level.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    #endregion

    #region Helper Operations

    /// <summary>
    /// Enriches inventory document rows with complete product and location data using optimized batch queries.
    /// Solves N+1 query problem by fetching all related data in 3 batch queries instead of N queries per row.
    /// Performance: 500 rows = 3 queries (~5 seconds) vs 1500 queries (~60+ seconds with old method).
    /// </summary>
    Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync(IEnumerable<DocumentRowDto> rows, CancellationToken cancellationToken = default);

    #endregion
}
