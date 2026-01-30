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
public class WarehouseFacade : IWarehouseFacade
{
    private readonly IStorageFacilityService _storageFacilityService;
    private readonly IStorageLocationService _storageLocationService;
    private readonly ILotService _lotService;
    private readonly IStockService _stockService;
    private readonly ISerialService _serialService;
    private readonly IStockMovementService _stockMovementService;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IProductService _productService;
    private readonly IInventoryBulkSeedService _inventoryBulkSeedService;
    private readonly IInventoryDiagnosticService _inventoryDiagnosticService;
    private readonly IStockReconciliationService _stockReconciliationService;
    private readonly IExportService _exportService;
    private readonly EventForgeDbContext _context;
    private readonly ILogger<WarehouseFacade> _logger;

    public WarehouseFacade(
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
        ILogger<WarehouseFacade> logger)
    {
        _storageFacilityService = storageFacilityService ?? throw new ArgumentNullException(nameof(storageFacilityService));
        _storageLocationService = storageLocationService ?? throw new ArgumentNullException(nameof(storageLocationService));
        _lotService = lotService ?? throw new ArgumentNullException(nameof(lotService));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _serialService = serialService ?? throw new ArgumentNullException(nameof(serialService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _inventoryBulkSeedService = inventoryBulkSeedService ?? throw new ArgumentNullException(nameof(inventoryBulkSeedService));
        _inventoryDiagnosticService = inventoryDiagnosticService ?? throw new ArgumentNullException(nameof(inventoryDiagnosticService));
        _stockReconciliationService = stockReconciliationService ?? throw new ArgumentNullException(nameof(stockReconciliationService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Storage Facility Operations

    public Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _storageFacilityService.CreateStorageFacilityAsync(createDto, currentUser, cancellationToken);

    public Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _storageFacilityService.GetStorageFacilityByIdAsync(id, cancellationToken);

    public Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
        => _storageFacilityService.GetStorageFacilitiesAsync(pagination, cancellationToken);

    public Task<IEnumerable<WarehouseExportDto>> GetWarehousesForExportAsync(PaginationParameters pagination, CancellationToken ct = default)
        => _storageFacilityService.GetWarehousesForExportAsync(pagination, ct);

    #endregion

    #region Storage Location Operations

    public Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _storageLocationService.GetStorageLocationByIdAsync(id, cancellationToken);

    public Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _storageLocationService.CreateStorageLocationAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default)
        => _storageLocationService.GetStorageLocationsAsync(pagination, warehouseId, cancellationToken);

    #endregion

    #region Lot Operations

    public Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default)
        => _lotService.BlockLotAsync(id, reason, currentUser, cancellationToken);

    public Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _lotService.CreateLotAsync(createDto, currentUser, cancellationToken);

    public Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => _lotService.DeleteLotAsync(id, currentUser, cancellationToken);

    public Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
        => _lotService.GetExpiringLotsAsync(daysAhead, cancellationToken);

    public Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _lotService.GetLotByCodeAsync(code, cancellationToken);

    public Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _lotService.GetLotByIdAsync(id, cancellationToken);

    public Task<PagedResult<LotDto>> GetLotsAsync(PaginationParameters pagination, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken cancellationToken = default)
        => _lotService.GetLotsAsync(pagination, productId, status, expiringSoon, cancellationToken);

    public Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => _lotService.UnblockLotAsync(id, currentUser, cancellationToken);

    public Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default)
        => _lotService.UpdateLotAsync(id, updateDto, currentUser, cancellationToken);

    public Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
        => _lotService.UpdateQualityStatusAsync(id, qualityStatus, currentUser, notes, cancellationToken);

    #endregion

    #region Stock Operations

    public Task UpdateLastInventoryDateAsync(Guid stockId, DateTime inventoryDate, CancellationToken cancellationToken = default)
        => _stockService.UpdateLastInventoryDateAsync(stockId, inventoryDate, cancellationToken);

    public Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _stockService.CreateOrUpdateStockAsync(createDto, currentUser, cancellationToken);

    public Task<StockDto> CreateOrUpdateStockAsync(CreateOrUpdateStockDto dto, string currentUser, CancellationToken cancellationToken = default)
        => _stockService.CreateOrUpdateStockAsync(dto, currentUser, cancellationToken);

    public Task<PagedResult<StockDto>> GetStockAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, CancellationToken cancellationToken = default)
        => _stockService.GetStockAsync(page, pageSize, productId, locationId, lotId, lowStock, cancellationToken);

    public Task<StockDto?> AdjustStockAsync(AdjustStockDto dto, string currentUser, CancellationToken cancellationToken = default)
        => _stockService.AdjustStockAsync(dto, currentUser, cancellationToken);

    public Task<PagedResult<StockLocationDetail>> GetStockOverviewAsync(int page = 1, int pageSize = 20, string? searchTerm = null, Guid? warehouseId = null, Guid? locationId = null, Guid? lotId = null, bool? lowStock = null, bool? criticalStock = null, bool? outOfStock = null, bool? inStockOnly = null, bool? showAllProducts = null, bool detailedView = false, CancellationToken cancellationToken = default)
        => _stockService.GetStockOverviewAsync(page, pageSize, searchTerm, warehouseId, locationId, lotId, lowStock, criticalStock, outOfStock, inStockOnly, showAllProducts, detailedView, cancellationToken);

    public Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default)
        => _stockService.ReserveStockAsync(productId, locationId, quantity, lotId, currentUser, cancellationToken);

