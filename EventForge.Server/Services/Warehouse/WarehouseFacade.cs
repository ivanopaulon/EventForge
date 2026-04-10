using EventForge.DTOs.Documents;
using EventForge.DTOs.Export;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Unified facade implementation for warehouse management operations.
/// Consolidates access to storage, stock, inventory, and related services to reduce controller dependencies.
/// This facade delegates to underlying services and does not contain business logic.
/// </summary>
public class WarehouseFacade(
    IStorageFacilityService storageFacilityService,
    IStorageLocationService storageLocationService,
    ILotService lotService,
    IStockService stockService,
    ISerialService serialService,
    IStockMovementService stockMovementService,
    IDocumentHeaderService documentHeaderService,
    IProductService productService,
    IInventoryBulkSeedService inventoryBulkSeedService,
    IInventoryDiagnosticService inventoryDiagnosticService,
    IStockReconciliationService stockReconciliationService,
    IExportService exportService,
    EventForgeDbContext context,
    ILogger<WarehouseFacade> logger) : IWarehouseFacade
{

    #region Storage Facility Operations

    public Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => storageFacilityService.CreateStorageFacilityAsync(createDto, currentUser, cancellationToken);

    public Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => storageFacilityService.GetStorageFacilityByIdAsync(id, cancellationToken);

    public Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
        => storageFacilityService.GetStorageFacilitiesAsync(pagination, cancellationToken);

    public Task<IEnumerable<WarehouseExportDto>> GetWarehousesForExportAsync(PaginationParameters pagination, CancellationToken ct = default)
        => storageFacilityService.GetWarehousesForExportAsync(pagination, ct);

    #endregion

    #region Storage Location Operations

    public Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => storageLocationService.GetStorageLocationByIdAsync(id, cancellationToken);

    public Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => storageLocationService.CreateStorageLocationAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default)
        => storageLocationService.GetStorageLocationsAsync(pagination, warehouseId, cancellationToken);

    #endregion

    #region Lot Operations

    public Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default)
        => lotService.BlockLotAsync(id, reason, currentUser, cancellationToken);

    public Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => lotService.CreateLotAsync(createDto, currentUser, cancellationToken);

    public Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => lotService.DeleteLotAsync(id, currentUser, cancellationToken);

    public Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
        => lotService.GetExpiringLotsAsync(daysAhead, cancellationToken);

    public Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default)
        => lotService.GetLotByCodeAsync(code, cancellationToken);

    public Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => lotService.GetLotByIdAsync(id, cancellationToken);

    public Task<PagedResult<LotDto>> GetLotsAsync(PaginationParameters pagination, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken cancellationToken = default)
        => lotService.GetLotsAsync(pagination, productId, status, expiringSoon, cancellationToken);

    public Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => lotService.UnblockLotAsync(id, currentUser, cancellationToken);

    public Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default)
        => lotService.UpdateLotAsync(id, updateDto, currentUser, cancellationToken);

    public Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
        => lotService.UpdateQualityStatusAsync(id, qualityStatus, currentUser, notes, cancellationToken);

    #endregion

    #region Stock Operations

    public Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default)
        => stockService.UpdateLastInventoryDateAsync(stockId, inventoryDate, cancellationToken);

    public Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => stockService.CreateOrUpdateStockAsync(createDto, currentUser, cancellationToken);

    public Task<StockDto> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, string currentUser, CancellationToken cancellationToken = default)
        => stockService.CreateOrUpdateStockAsync(dto, currentUser, cancellationToken);

    public Task<PagedResult<StockDto>> GetStockAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, CancellationToken cancellationToken = default)
        => stockService.GetStockAsync(page, pageSize, productId, locationId, lotId, lowStock, cancellationToken);

    public Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default)
        => stockService.AdjustStockAsync(dto, currentUser, cancellationToken);

    public Task<PagedResult<StockLocationDetail>> GetStockOverviewAsync(int page = 1, int pageSize = 20, string? searchTerm = null, Guid? warehouseId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, bool? criticalStock = null, bool? outOfStock = null, bool? inStockOnly = null, bool? showAllProducts = null, bool detailedView = false, CancellationToken cancellationToken = default)
        => stockService.GetStockOverviewAsync(page, pageSize, searchTerm, warehouseId, locationId, lotId, lowStock, criticalStock, outOfStock, inStockOnly, showAllProducts, detailedView, cancellationToken);

    public Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
        => stockService.ReserveStockAsync(productId, locationId, quantity, lotId, currentUser, cancellationToken);

    public Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => stockService.GetStockByIdAsync(id, cancellationToken);

    public Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => stockService.GetStockByProductIdAsync(productId, cancellationToken);

    #endregion

    #region Serial Operations

    public Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => serialService.CreateSerialAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<SerialDto>> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, Guid? locationId = null, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
        => serialService.GetSerialsAsync(page, pageSize, productId, lotId, locationId, status, searchTerm, cancellationToken);

    public Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
        => serialService.UpdateSerialStatusAsync(id, status, currentUser, notes, cancellationToken);

    public Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => serialService.GetSerialByIdAsync(id, cancellationToken);

    #endregion

    #region Stock Movement Operations

    public Task<StockMovementDto> ProcessAdjustmentMovementAsync(Guid productId, Guid locationId, decimal adjustmentQuantity, string reason, Guid? lotId = null, string? notes = null, string? currentUser = null, DateTime? movementDate = null, CancellationToken cancellationToken = default)
        => stockMovementService.ProcessAdjustmentMovementAsync(productId, locationId, adjustmentQuantity, reason, lotId, notes, currentUser, movementDate, cancellationToken);

    public Task<IEnumerable<InventoryExportDto>> GetInventoryForExportAsync(PaginationParameters pagination, CancellationToken ct = default)
        => stockMovementService.GetInventoryForExportAsync(pagination, ct);

    #endregion

    #region Document Operations

    public Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);

    public Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken cancellationToken = default)
        => documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);

    public Task<DocumentRowDto> AddDocumentRowAsync(CreateDocumentRowDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => documentHeaderService.AddDocumentRowAsync(createDto, currentUser, cancellationToken);

    public Task<DocumentHeaderDto> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken cancellationToken = default)
        => documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

    public Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(tenantId, cancellationToken);

    public Task<Guid> GetOrCreateSystemBusinessPartyAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => documentHeaderService.GetOrCreateSystemBusinessPartyAsync(tenantId, cancellationToken);

    #endregion

    #region Product Operations

    public Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => productService.GetProductByIdAsync(id, cancellationToken);

    #endregion

    #region Inventory Bulk Operations

    public Task<InventorySeedResultDto> SeedInventoryAsync(InventorySeedRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        => inventoryBulkSeedService.SeedInventoryAsync(request, currentUser, cancellationToken);

    #endregion

    #region Inventory Diagnostic Operations

    public Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, string currentUser, CancellationToken cancellationToken = default)
        => inventoryDiagnosticService.RemoveProblematicRowsAsync(documentId, rowIds, currentUser, cancellationToken);

    public Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => inventoryDiagnosticService.DiagnoseDocumentAsync(documentId, cancellationToken);

    public Task<InventoryRepairResultDto> AutoRepairDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, string currentUser, CancellationToken cancellationToken = default)
        => inventoryDiagnosticService.AutoRepairDocumentAsync(documentId, options, currentUser, cancellationToken);

    public Task<bool> RepairRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, string currentUser, CancellationToken cancellationToken = default)
        => inventoryDiagnosticService.RepairRowAsync(documentId, rowId, repairData, currentUser, cancellationToken);

    #endregion

    #region Stock Reconciliation Operations

    public Task<byte[]> ExportReconciliationReportAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default)
        => stockReconciliationService.ExportReconciliationReportAsync(request, cancellationToken);

    public Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        => stockReconciliationService.ApplyReconciliationAsync(request, currentUser, cancellationToken);

    public Task<StockReconciliationResultDto> CalculateReconciledStockAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default)
        => stockReconciliationService.CalculateReconciledStockAsync(request, cancellationToken);

    public Task<RebuildMovementsResultDto> RebuildMissingMovementsFromDocumentsAsync(RebuildMovementsRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        => stockReconciliationService.RebuildMissingMovementsFromDocumentsAsync(request, currentUser, cancellationToken);

    #endregion

    #region Export Operations

    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken ct = default) where T : class
        => exportService.ExportToCsvAsync(data, ct);

    public Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken ct = default) where T : class
        => exportService.ExportToExcelAsync(data, sheetName, ct);

    #endregion

    #region Helper Operations

    /// <summary>
    /// Enriches inventory document rows with complete product and location data using optimized batch queries.
    /// Solves N+1 query problem by fetching all related data in 3 batch queries instead of N queries per row.
    /// Performance: 500 rows = 3 queries (~5 seconds) vs 1500 queries (~60+ seconds with old method).
    /// </summary>
    public async Task<List<InventoryDocumentRowDto>> EnrichInventoryDocumentRowsAsync(
        IEnumerable<DocumentRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsList = rows.ToList();
            if (!rowsList.Any())
            {
                return [];
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            logger.LogInformation("Starting optimized enrichment for {RowCount} inventory rows", rowsList.Count);

            // BATCH 1: Fetch ALL products in a single query
            var productIds = rowsList
                .Where(r => r.ProductId.HasValue)
                .Select(r => r.ProductId!.Value)
                .Distinct()
                .ToList();

            var productsDict = new Dictionary<Guid, Product>();
            if (productIds.Any())
            {
                var products = await context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync(cancellationToken);

                productsDict = products.ToDictionary(p => p.Id);
                logger.LogDebug("Batch loaded {ProductCount} unique products", productsDict.Count);
            }

            // BATCH 2: Fetch ALL locations in a single query
            var locationIds = rowsList
                .Where(r => r.LocationId.HasValue)
                .Select(r => r.LocationId!.Value)
                .Distinct()
                .ToList();

            var locationsDict = new Dictionary<Guid, StorageLocation>();
            if (locationIds.Any())
            {
                var locations = await context.StorageLocations
                    .AsNoTracking()
                    .Where(l => locationIds.Contains(l.Id))
                    .ToListAsync(cancellationToken);

                locationsDict = locations.ToDictionary(l => l.Id);
                logger.LogDebug("Batch loaded {LocationCount} unique locations", locationsDict.Count);
            }

            // BATCH 3: Fetch ALL stocks in a single query
            // Build list of (ProductId, LocationId) pairs for stock lookup
            var stockKeys = rowsList
                .Where(r => r.ProductId.HasValue && r.LocationId.HasValue)
                .Select(r => new { ProductId = r.ProductId!.Value, LocationId = r.LocationId!.Value })
                .Distinct()
                .ToList();

            var stocksDict = new Dictionary<(Guid ProductId, Guid LocationId), Stock>();
            if (stockKeys.Any())
            {
                var stockProductIds = stockKeys.Select(k => k.ProductId).ToList();
                var stockLocationIds = stockKeys.Select(k => k.LocationId).ToList();

                var stocks = await context.Stocks
                    .AsNoTracking()
                    .Where(s => stockProductIds.Contains(s.ProductId) &&
                                stockLocationIds.Contains(s.StorageLocationId) &&
                                s.LotId == null) // Only get stock without lot for inventory
                    .ToListAsync(cancellationToken);

                stocksDict = stocks.ToDictionary(s => (s.ProductId, s.StorageLocationId));
                logger.LogDebug("Batch loaded {StockCount} stock entries", stocksDict.Count);
            }

            // Now process all rows with O(1) dictionary lookups
            var enrichedRows = new List<InventoryDocumentRowDto>(rowsList.Count);
            foreach (var row in rowsList)
            {
                var productId = row.ProductId;
                var locationId = row.LocationId;

                // Lookup product from dictionary (O(1))
                Product? product = null;
                if (productId.HasValue)
                {
                    if (!productsDict.TryGetValue(productId.Value, out product))
                    {
                        // Product lookup failed - log for data quality tracking
                        logger.LogWarning("Product {ProductId} not found in batch - using row description as fallback", productId.Value);
                    }
                }

                // Lookup location from dictionary (O(1))
                StorageLocation? location = null;
                string locationName = string.Empty;
                if (locationId.HasValue)
                {
                    if (locationsDict.TryGetValue(locationId.Value, out location))
                    {
                        locationName = location.Code ?? string.Empty;
                    }
                }

                // Lookup stock from dictionary (O(1))
                decimal? previousQuantity = null;
                decimal? adjustmentQuantity = null;
                if (productId.HasValue && locationId.HasValue)
                {
                    if (stocksDict.TryGetValue((productId.Value, locationId.Value), out var stock) && stock is not null)
                    {
                        previousQuantity = stock.Quantity;
                        adjustmentQuantity = row.Quantity - previousQuantity;
                    }
                }

                enrichedRows.Add(new InventoryDocumentRowDto
                {
                    Id = row.Id,
                    ProductId = productId ?? Guid.Empty,
                    ProductCode = row.ProductCode ?? string.Empty,
                    ProductName = product?.Name ?? row.Description,
                    LocationId = locationId ?? Guid.Empty,
                    LocationName = locationName,
                    Quantity = row.Quantity,
                    PreviousQuantity = previousQuantity,
                    AdjustmentQuantity = adjustmentQuantity,
                    Notes = row.Notes,
                    CreatedAt = row.CreatedAt,
                    CreatedBy = row.CreatedBy
                });
            }

            stopwatch.Stop();
            logger.LogInformation(
                "Completed optimized enrichment for {RowCount} rows in {ElapsedMs}ms. " +
                "Unique products: {ProductCount}, locations: {LocationCount}, stocks: {StockCount}",
                rowsList.Count, stopwatch.ElapsedMilliseconds,
                productsDict.Count, locationsDict.Count, stocksDict.Count);

            return enrichedRows;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in EnrichInventoryDocumentRowsAsync.");
            throw;
        }
    }

    public async Task<List<InventoryDocumentHeaderDto>> GetOpenInventoryDocumentHeadersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inventoryDocType = await GetOrCreateInventoryDocumentTypeAsync(tenantId, cancellationToken);

            return await context.DocumentHeaders
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId
                            && d.DocumentTypeId == inventoryDocType.Id
                            && d.Status == DocumentStatus.Open
                            && !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new InventoryDocumentHeaderDto
                {
                    Id = d.Id,
                    Number = d.Number,
                    InventoryDate = d.Date,
                    Status = d.Status.ToString(),
                    WarehouseName = d.SourceWarehouse != null ? d.SourceWarehouse.Name : null,
                    RowCount = d.Rows.Count(r => !r.IsDeleted),
                    Notes = d.Notes,
                    CreatedAt = d.CreatedAt,
                    CreatedBy = d.CreatedBy
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetOpenInventoryDocumentHeadersAsync for {TenantId}.", tenantId);
            throw;
        }
    }

    #endregion

    #region Inventory Row Management Operations

    public async Task<string?> GetUnitOfMeasureSymbolAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default)
    {
        try
        {
            var um = await context.UMs
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == unitOfMeasureId && !u.IsDeleted, cancellationToken);
            return um?.Symbol;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUnitOfMeasureSymbolAsync for {UnitOfMeasureId}.", unitOfMeasureId);
            throw;
        }
    }

    public async Task<(decimal Percentage, string? Description)?> GetVatRateDetailsAsync(Guid vatRateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var vat = await context.VatRates
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == vatRateId && !v.IsDeleted, cancellationToken);

            if (vat is null)
                return null;

            return (vat.Percentage, $"VAT {vat.Percentage}%");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetVatRateDetailsAsync for {VatRateId}.", vatRateId);
            throw;
        }
    }

    public async Task<DocumentRowDto> UpdateOrMergeInventoryRowAsync(Guid documentId, Guid existingRowId, decimal newQuantity, string? additionalNotes, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowEntity = await context.DocumentRows
                .FirstOrDefaultAsync(r => r.Id == existingRowId && !r.IsDeleted, cancellationToken);

            if (rowEntity is null)
                throw new InvalidOperationException($"Row {existingRowId} not found");

            rowEntity.Quantity = newQuantity;

            if (!string.IsNullOrWhiteSpace(additionalNotes))
            {
                rowEntity.Notes = string.IsNullOrWhiteSpace(rowEntity.Notes)
                    ? additionalNotes
                    : $"{rowEntity.Notes}; {additionalNotes}";
            }

            rowEntity.ModifiedAt = DateTime.UtcNow;
            rowEntity.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);

            return new DocumentRowDto
            {
                Id = rowEntity.Id,
                ProductId = rowEntity.ProductId,
                ProductCode = rowEntity.ProductCode,
                LocationId = rowEntity.LocationId,
                Description = rowEntity.Description,
                Quantity = rowEntity.Quantity,
                Notes = rowEntity.Notes,
                DocumentHeaderId = rowEntity.DocumentHeaderId,
                CreatedAt = rowEntity.CreatedAt,
                CreatedBy = rowEntity.CreatedBy
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateOrMergeInventoryRowAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<bool> UpdateDocumentHeaderFieldsAsync(Guid documentId, DateTime date, Guid? warehouseId, string? notes, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = await context.DocumentHeaders
                .Include(dh => dh.Rows)
                .FirstOrDefaultAsync(dh => dh.Id == documentId && !dh.IsDeleted, cancellationToken);

            if (documentHeader is null)
                return false;

            documentHeader.Date = date;
            documentHeader.SourceWarehouseId = warehouseId;
            documentHeader.Notes = notes;
            documentHeader.ModifiedBy = currentUser;
            documentHeader.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateDocumentHeaderFieldsAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<bool> UpdateInventoryRowAsync(Guid rowId, Guid? productId, decimal quantity, Guid? locationId, string? notes, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowEntity = await context.DocumentRows
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (rowEntity is null)
                return false;

            if (productId.HasValue)
            {
                rowEntity.ProductId = productId.Value;

                var product = await context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId.Value && !p.IsDeleted, cancellationToken);

                if (product is not null)
                {
                    rowEntity.ProductCode = product.Code;
                    rowEntity.Description = product.Name;
                }
            }

            rowEntity.Quantity = quantity;

            if (locationId.HasValue)
            {
                rowEntity.LocationId = locationId.Value;
            }

            rowEntity.Notes = notes;
            rowEntity.ModifiedAt = DateTime.UtcNow;
            rowEntity.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateInventoryRowAsync for {RowId}.", rowId);
            throw;
        }
    }

    public async Task<bool> DeleteInventoryRowAsync(Guid rowId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowEntity = await context.DocumentRows
                .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

            if (rowEntity is null)
                return false;

            rowEntity.IsDeleted = true;
            rowEntity.DeletedAt = DateTime.UtcNow;
            rowEntity.DeletedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in DeleteInventoryRowAsync for {RowId}.", rowId);
            throw;
        }
    }

    public async Task<List<Guid>> ValidateProductsExistAsync(List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingProducts = await context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            return productIds.Except(existingProducts).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ValidateProductsExistAsync.");
            throw;
        }
    }

    public async Task<List<Guid>> ValidateLocationsExistAsync(List<Guid> locationIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingLocations = await context.StorageLocations
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            return locationIds.Except(existingLocations).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ValidateLocationsExistAsync.");
            throw;
        }
    }

    public async Task<bool> CancelInventoryDocumentAsync(Guid documentId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentEntity = await context.DocumentHeaders
                .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (documentEntity is null)
                return false;

            documentEntity.Status = DocumentStatus.Cancelled;
            documentEntity.ModifiedAt = DateTime.UtcNow;
            documentEntity.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CancelInventoryDocumentAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<(List<DocumentRowDto> Rows, int TotalCount)> GetDocumentRowsPagedAsync(Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var totalCount = await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .CountAsync(cancellationToken);

            var skip = (page - 1) * pageSize;
            var documentRows = await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(r => new DocumentRowDto
                {
                    Id = r.Id,
                    DocumentHeaderId = r.DocumentHeaderId,
                    ProductId = r.ProductId,
                    ProductCode = r.ProductCode,
                    Description = r.Description,
                    LocationId = r.LocationId,
                    Quantity = r.Quantity,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy
                })
                .ToListAsync(cancellationToken);

            return (documentRows, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetDocumentRowsPagedAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<int> CancelInventoryDocumentsBatchAsync(List<Guid> documentIds, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentEntities = await context.DocumentHeaders
                .Where(d => documentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            int cancelledCount = 0;

            foreach (var documentEntity in documentEntities)
            {
                documentEntity.Status = DocumentStatus.Cancelled;
                documentEntity.ModifiedAt = now;
                documentEntity.ModifiedBy = currentUser;
                cancelledCount++;
            }

            await context.SaveChangesAsync(cancellationToken);
            return cancelledCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CancelInventoryDocumentsBatchAsync.");
            throw;
        }
    }

    public async Task<List<(Guid Id, DocumentStatus Status, Guid? SourceWarehouseId, List<DocumentRowDto> Rows, string Number, string? Notes)>> LoadDocumentsForMergeAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await context.DocumentHeaders
                .AsNoTracking()
                .Include(d => d.Rows)
                .Where(d => documentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            return documents.Select(d => (
                d.Id,
                d.Status,
                d.SourceWarehouseId,
                d.Rows.Where(r => !r.IsDeleted).Select(r => new DocumentRowDto
                {
                    Id = r.Id,
                    DocumentHeaderId = r.DocumentHeaderId,
                    ProductId = r.ProductId,
                    ProductCode = r.ProductCode,
                    Description = r.Description,
                    LocationId = r.LocationId,
                    Quantity = r.Quantity,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy
                }).ToList(),
                d.Number,
                d.Notes
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in LoadDocumentsForMergeAsync.");
            throw;
        }
    }

    public async Task UpdateDocumentStatusesBatchAsync(List<(Guid DocumentId, DocumentStatus Status, string Notes)> updates, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentIds = updates.Select(u => u.DocumentId).ToList();
            var documents = await context.DocumentHeaders
                .Where(d => documentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            foreach (var doc in documents)
            {
                var update = updates.FirstOrDefault(u => u.DocumentId == doc.Id);
                if (update != default)
                {
                    doc.Status = update.Status;
                    doc.Notes = update.Notes;
                    doc.ModifiedAt = now;
                    doc.ModifiedBy = currentUser;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in UpdateDocumentStatusesBatchAsync.");
            throw;
        }
    }

    public async Task<MergeInventoryDocumentsPreviewDto> PreviewMergeInventoryDocumentsAsync(
        List<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await context.DocumentHeaders
                .AsNoTracking()
                .Include(d => d.Rows)
                .Include(d => d.SourceWarehouse)
                .Where(d => documentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            var preview = new MergeInventoryDocumentsPreviewDto();
            var warnings = new List<string>();

            foreach (var doc in documents)
            {
                var rowCount = doc.Rows?.Count(r => !r.IsDeleted) ?? 0;
                preview.SourceDocuments.Add(new MergeSourceDocumentSummaryDto
                {
                    Id = doc.Id,
                    Number = doc.Number,
                    Status = doc.Status.ToString(),
                    RowCount = rowCount,
                    WarehouseId = doc.SourceWarehouseId,
                    WarehouseName = doc.SourceWarehouse?.Name,
                    InventoryDate = doc.Date
                });
            }

            var allRows = documents.SelectMany(d => d.Rows?.Where(r => !r.IsDeleted) ?? Enumerable.Empty<EventForge.Server.Data.Entities.Documents.DocumentRow>()).ToList();
            preview.TotalInputRows = allRows.Count;

            var groupedByKey = allRows
                .GroupBy(r => new { ProductId = r.ProductId ?? Guid.Empty, LocationId = r.LocationId ?? Guid.Empty })
                .ToList();

            preview.EstimatedOutputRows = groupedByKey.Count;
            preview.RowsToMerge = groupedByKey.Count(g => g.Count() > 1);
            preview.RowsToCopy = groupedByKey.Count(g => g.Count() == 1);

            var warehouseIds = documents.Select(d => d.SourceWarehouseId).Distinct().ToList();
            preview.WarehouseIds = warehouseIds;
            preview.SameWarehouse = warehouseIds.Count == 1;

            if (!preview.SameWarehouse)
            {
                warnings.Add("I documenti selezionati appartengono a magazzini diversi. Le righe verranno accorpate indipendentemente dal magazzino.");
            }

            if (preview.RowsToMerge > 0)
            {
                warnings.Add($"{preview.RowsToMerge} righe con stesso prodotto e ubicazione verranno accorpate sommando le quantità.");
            }

            preview.Warnings = warnings;

            return preview;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in PreviewMergeInventoryDocumentsAsync.");
            throw;
        }
    }

    public async Task<MergeInventoryDocumentsResultDto> MergeInventoryDocumentsAsync(
        MergeInventoryDocumentsDto mergeDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            if (mergeDto.SourceDocumentIds.Count == 0)
                throw new ArgumentException("SourceDocumentIds must contain at least one document ID.", nameof(mergeDto));

            // Load document headers WITHOUT rows to avoid tracking potentially thousands of
            // source rows in the EF change tracker, which was causing SaveChangesAsync timeouts.
            var documents = await context.DocumentHeaders
                .Where(d => mergeDto.SourceDocumentIds.Contains(d.Id) && !d.IsDeleted)
                .ToListAsync(cancellationToken);

            // Determine target document
            var targetId = mergeDto.TargetDocumentId ?? mergeDto.SourceDocumentIds.First();
            var targetDocument = documents.FirstOrDefault(d => d.Id == targetId);
            if (targetDocument is null)
                throw new InvalidOperationException($"Target document {targetId} not found.");

            var sourceDocs = documents.Where(d => d.Id != targetId).ToList();
            var sourceDocIds = sourceDocs.Select(d => d.Id).ToList();

            var result = new MergeInventoryDocumentsResultDto
            {
                MergedDocumentId = targetDocument.Id,
                MergedDocumentNumber = targetDocument.Number
            };

            var now = DateTime.UtcNow;
            int mergedRows = 0;
            int copiedRows = 0;
            var warnings = new List<string>();

            // Load ONLY target rows WITH change tracking (these may be updated).
            var targetRows = await context.DocumentRows
                .Where(r => r.DocumentHeaderId == targetId && !r.IsDeleted)
                .ToListAsync(cancellationToken);

            // Build an O(1) lookup by (ProductId, LocationId) to replace the O(n) FirstOrDefault scan.
            var targetRowLookup = new Dictionary<(Guid?, Guid?), EventForge.Server.Data.Entities.Documents.DocumentRow>(targetRows.Count);
            foreach (var row in targetRows)
                targetRowLookup.TryAdd((row.ProductId, row.LocationId), row);

            var newRows = new List<EventForge.Server.Data.Entities.Documents.DocumentRow>();

            if (sourceDocIds.Count > 0)
            {
                // Load ALL source rows in one query WITHOUT tracking (read-only input).
                var sourceRows = await context.DocumentRows
                    .AsNoTracking()
                    .Where(r => sourceDocIds.Contains(r.DocumentHeaderId) && !r.IsDeleted)
                    .ToListAsync(cancellationToken);

                foreach (var sourceRow in sourceRows)
                {
                    var key = (sourceRow.ProductId, sourceRow.LocationId);
                    if (targetRowLookup.TryGetValue(key, out var existingTargetRow))
                    {
                        // Merge: sum quantities into the already-tracked entity.
                        existingTargetRow.Quantity += sourceRow.Quantity;
                        existingTargetRow.ModifiedAt = now;
                        existingTargetRow.ModifiedBy = currentUser;
                        if (!string.IsNullOrWhiteSpace(sourceRow.Notes))
                        {
                            existingTargetRow.Notes = string.IsNullOrWhiteSpace(existingTargetRow.Notes)
                                ? sourceRow.Notes
                                : $"{existingTargetRow.Notes}; {sourceRow.Notes}";
                        }
                        mergedRows++;
                    }
                    else
                    {
                        // Copy: new row in target.
                        var newRow = new EventForge.Server.Data.Entities.Documents.DocumentRow
                        {
                            Id = Guid.NewGuid(),
                            DocumentHeaderId = targetId,
                            ProductId = sourceRow.ProductId,
                            ProductCode = sourceRow.ProductCode,
                            Description = sourceRow.Description,
                            LocationId = sourceRow.LocationId,
                            Quantity = sourceRow.Quantity,
                            UnitOfMeasure = sourceRow.UnitOfMeasure,
                            Notes = sourceRow.Notes,
                            TenantId = targetDocument.TenantId,
                            CreatedAt = now,
                            CreatedBy = currentUser,
                            ModifiedAt = now,
                            ModifiedBy = currentUser
                        };
                        newRows.Add(newRow);
                        targetRows.Add(newRow);
                        // Keep lookup current so subsequent source rows with the same key merge correctly.
                        targetRowLookup[key] = newRow;
                        copiedRows++;
                    }
                }
            }

            if (newRows.Count > 0)
                context.DocumentRows.AddRange(newRows);

            // Append optional notes to target document
            if (!string.IsNullOrWhiteSpace(mergeDto.Notes))
            {
                targetDocument.Notes = string.IsNullOrWhiteSpace(targetDocument.Notes)
                    ? mergeDto.Notes
                    : $"{targetDocument.Notes}; {mergeDto.Notes}";
            }

            // Keep the target document Open so it remains visible in the inventory procedure
            // and can be finalized normally via the standard finalize workflow.
            targetDocument.ModifiedAt = now;
            targetDocument.ModifiedBy = currentUser;

            // Soft delete all source documents (not the target)
            var softDeletedIds = new List<Guid>();
            foreach (var sourceDoc in sourceDocs)
            {
                var mergedIntoNote = $"[Accorpato in {targetDocument.Number}]";
                sourceDoc.IsDeleted = true;
                sourceDoc.DeletedAt = now;
                sourceDoc.DeletedBy = currentUser;
                sourceDoc.Notes = string.IsNullOrWhiteSpace(sourceDoc.Notes)
                    ? mergedIntoNote
                    : $"{sourceDoc.Notes} {mergedIntoNote}";
                sourceDoc.ModifiedAt = now;
                sourceDoc.ModifiedBy = currentUser;
                softDeletedIds.Add(sourceDoc.Id);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            result.TotalRows = targetRows.Count;
            result.MergedRows = mergedRows;
            result.CopiedRows = copiedRows;
            result.SoftDeletedDocumentIds = softDeletedIds;
            result.Warnings = warnings;

            return result;
        }
        catch
        {
            // Use CancellationToken.None for rollback: the catch block is entered precisely when
            // the original token is cancelled (e.g. client disconnect), so we must not pass the
            // already-cancelled token or RollbackAsync will itself throw TaskCanceledException.
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    #endregion

    #region Inventory Validation Operations

    public async Task<int> CountDocumentRowsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CountDocumentRowsAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<List<(Guid Id, Guid? ProductId, Guid? LocationId)>> GetRowsWithNullDataAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted &&
                           (r.ProductId == null || r.LocationId == null))
                .Select(r => new ValueTuple<Guid, Guid?, Guid?>(r.Id, r.ProductId, r.LocationId))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetRowsWithNullDataAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    public async Task<(List<Guid> ProductIds, List<Guid> LocationIds)> GetUniqueProductAndLocationIdsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var productIds = await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.ProductId != null)
                .Select(r => r.ProductId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var locationIds = await context.DocumentRows
                .AsNoTracking()
                .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.LocationId != null)
                .Select(r => r.LocationId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return (productIds, locationIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetUniqueProductAndLocationIdsAsync for {DocumentId}.", documentId);
            throw;
        }
    }

    #endregion

    #region Transaction Operations

    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    #endregion

    #region Bulk Operations

    public async Task<EventForge.DTOs.Bulk.BulkTransferResultDto> BulkTransferAsync(
        EventForge.DTOs.Bulk.BulkTransferDto bulkTransferDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<EventForge.DTOs.Bulk.BulkItemError>();
        var successCount = 0;

        // Validate batch size
        if (bulkTransferDto.Items.Count > 500)
        {
            throw new ArgumentException("Maximum 500 items can be transferred at once.");
        }

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Validate facilities exist
            var sourceFacility = await storageFacilityService.GetStorageFacilityByIdAsync(
                bulkTransferDto.SourceFacilityId, cancellationToken);
            var destFacility = await storageFacilityService.GetStorageFacilityByIdAsync(
                bulkTransferDto.DestinationFacilityId, cancellationToken);

            if (sourceFacility is null)
            {
                throw new ArgumentException($"Source facility {bulkTransferDto.SourceFacilityId} not found.");
            }

            if (destFacility is null)
            {
                throw new ArgumentException($"Destination facility {bulkTransferDto.DestinationFacilityId} not found.");
            }

            // Validate locations if specified
            if (bulkTransferDto.SourceLocationId.HasValue)
            {
                var sourceLocation = await storageLocationService.GetStorageLocationByIdAsync(
                    bulkTransferDto.SourceLocationId.Value, cancellationToken);
                if (sourceLocation is null)
                {
                    throw new ArgumentException($"Source location {bulkTransferDto.SourceLocationId} not found.");
                }
            }

            if (bulkTransferDto.DestinationLocationId.HasValue)
            {
                var destLocation = await storageLocationService.GetStorageLocationByIdAsync(
                    bulkTransferDto.DestinationLocationId.Value, cancellationToken);
                if (destLocation is null)
                {
                    throw new ArgumentException($"Destination location {bulkTransferDto.DestinationLocationId} not found.");
                }
            }

            var transferDate = bulkTransferDto.TransferDate ?? DateTime.UtcNow;

            // Process each item
            foreach (var item in bulkTransferDto.Items)
            {
                try
                {
                    // Create stock movement for the transfer
                    var createMovementDto = new CreateStockMovementDto
                    {
                        ProductId = item.ProductId,
                        FromLocationId = bulkTransferDto.SourceLocationId,
                        ToLocationId = bulkTransferDto.DestinationLocationId,
                        LotId = item.LotId,
                        Quantity = item.Quantity,
                        MovementType = "Transfer",
                        MovementDate = transferDate,
                        Reason = bulkTransferDto.Reason ?? "Bulk Transfer",
                        Notes = item.Notes,
                        Reference = "BulkTransfer"
                    };

                    await stockMovementService.CreateMovementAsync(createMovementDto, currentUser, cancellationToken);
                    successCount++;

                    logger.LogInformation(
                        "Bulk transfer: Product {ProductId} transferred {Quantity} units from location {SourceLocation} to {DestLocation}",
                        item.ProductId, item.Quantity, bulkTransferDto.SourceLocationId, bulkTransferDto.DestinationLocationId);
                }
                catch (Exception ex)
                {
                    errors.Add(new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = item.ProductId,
                        ErrorMessage = ex.Message
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Bulk transfer completed: {SuccessCount} successful, {FailureCount} failed",
                successCount, errors.Count);

            return new EventForge.DTOs.Bulk.BulkTransferResultDto
            {
                TotalCount = bulkTransferDto.Items.Count,
                SuccessCount = successCount,
                FailedCount = errors.Count,
                Errors = errors,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = false
            };
        }
        catch (Exception ex)
        {
            // Use CancellationToken.None for rollback: the catch block is entered precisely when
            // the original token is cancelled (e.g. client disconnect), so we must not pass the
            // already-cancelled token or RollbackAsync will itself throw TaskCanceledException.
            await transaction.RollbackAsync(CancellationToken.None);
            logger.LogError(ex, "Bulk transfer failed and was rolled back");

            return new EventForge.DTOs.Bulk.BulkTransferResultDto
            {
                TotalCount = bulkTransferDto.Items.Count,
                SuccessCount = 0,
                FailedCount = bulkTransferDto.Items.Count,
                Errors = new List<EventForge.DTOs.Bulk.BulkItemError>
                {
                    new EventForge.DTOs.Bulk.BulkItemError
                    {
                        ItemId = Guid.Empty,
                        ErrorMessage = $"Transaction failed and was rolled back: {ex.Message}"
                    }
                },
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = DateTime.UtcNow - startTime,
                RolledBack = true
            };
        }
    }

    #endregion

}
