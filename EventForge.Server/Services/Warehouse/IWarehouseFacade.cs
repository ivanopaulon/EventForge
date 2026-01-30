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
    /// <param name="createDto">Storage facility creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created storage facility DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a storage facility by its unique identifier.
    /// </summary>
    /// <param name="id">Storage facility unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Storage facility DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of storage facilities.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of storage facilities</returns>
    /// <exception cref="ArgumentNullException">Thrown when pagination is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves warehouses formatted for export operations.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Collection of warehouses formatted for export</returns>
    /// <exception cref="ArgumentNullException">Thrown when pagination is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<WarehouseExportDto>> GetWarehousesForExportAsync(PaginationParameters pagination, CancellationToken ct = default);

    #endregion

    #region Storage Location Operations

    /// <summary>
    /// Retrieves a storage location by its unique identifier.
    /// </summary>
    /// <param name="id">Storage location unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Storage location DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new storage location within a facility.
    /// </summary>
    /// <param name="createDto">Storage location creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created storage location DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of storage locations, optionally filtered by warehouse.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="warehouseId">Optional warehouse unique identifier to filter locations</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of storage locations</returns>
    /// <exception cref="ArgumentNullException">Thrown when pagination is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default);

    #endregion

    #region Lot Operations

    /// <summary>
    /// Blocks a lot from being used with a specified reason.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="reason">Reason for blocking the lot</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if blocked successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when reason or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new lot for batch/serial tracking.
    /// </summary>
    /// <param name="createDto">Lot creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created lot DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lot by its unique identifier.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves lots that are expiring within the specified number of days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead for expiring lots</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of expiring lots</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lot by its code.
    /// </summary>
    /// <param name="code">Lot code</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Lot DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when code is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a lot by its unique identifier.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Lot DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of lots with optional filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="productId">Optional product unique identifier to filter lots</param>
    /// <param name="status">Optional status to filter lots</param>
    /// <param name="expiringSoon">Optional flag to filter lots expiring soon</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of lots</returns>
    /// <exception cref="ArgumentNullException">Thrown when pagination is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<LotDto>> GetLotsAsync(PaginationParameters pagination, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks a previously blocked lot.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if unblocked successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing lot.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="updateDto">Lot update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated lot DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when updateDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the quality status of a lot.
    /// </summary>
    /// <param name="id">Lot unique identifier</param>
    /// <param name="qualityStatus">New quality status</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="notes">Optional notes about the quality status change</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if updated successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when qualityStatus or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Operations

    /// <summary>
    /// Updates the last inventory date for a stock entry.
    /// </summary>
    /// <param name="stockId">Stock unique identifier</param>
    /// <param name="inventoryDate">New inventory date</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates stock with the specified details.
    /// </summary>
    /// <param name="createDto">Stock creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created or updated stock DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates stock with the specified details (alternative overload).
    /// </summary>
    /// <param name="dto">Stock creation or update data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created or updated stock DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockDto> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of stock entries with optional filtering.
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="productId">Optional product unique identifier to filter stock</param>
    /// <param name="locationId">Optional location unique identifier to filter stock</param>
    /// <param name="lotId">Optional lot unique identifier to filter stock</param>
    /// <param name="lowStock">Optional flag to filter low stock items</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of stock entries</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<StockDto>> GetStockAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adjusts stock quantity with a reason.
    /// </summary>
    /// <param name="dto">Stock adjustment data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Adjusted stock DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when dto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a comprehensive stock overview with filtering and detailed view options.
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term to filter products</param>
    /// <param name="warehouseId">Optional warehouse unique identifier to filter stock</param>
    /// <param name="locationId">Optional location unique identifier to filter stock</param>
    /// <param name="lotId">Optional lot unique identifier to filter stock</param>
    /// <param name="lowStock">Optional flag to filter low stock items</param>
    /// <param name="criticalStock">Optional flag to filter critical stock items</param>
    /// <param name="outOfStock">Optional flag to filter out of stock items</param>
    /// <param name="inStockOnly">Optional flag to show only in-stock items</param>
    /// <param name="showAllProducts">Optional flag to show all products including zero stock</param>
    /// <param name="detailedView">Flag to enable detailed view with additional information</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated stock overview with location details</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<StockLocationDetail>> GetStockOverviewAsync(int page = 1, int pageSize = 20, string? searchTerm = null, Guid? warehouseId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, bool? criticalStock = null, bool? outOfStock = null, bool? inStockOnly = null, bool? showAllProducts = null, bool detailedView = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves stock for a product at a specific location.
    /// </summary>
    /// <param name="productId">Product unique identifier</param>
    /// <param name="locationId">Location unique identifier</param>
    /// <param name="quantity">Quantity to reserve</param>
    /// <param name="lotId">Optional lot unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if reserved successfully, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid or insufficient stock</exception>
    Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a stock entry by its unique identifier.
    /// </summary>
    /// <param name="id">Stock unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Stock DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all stock entries for a specific product.
    /// </summary>
    /// <param name="productId">Product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Collection of stock entries for the product</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    #endregion

    #region Serial Operations

    /// <summary>
    /// Creates a new serial number for tracking individual items.
    /// </summary>
    /// <param name="createDto">Serial number creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created serial number DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of serial numbers with optional filtering.
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="productId">Optional product unique identifier to filter serials</param>
    /// <param name="lotId">Optional lot unique identifier to filter serials</param>
    /// <param name="locationId">Optional location unique identifier to filter serials</param>
    /// <param name="status">Optional status to filter serials</param>
    /// <param name="searchTerm">Optional search term to filter serials</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of serial numbers</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<SerialDto>> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, Guid? locationId = null, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a serial number.
    /// </summary>
    /// <param name="id">Serial unique identifier</param>
    /// <param name="status">New status</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="notes">Optional notes about the status change</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if updated successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when status or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a serial number by its unique identifier.
    /// </summary>
    /// <param name="id">Serial unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Serial number DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Movement Operations

    /// <summary>
    /// Processes a stock adjustment movement.
    /// </summary>
    /// <param name="productId">Product unique identifier</param>
    /// <param name="locationId">Location unique identifier</param>
    /// <param name="adjustmentQuantity">Quantity to adjust (positive or negative)</param>
    /// <param name="reason">Reason for the adjustment</param>
    /// <param name="lotId">Optional lot unique identifier</param>
    /// <param name="notes">Optional notes about the movement</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="movementDate">Optional movement date (defaults to current date)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created stock movement DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when reason is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockMovementDto> ProcessAdjustmentMovementAsync(Guid productId, Guid locationId, decimal adjustmentQuantity, string reason, Guid? lotId = null, string? notes = null, string? currentUser = null, DateTime? movementDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves inventory data formatted for export operations.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Collection of inventory data formatted for export</returns>
    /// <exception cref="ArgumentNullException">Thrown when pagination is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IEnumerable<InventoryExportDto>> GetInventoryForExportAsync(PaginationParameters pagination, CancellationToken ct = default);

    #endregion

    #region Document Operations

    /// <summary>
    /// Closes a document and finalizes its state.
    /// </summary>
    /// <param name="id">Document unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Closed document header DTO or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a document header by its unique identifier, optionally including rows.
    /// </summary>
    /// <param name="id">Document header unique identifier</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Document header DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new row to a document.
    /// </summary>
    /// <param name="createDto">Document row creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created document row DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentRowDto> AddDocumentRowAsync(CreateDocumentRowDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created document header DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when createDto or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentHeaderDto> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Paginated list of document headers</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryParameters is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates the inventory document type for the specified tenant.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Inventory document type DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates the system business party for the specified tenant.
    /// </summary>
    /// <param name="tenantId">Tenant unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>System business party unique identifier</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<Guid> GetOrCreateSystemBusinessPartyAsync(Guid tenantId, CancellationToken cancellationToken = default);

    #endregion

    #region Product Operations

    /// <summary>
    /// Retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Product DTO or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Bulk Operations

    /// <summary>
    /// Seeds inventory with bulk data for testing or initialization.
    /// </summary>
    /// <param name="request">Inventory seed request with bulk data</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the inventory seeding operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when request or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<InventorySeedResultDto> SeedInventoryAsync(InventorySeedRequestDto request, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Diagnostic Operations

    /// <summary>
    /// Removes problematic rows from an inventory document.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="rowIds">List of row unique identifiers to remove</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of rows removed</returns>
    /// <exception cref="ArgumentNullException">Thrown when rowIds or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnoses an inventory document for issues.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Diagnostic report with identified issues</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically repairs an inventory document based on specified options.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="options">Auto-repair options specifying what to fix</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the auto-repair operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when options or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<InventoryRepairResultDto> AutoRepairDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Repairs a specific row in an inventory document.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="rowId">Row unique identifier</param>
    /// <param name="repairData">Repair data with corrected values</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if repaired successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when repairData or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> RepairRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Stock Reconciliation Operations

    /// <summary>
    /// Exports a stock reconciliation report.
    /// </summary>
    /// <param name="request">Reconciliation request parameters</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Exported report as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<byte[]> ExportReconciliationReportAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies stock reconciliation adjustments.
    /// </summary>
    /// <param name="request">Apply reconciliation request with adjustments</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Result of the reconciliation apply operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when request or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates reconciled stock based on the request parameters.
    /// </summary>
    /// <param name="request">Reconciliation request parameters</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Reconciliation result with calculated stock differences</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<StockReconciliationResultDto> CalculateReconciledStockAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default);

    #endregion

    #region Export Operations

    /// <summary>
    /// Exports data to CSV format.
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>CSV file as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Exports data to Excel format with a specified sheet name.
    /// </summary>
    /// <typeparam name="T">Type of data to export</typeparam>
    /// <param name="data">Collection of data to export</param>
    /// <param name="sheetName">Name of the Excel sheet</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Excel file as byte array</returns>
    /// <exception cref="ArgumentNullException">Thrown when data or sheetName is null</exception>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken ct = default) where T : class;

    #endregion

    #region Inventory Row Management Operations

    /// <summary>
    /// Gets unit of measure symbol for a product.
    /// </summary>
    /// <param name="unitOfMeasureId">Unit of measure unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Unit of measure symbol or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<string?> GetUnitOfMeasureSymbolAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets VAT rate details for a product.
    /// </summary>
    /// <param name="vatRateId">VAT rate unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>VAT rate percentage and description or null if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<(decimal Percentage, string? Description)?> GetVatRateDetailsAsync(Guid vatRateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing inventory document row by merging quantities or creating new row.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="existingRowId">Existing row unique identifier</param>
    /// <param name="newQuantity">New quantity to merge</param>
    /// <param name="additionalNotes">Additional notes to append</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated or created document row DTO</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<DocumentRowDto> UpdateOrMergeInventoryRowAsync(Guid documentId, Guid existingRowId, decimal newQuantity, string? additionalNotes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an inventory document header fields.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="date">Document date</param>
    /// <param name="warehouseId">Warehouse unique identifier</param>
    /// <param name="notes">Document notes</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if updated successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UpdateDocumentHeaderFieldsAsync(Guid documentId, DateTime date, Guid? warehouseId, string? notes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an inventory row with product, quantity, location, and notes.
    /// </summary>
    /// <param name="rowId">Row unique identifier</param>
    /// <param name="productId">Product unique identifier</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="locationId">Location unique identifier</param>
    /// <param name="notes">Row notes</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if updated successfully, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> UpdateInventoryRowAsync(Guid rowId, Guid? productId, decimal quantity, Guid? locationId, string? notes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an inventory row.
    /// </summary>
    /// <param name="rowId">Row unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if deleted, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> DeleteInventoryRowAsync(Guid rowId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all product IDs exist in the database.
    /// </summary>
    /// <param name="productIds">List of product unique identifiers to validate</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of product IDs that exist</returns>
    /// <exception cref="ArgumentNullException">Thrown when productIds is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<Guid>> ValidateProductsExistAsync(List<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all location IDs exist in the database.
    /// </summary>
    /// <param name="locationIds">List of location unique identifiers to validate</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of location IDs that exist</returns>
    /// <exception cref="ArgumentNullException">Thrown when locationIds is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<Guid>> ValidateLocationsExistAsync(List<Guid> locationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an inventory document by updating its status.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if cancelled, false if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<bool> CancelInventoryDocumentAsync(Guid documentId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inventory document rows with pagination for a specific document.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing list of rows and total count</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<(List<DocumentRowDto> Rows, int TotalCount)> GetDocumentRowsPagedAsync(Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels multiple inventory documents in batch.
    /// </summary>
    /// <param name="documentIds">List of document unique identifiers to cancel</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of documents cancelled</returns>
    /// <exception cref="ArgumentNullException">Thrown when documentIds or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<int> CancelInventoryDocumentsBatchAsync(List<Guid> documentIds, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads inventory documents with rows for merge operation.
    /// </summary>
    /// <param name="documentIds">List of document unique identifiers to load</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of tuples containing document information and rows</returns>
    /// <exception cref="ArgumentNullException">Thrown when documentIds is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<(Guid Id, DocumentStatus Status, Guid? SourceWarehouseId, List<DocumentRowDto> Rows, string Number, string? Notes)>> LoadDocumentsForMergeAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple document statuses in batch and saves changes.
    /// </summary>
    /// <param name="updates">List of tuples containing document ID, status, and notes</param>
    /// <param name="currentUser">Current user identifier for audit logging</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <exception cref="ArgumentNullException">Thrown when updates or currentUser is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task UpdateDocumentStatusesBatchAsync(List<(Guid DocumentId, DocumentStatus Status, string Notes)> updates, string currentUser, CancellationToken cancellationToken = default);

    #endregion

    #region Inventory Validation Operations

    /// <summary>
    /// Counts total rows for an inventory document.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Total count of document rows</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<int> CountDocumentRowsAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets rows with null ProductId or LocationId for validation.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of tuples containing row ID, product ID, and location ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<(Guid Id, Guid? ProductId, Guid? LocationId)>> GetRowsWithNullDataAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unique product and location IDs from document rows.
    /// </summary>
    /// <param name="documentId">Document unique identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Tuple containing lists of unique product IDs and location IDs</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<(List<Guid> ProductIds, List<Guid> LocationIds)> GetUniqueProductAndLocationIdsAsync(Guid documentId, CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Operations

    /// <summary>
    /// Begins a database transaction with the specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">Transaction isolation level</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Database context transaction</returns>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    #endregion

    #region Helper Operations

    /// <summary>
    /// Enriches inventory document rows with complete product and location data using optimized batch queries.
    /// Solves N+1 query problem by fetching all related data in 3 batch queries instead of N queries per row.
    /// Performance: 500 rows = 3 queries (~5 seconds) vs 1500 queries (~60+ seconds with old method).
    /// </summary>
    /// <param name="rows">Collection of document rows to enrich</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of enriched inventory document rows with complete product and location data</returns>
    /// <exception cref="ArgumentNullException">Thrown when rows is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when tenant context is invalid</exception>
    Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync(IEnumerable<DocumentRowDto> rows, CancellationToken cancellationToken = default);

    #endregion
}