    public Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _stockService.GetStockByIdAsync(id, cancellationToken);

    public Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => _stockService.GetStockByProductIdAsync(productId, cancellationToken);

    #endregion

    #region Serial Operations

    public Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _serialService.CreateSerialAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<SerialDto>> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, Guid? locationId = null, string? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
        => _serialService.GetSerialsAsync(page, pageSize, productId, lotId, locationId, status, searchTerm, cancellationToken);

    public Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
        => _serialService.UpdateSerialStatusAsync(id, status, currentUser, notes, cancellationToken);

    public Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _serialService.GetSerialByIdAsync(id, cancellationToken);

    #endregion

    #region Stock Movement Operations

    public Task<StockMovementDto> ProcessAdjustmentMovementAsync(Guid productId, Guid locationId, decimal adjustmentQuantity, string reason, Guid? lotId = null, string? notes = null, string? currentUser = null, DateTime? movementDate = null, CancellationToken cancellationToken = default)
        => _stockMovementService.ProcessAdjustmentMovementAsync(productId, locationId, adjustmentQuantity, reason, lotId, notes, currentUser, movementDate, cancellationToken);

    public Task<IEnumerable<InventoryExportDto>> GetInventoryForExportAsync(PaginationParameters pagination, CancellationToken ct = default)
        => _stockMovementService.GetInventoryForExportAsync(pagination, ct);

    #endregion

    #region Document Operations

    public Task<DocumentHeaderDto?> CloseDocumentAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
        => _documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);

    public Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(Guid id, bool includeRows = false, CancellationToken cancellationToken = default)
        => _documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);

    public Task<DocumentRowDto> AddDocumentRowAsync(CreateDocumentRowDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _documentHeaderService.AddDocumentRowAsync(createDto, currentUser, cancellationToken);

    public Task<DocumentHeaderDto> CreateDocumentHeaderAsync(CreateDocumentHeaderDto createDto, string currentUser, CancellationToken cancellationToken = default)
        => _documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);

    public Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(DocumentHeaderQueryParameters queryParameters, CancellationToken cancellationToken = default)
        => _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);

    public Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _documentHeaderService.GetOrCreateInventoryDocumentTypeAsync(tenantId, cancellationToken);

    public Task<Guid> GetOrCreateSystemBusinessPartyAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _documentHeaderService.GetOrCreateSystemBusinessPartyAsync(tenantId, cancellationToken);

    #endregion

    #region Product Operations

    public Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _productService.GetProductByIdAsync(id, cancellationToken);

    #endregion

    #region Inventory Bulk Operations

    public Task<InventorySeedResultDto> SeedInventoryAsync(InventorySeedRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        => _inventoryBulkSeedService.SeedInventoryAsync(request, currentUser, cancellationToken);

    #endregion

    #region Inventory Diagnostic Operations

    public Task<int> RemoveProblematicRowsAsync(Guid documentId, List<Guid> rowIds, string currentUser, CancellationToken cancellationToken = default)
        => _inventoryDiagnosticService.RemoveProblematicRowsAsync(documentId, rowIds, currentUser, cancellationToken);

    public Task<InventoryDiagnosticReportDto> DiagnoseDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        => _inventoryDiagnosticService.DiagnoseDocumentAsync(documentId, cancellationToken);

    public Task<InventoryRepairResultDto> AutoRepairDocumentAsync(Guid documentId, InventoryAutoRepairOptionsDto options, string currentUser, CancellationToken cancellationToken = default)
        => _inventoryDiagnosticService.AutoRepairDocumentAsync(documentId, options, currentUser, cancellationToken);

    public Task<bool> RepairRowAsync(Guid documentId, Guid rowId, InventoryRowRepairDto repairData, string currentUser, CancellationToken cancellationToken = default)
        => _inventoryDiagnosticService.RepairRowAsync(documentId, rowId, repairData, currentUser, cancellationToken);

    #endregion

    #region Stock Reconciliation Operations

    public Task<byte[]> ExportReconciliationReportAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default)
        => _stockReconciliationService.ExportReconciliationReportAsync(request, cancellationToken);

    public Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, string currentUser, CancellationToken cancellationToken = default)
        => _stockReconciliationService.ApplyReconciliationAsync(request, currentUser, cancellationToken);

    public Task<StockReconciliationResultDto> CalculateReconciledStockAsync(StockReconciliationRequestDto request, CancellationToken cancellationToken = default)
        => _stockReconciliationService.CalculateReconciledStockAsync(request, cancellationToken);

    #endregion

    #region Export Operations

    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken ct = default) where T : class
        => _exportService.ExportToCsvAsync(data, ct);

    public Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName = "Data", CancellationToken ct = default) where T : class
        => _exportService.ExportToExcelAsync(data, sheetName, ct);

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
        var rowsList = rows.ToList();
        if (!rowsList.Any())
        {
            return new List<InventoryDocumentRowDto>();
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting optimized enrichment for {RowCount} inventory rows", rowsList.Count);

        // BATCH 1: Fetch ALL products in a single query
        var productIds = rowsList
            .Where(r => r.ProductId.HasValue)
            .Select(r => r.ProductId!.Value)
            .Distinct()
            .ToList();

        var productsDict = new Dictionary<Guid, Product>();
        if (productIds.Any())
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            productsDict = products.ToDictionary(p => p.Id);
            _logger.LogDebug("Batch loaded {ProductCount} unique products", productsDict.Count);
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
            var locations = await _context.StorageLocations
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            locationsDict = locations.ToDictionary(l => l.Id);
            _logger.LogDebug("Batch loaded {LocationCount} unique locations", locationsDict.Count);
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

            var stocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => stockProductIds.Contains(s.ProductId) &&
                           stockLocationIds.Contains(s.StorageLocationId) &&
                           s.LotId == null) // Only get stock without lot for inventory
                .ToListAsync(cancellationToken);

            stocksDict = stocks.ToDictionary(s => (s.ProductId, s.StorageLocationId));
            _logger.LogDebug("Batch loaded {StockCount} stock entries", stocksDict.Count);
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
                    _logger.LogWarning("Product {ProductId} not found in batch - using row description as fallback", productId.Value);
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
                if (stocksDict.TryGetValue((productId.Value, locationId.Value), out var stock) && stock != null)
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
        _logger.LogInformation(
            "Completed optimized enrichment for {RowCount} rows in {ElapsedMs}ms. " +
            "Unique products: {ProductCount}, locations: {LocationCount}, stocks: {StockCount}",
            rowsList.Count, stopwatch.ElapsedMilliseconds,
            productsDict.Count, locationsDict.Count, stocksDict.Count);

        return enrichedRows;
    }

    #endregion

    #region Inventory Row Management Operations

    public async Task<string?> GetUnitOfMeasureSymbolAsync(Guid unitOfMeasureId, CancellationToken cancellationToken = default)
    {
        var um = await _context.UMs
            .FirstOrDefaultAsync(u => u.Id == unitOfMeasureId && !u.IsDeleted, cancellationToken);
        return um?.Symbol;
    }

    public async Task<(decimal Percentage, string? Description)?> GetVatRateDetailsAsync(Guid vatRateId, CancellationToken cancellationToken = default)
    {
        var vat = await _context.VatRates
            .FirstOrDefaultAsync(v => v.Id == vatRateId && !v.IsDeleted, cancellationToken);

        if (vat == null)
            return null;

        return (vat.Percentage, $"VAT {vat.Percentage}%");
    }

    public async Task<DocumentRowDto> UpdateOrMergeInventoryRowAsync(Guid documentId, Guid existingRowId, decimal newQuantity, string? additionalNotes, string currentUser, CancellationToken cancellationToken = default)
    {
        var rowEntity = await _context.DocumentRows
            .FirstOrDefaultAsync(r => r.Id == existingRowId && !r.IsDeleted, cancellationToken);

        if (rowEntity == null)
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

        await _context.SaveChangesAsync(cancellationToken);

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

    public async Task<bool> UpdateDocumentHeaderFieldsAsync(Guid documentId, DateTime date, Guid? warehouseId, string? notes, string currentUser, CancellationToken cancellationToken = default)
    {
        var documentHeader = await _context.DocumentHeaders
            .Include(dh => dh.Rows)
            .FirstOrDefaultAsync(dh => dh.Id == documentId && !dh.IsDeleted, cancellationToken);

        if (documentHeader == null)
            return false;

        documentHeader.Date = date;
        documentHeader.SourceWarehouseId = warehouseId;
        documentHeader.Notes = notes;
        documentHeader.ModifiedBy = currentUser;
        documentHeader.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateInventoryRowAsync(Guid rowId, Guid? productId, decimal quantity, Guid? locationId, string? notes, string currentUser, CancellationToken cancellationToken = default)
    {
        var rowEntity = await _context.DocumentRows
            .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

        if (rowEntity == null)
            return false;

        if (productId.HasValue)
        {
            rowEntity.ProductId = productId.Value;

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId.Value && !p.IsDeleted, cancellationToken);

            if (product != null)
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

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteInventoryRowAsync(Guid rowId, string currentUser, CancellationToken cancellationToken = default)
    {
        var rowEntity = await _context.DocumentRows
            .FirstOrDefaultAsync(r => r.Id == rowId && !r.IsDeleted, cancellationToken);

        if (rowEntity == null)
            return false;

        rowEntity.IsDeleted = true;
        rowEntity.DeletedAt = DateTime.UtcNow;
        rowEntity.DeletedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<Guid>> ValidateProductsExistAsync(List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        var existingProducts = await _context.Products
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return productIds.Except(existingProducts).ToList();
    }

    public async Task<List<Guid>> ValidateLocationsExistAsync(List<Guid> locationIds, CancellationToken cancellationToken = default)
    {
        var existingLocations = await _context.StorageLocations
            .Where(l => locationIds.Contains(l.Id) && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        return locationIds.Except(existingLocations).ToList();
    }

    public async Task<bool> CancelInventoryDocumentAsync(Guid documentId, string currentUser, CancellationToken cancellationToken = default)
    {
        var documentEntity = await _context.DocumentHeaders
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

        if (documentEntity == null)
            return false;

        documentEntity.Status = DocumentStatus.Cancelled;
        documentEntity.ModifiedAt = DateTime.UtcNow;
        documentEntity.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(List<DocumentRowDto> Rows, int TotalCount)> GetDocumentRowsPagedAsync(Guid documentId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
            .CountAsync(cancellationToken);

        var skip = (page - 1) * pageSize;
        var documentRows = await _context.DocumentRows
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

    public async Task<int> CancelInventoryDocumentsBatchAsync(List<Guid> documentIds, string currentUser, CancellationToken cancellationToken = default)
    {
        var documentEntities = await _context.DocumentHeaders
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

        await _context.SaveChangesAsync(cancellationToken);
        return cancelledCount;
    }

    public async Task<List<(Guid Id, DocumentStatus Status, Guid? SourceWarehouseId, List<DocumentRowDto> Rows, string Number, string? Notes)>> LoadDocumentsForMergeAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        var documents = await _context.DocumentHeaders
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

    public async Task UpdateDocumentStatusesBatchAsync(List<(Guid DocumentId, DocumentStatus Status, string Notes)> updates, string currentUser, CancellationToken cancellationToken = default)
    {
        var documentIds = updates.Select(u => u.DocumentId).ToList();
        var documents = await _context.DocumentHeaders
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

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Inventory Validation Operations

    public async Task<int> CountDocumentRowsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<List<(Guid Id, Guid? ProductId, Guid? LocationId)>> GetRowsWithNullDataAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted &&
                       (r.ProductId == null || r.LocationId == null))
            .Select(r => new ValueTuple<Guid, Guid?, Guid?>(r.Id, r.ProductId, r.LocationId))
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Guid> ProductIds, List<Guid> LocationIds)> GetUniqueProductAndLocationIdsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var productIds = await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.ProductId != null)
            .Select(r => r.ProductId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var locationIds = await _context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentId && !r.IsDeleted && r.LocationId != null)
            .Select(r => r.LocationId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (productIds, locationIds);
    }

    #endregion

    #region Transaction Operations

    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
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

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Validate facilities exist
            var sourceFacility = await _storageFacilityService.GetStorageFacilityByIdAsync(
                bulkTransferDto.SourceFacilityId, cancellationToken);
            var destFacility = await _storageFacilityService.GetStorageFacilityByIdAsync(
                bulkTransferDto.DestinationFacilityId, cancellationToken);

            if (sourceFacility == null)
            {
                throw new ArgumentException($"Source facility {bulkTransferDto.SourceFacilityId} not found.");
            }

            if (destFacility == null)
            {
                throw new ArgumentException($"Destination facility {bulkTransferDto.DestinationFacilityId} not found.");
            }

            // Validate locations if specified
            if (bulkTransferDto.SourceLocationId.HasValue)
            {
                var sourceLocation = await _storageLocationService.GetStorageLocationByIdAsync(
                    bulkTransferDto.SourceLocationId.Value, cancellationToken);
                if (sourceLocation == null)
                {
                    throw new ArgumentException($"Source location {bulkTransferDto.SourceLocationId} not found.");
                }
            }

            if (bulkTransferDto.DestinationLocationId.HasValue)
            {
                var destLocation = await _storageLocationService.GetStorageLocationByIdAsync(
                    bulkTransferDto.DestinationLocationId.Value, cancellationToken);
                if (destLocation == null)
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

                    await _stockMovementService.CreateMovementAsync(createMovementDto, currentUser, cancellationToken);
                    successCount++;

                    _logger.LogInformation(
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

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
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
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Bulk transfer failed and was rolled back");

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
