using EventForge.DTOs.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for stock reconciliation operations.
/// </summary>
public class StockReconciliationService : IStockReconciliationService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IStockMovementService _stockMovementService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StockReconciliationService> _logger;

    public StockReconciliationService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        IStockMovementService stockMovementService,
        ITenantContext tenantContext,
        ILogger<StockReconciliationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _stockMovementService = stockMovementService ?? throw new ArgumentNullException(nameof(stockMovementService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StockReconciliationResultDto> CalculateReconciledStockAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Server-side date validation
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            {
                throw new ArgumentException("FromDate cannot be after ToDate.");
            }

            _logger.LogInformation("Starting stock reconciliation calculation for tenant {TenantId}", currentTenantId);

            var result = new StockReconciliationResultDto();

            // Get stocks based on filters
            var stocksQuery = _context.Stocks
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse)
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted);

            // Apply filters
            if (request.ProductId.HasValue)
            {
                stocksQuery = stocksQuery.Where(s => s.ProductId == request.ProductId.Value);
            }

            if (request.LocationId.HasValue)
            {
                stocksQuery = stocksQuery.Where(s => s.StorageLocationId == request.LocationId.Value);
            }

            if (request.WarehouseId.HasValue)
            {
                stocksQuery = stocksQuery.Where(s => s.StorageLocation!.WarehouseId == request.WarehouseId.Value);
            }

            var stocks = await stocksQuery.ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} stock records to reconcile", stocks.Count);

            if (stocks.Count == 0)
            {
                result.Summary = CalculateSummary(result.Items);
                return result;
            }

            // Batch pre-load all relevant data to avoid N+1 queries.
            var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
            var locationIds = stocks.Select(s => s.StorageLocationId).Distinct().ToList();

            // Batch load inventory document rows (only closed inventory documents)
            var allInventoryRows = new List<Data.Entities.Documents.DocumentRow>();
            if (request.IncludeInventories)
            {
                var invQuery = _context.DocumentRows
                    .AsNoTracking()
                    .Include(dr => dr.DocumentHeader)
                        .ThenInclude(dh => dh!.DocumentType)
                    .Where(dr => dr.TenantId == currentTenantId.Value &&
                                !dr.IsDeleted &&
                                dr.ProductId.HasValue && productIds.Contains(dr.ProductId.Value) &&
                                dr.LocationId.HasValue && locationIds.Contains(dr.LocationId.Value) &&
                                dr.DocumentHeader != null &&
                                dr.DocumentHeader.DocumentType != null &&
                                dr.DocumentHeader.DocumentType.IsInventoryDocument &&
                                dr.DocumentHeader.Status == EventForge.DTOs.Common.DocumentStatus.Closed);

                if (request.FromDate.HasValue)
                    invQuery = invQuery.Where(dr => dr.DocumentHeader!.Date >= request.FromDate.Value);
                if (request.ToDate.HasValue)
                    invQuery = invQuery.Where(dr => dr.DocumentHeader!.Date <= request.ToDate.Value);

                allInventoryRows = await invQuery.ToListAsync(cancellationToken);
            }

            // Batch load regular (non-inventory) document rows in Open or Closed status.
            // We do NOT apply the per-stock effectiveFromDate here; it is applied in-memory per stock.
            var allDocumentRows = new List<Data.Entities.Documents.DocumentRow>();
            if (request.IncludeDocuments)
            {
                var docQuery = _context.DocumentRows
                    .AsNoTracking()
                    .Include(dr => dr.DocumentHeader)
                        .ThenInclude(dh => dh!.DocumentType)
                    .Where(dr => dr.TenantId == currentTenantId.Value &&
                                !dr.IsDeleted &&
                                dr.ProductId.HasValue && productIds.Contains(dr.ProductId.Value) &&
                                dr.LocationId.HasValue && locationIds.Contains(dr.LocationId.Value) &&
                                dr.DocumentHeader != null &&
                                dr.DocumentHeader.DocumentType != null &&
                                !dr.DocumentHeader.DocumentType.IsInventoryDocument &&
                                (dr.DocumentHeader.Status == EventForge.DTOs.Common.DocumentStatus.Open ||
                                 dr.DocumentHeader.Status == EventForge.DTOs.Common.DocumentStatus.Closed));

                if (request.FromDate.HasValue)
                    docQuery = docQuery.Where(dr => dr.DocumentHeader!.Date >= request.FromDate.Value);
                if (request.ToDate.HasValue)
                    docQuery = docQuery.Where(dr => dr.DocumentHeader!.Date <= request.ToDate.Value);

                allDocumentRows = await docQuery.ToListAsync(cancellationToken);
            }

            // Batch load manual stock movements.
            // We do NOT apply the per-stock effectiveFromDate here; it is applied in-memory per stock.
            var manualMovQuery = _context.StockMovements
                .AsNoTracking()
                .Where(sm => sm.TenantId == currentTenantId.Value &&
                            !sm.IsDeleted &&
                            productIds.Contains(sm.ProductId) &&
                            (sm.FromLocationId.HasValue && locationIds.Contains(sm.FromLocationId.Value) ||
                             sm.ToLocationId.HasValue && locationIds.Contains(sm.ToLocationId.Value)));

            if (request.FromDate.HasValue)
                manualMovQuery = manualMovQuery.Where(sm => sm.MovementDate >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                manualMovQuery = manualMovQuery.Where(sm => sm.MovementDate <= request.ToDate.Value);

            var allManualMovements = await manualMovQuery.ToListAsync(cancellationToken);

            // Process each stock using pre-loaded in-memory data
            foreach (var stock in stocks)
            {
                var item = CalculateStockItem(stock, request, allInventoryRows, allDocumentRows, allManualMovements);

                // Filter by discrepancies if requested
                if (!request.OnlyWithDiscrepancies || item.Severity != ReconciliationSeverity.Correct)
                {
                    result.Items.Add(item);
                }
            }

            // Calculate summary
            result.Summary = CalculateSummary(result.Items);

            _logger.LogInformation("Reconciliation calculation completed. Total items: {Total}, With discrepancies: {Discrepancies}",
                result.Summary.TotalProducts, result.Summary.TotalProducts - result.Summary.CorrectCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stock reconciliation");
            throw;
        }
    }

    /// <summary>
    /// Calculates the reconciliation item for a single stock record using pre-loaded in-memory data.
    /// </summary>
    private StockReconciliationItemDto CalculateStockItem(
        Data.Entities.Warehouse.Stock stock,
        StockReconciliationRequestDto request,
        List<Data.Entities.Documents.DocumentRow> allInventoryRows,
        List<Data.Entities.Documents.DocumentRow> allDocumentRows,
        List<Data.Entities.Warehouse.StockMovement> allManualMovements)
    {
        var item = new StockReconciliationItemDto
        {
            StockId = stock.Id,
            ProductId = stock.ProductId,
            ProductCode = stock.Product?.Code ?? string.Empty,
            ProductName = stock.Product?.Name ?? string.Empty,
            WarehouseName = stock.StorageLocation?.Warehouse?.Name ?? string.Empty,
            LocationCode = stock.StorageLocation?.Code ?? string.Empty,
            CurrentQuantity = stock.Quantity
        };

        decimal calculatedQuantity = request.StartingQuantity ?? 0m;
        var sourceMovements = new List<StockMovementSourceDto>();
        DateTime? effectiveFromDate = request.FromDate;

        // 1. Find the most recent closed inventory document for this stock (replaces starting quantity)
        if (request.IncludeInventories)
        {
            var lastInventoryRow = allInventoryRows
                .Where(dr => dr.ProductId == stock.ProductId && dr.LocationId == stock.StorageLocationId)
                .OrderByDescending(dr => dr.DocumentHeader!.Date)
                .FirstOrDefault();

            if (lastInventoryRow != null)
            {
                sourceMovements.Add(new StockMovementSourceDto
                {
                    Type = "Inventory",
                    Reference = $"{lastInventoryRow.DocumentHeader!.DocumentType!.Code}-{lastInventoryRow.DocumentHeader.Number}",
                    Quantity = lastInventoryRow.Quantity,
                    Date = lastInventoryRow.DocumentHeader.Date,
                    IsReplacement = true
                });
                // Inventory snapshot replaces the calculated quantity and advances the effective start date
                calculatedQuantity = lastInventoryRow.Quantity;
                effectiveFromDate = lastInventoryRow.DocumentHeader.Date;
                item.TotalInventories = 1;

                if (request.StartingQuantity.HasValue && request.StartingQuantity.Value != 0m)
                {
                    _logger.LogDebug(
                        "StartingQuantity {Qty} overridden by inventory document for product {ProductId} at location {LocationId}",
                        request.StartingQuantity.Value, stock.ProductId, stock.StorageLocationId);
                }
            }
        }

        // 2. Apply non-inventory document movements (carico/scarico) from the effective start date.
        //    Draft and Cancelled documents are excluded; inventory documents are excluded to avoid double-counting.
        if (request.IncludeDocuments)
        {
            var documentMovements = allDocumentRows
                .Where(dr => dr.ProductId == stock.ProductId &&
                             dr.LocationId == stock.StorageLocationId &&
                             (effectiveFromDate == null || dr.DocumentHeader!.Date >= effectiveFromDate.Value))
                .Select(dr => new StockMovementSourceDto
                {
                    Type = "Document",
                    Reference = $"{dr.DocumentHeader!.DocumentType!.Code}-{dr.DocumentHeader.Number}",
                    // Use Math.Abs to guard against negative quantities stored in the DB
                    Quantity = dr.DocumentHeader.DocumentType.IsStockIncrease
                        ? Math.Abs(dr.Quantity)
                        : -Math.Abs(dr.Quantity),
                    Date = dr.DocumentHeader.Date,
                    IsReplacement = false
                })
                .ToList();

            foreach (var movement in documentMovements)
            {
                sourceMovements.Add(movement);
                calculatedQuantity += movement.Quantity;
            }

            item.TotalDocuments = documentMovements.Count;
        }

        // 3. Apply manual stock movements (transfers, adjustments) from the effective start date.
        //    Quantities are always stored as positive values; the sign is determined by the location role.
        var manualMovements = allManualMovements
            .Where(sm => sm.ProductId == stock.ProductId &&
                         (sm.FromLocationId == stock.StorageLocationId || sm.ToLocationId == stock.StorageLocationId) &&
                         (effectiveFromDate == null || sm.MovementDate >= effectiveFromDate.Value))
            .Select(sm => new StockMovementSourceDto
            {
                Type = "Manual",
                Reference = sm.Reference ?? "Manual Adjustment",
                // Inbound when this location is the destination; outbound when it is the source
                Quantity = sm.ToLocationId == stock.StorageLocationId
                    ? Math.Abs(sm.Quantity)
                    : -Math.Abs(sm.Quantity),
                Date = sm.MovementDate,
                IsReplacement = false
            })
            .ToList();

        foreach (var movement in manualMovements)
        {
            sourceMovements.Add(movement);
            calculatedQuantity += movement.Quantity;
        }

        item.TotalManualMovements = manualMovements.Count;

        // Set calculated values
        item.CalculatedQuantity = calculatedQuantity;
        item.Difference = calculatedQuantity - stock.Quantity;

        var baseValue = Math.Max(Math.Abs(item.CurrentQuantity), Math.Abs(calculatedQuantity));
        item.DifferencePercentage = baseValue != 0
            ? Math.Abs(item.Difference) / baseValue * 100
            : 0;

        item.Severity = DetermineSeverity(
            item.CurrentQuantity,
            item.CalculatedQuantity,
            item.DifferencePercentage,
            request.DiscrepancyThreshold);
        item.SourceMovements = sourceMovements.OrderBy(m => m.Date).ToList();

        return item;
    }

    private static ReconciliationSeverity DetermineSeverity(
        decimal currentQuantity,
        decimal calculatedQuantity,
        decimal differencePercentage,
        decimal threshold)
    {
        // Missing: physical stock is zero but movements indicate stock should exist
        if (currentQuantity == 0 && calculatedQuantity > 0)
        {
            return ReconciliationSeverity.Missing;
        }

        // Phantom: physical stock exists but movements show zero or negative (data integrity issue)
        if (currentQuantity > 0 && calculatedQuantity <= 0)
        {
            return ReconciliationSeverity.Major;
        }

        // Correct: no difference
        if (currentQuantity == calculatedQuantity)
        {
            return ReconciliationSeverity.Correct;
        }

        // Major: difference exceeds threshold
        if (differencePercentage > threshold)
        {
            return ReconciliationSeverity.Major;
        }

        // Minor: difference within threshold
        return ReconciliationSeverity.Minor;
    }

    private static StockReconciliationSummaryDto CalculateSummary(List<StockReconciliationItemDto> items)
    {
        return new StockReconciliationSummaryDto
        {
            TotalProducts = items.Count,
            CorrectCount = items.Count(i => i.Severity == ReconciliationSeverity.Correct),
            MinorDiscrepancyCount = items.Count(i => i.Severity == ReconciliationSeverity.Minor),
            MajorDiscrepancyCount = items.Count(i => i.Severity == ReconciliationSeverity.Major),
            MissingCount = items.Count(i => i.Severity == ReconciliationSeverity.Missing),
            TotalDifferenceValue = items.Sum(i => Math.Abs(i.Difference))
        };
    }

    public async Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(
        StockReconciliationApplyRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            _logger.LogInformation("Applying stock reconciliation for {Count} items by user {User}",
                request.ItemsToApply.Count, currentUser);

            var result = new StockReconciliationApplyResultDto { Success = true };

            // Use transaction for atomicity
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Recalculate using the exact same filters that produced the preview shown to the user.
                // This ensures the quantities being applied match what was displayed.
                var recalcRequest = request.ReconciliationFilters ?? new StockReconciliationRequestDto
                {
                    IncludeDocuments = true,
                    IncludeInventories = true
                };
                var reconciliation = await CalculateReconciledStockAsync(recalcRequest, cancellationToken);

                var itemsToUpdate = reconciliation.Items
                    .Where(i => request.ItemsToApply.Contains(i.StockId))
                    .ToList();

                foreach (var item in itemsToUpdate)
                {
                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.Id == item.StockId && s.TenantId == currentTenantId.Value, cancellationToken);

                    if (stock == null)
                    {
                        _logger.LogWarning("Stock {StockId} not found", item.StockId);
                        continue;
                    }

                    var oldQuantity = stock.Quantity;
                    var newQuantity = item.CalculatedQuantity;
                    var adjustment = newQuantity - oldQuantity;

                    result.UpdatedCount++;
                    result.UpdatedStockIds.Add(stock.Id);
                    result.TotalAdjustmentValue += Math.Abs(adjustment);

                    // Create adjustment movement if requested.
                    // Note: ProcessAdjustmentMovementAsync already updates stock levels via UpdateStockLevelsForMovementAsync,
                    // so we only set stock.Quantity directly when not creating a movement to avoid double-applying the delta.
                    if (request.CreateAdjustmentMovements && adjustment != 0)
                    {
                        await _stockMovementService.ProcessAdjustmentMovementAsync(
                            productId: stock.ProductId,
                            locationId: stock.StorageLocationId,
                            adjustmentQuantity: adjustment,
                            reason: "Stock Reconciliation",
                            lotId: stock.LotId,
                            notes: $"{request.Reason}. Adjusted from {oldQuantity} to {newQuantity}",
                            currentUser: currentUser,
                            movementDate: DateTime.UtcNow,
                            cancellationToken: cancellationToken);

                        result.MovementsCreated++;
                        stock.ModifiedBy = currentUser;
                    }
                    else if (adjustment != 0)
                    {
                        stock.Quantity = newQuantity;
                        stock.ModifiedAt = DateTime.UtcNow;
                        stock.ModifiedBy = currentUser;
                    }

                    // Audit log — include the reconciliation reason for a complete audit trail
                    await _auditLogService.LogEntityChangeAsync(
                        entityName: "Stock",
                        entityId: stock.Id,
                        propertyName: "Quantity",
                        operationType: "Reconciliation",
                        oldValue: oldQuantity.ToString(),
                        newValue: newQuantity.ToString(),
                        changedBy: currentUser,
                        entityDisplayName: $"{stock.Product?.Name ?? "Unknown"} @ {stock.StorageLocation?.Code ?? "Unknown"} - {request.Reason}",
                        cancellationToken: cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Stock reconciliation applied successfully. Updated: {Updated}, Movements: {Movements}",
                    result.UpdatedCount, result.MovementsCreated);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error applying stock reconciliation, transaction rolled back");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApplyReconciliationAsync");
            return new StockReconciliationApplyResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<byte[]> ExportReconciliationReportAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement Excel export using EPPlus or ClosedXML with Summary, Details, and Movements sheets
        _logger.LogWarning("Excel export not yet implemented - returning empty array");
        return Task.FromResult(Array.Empty<byte>());
    }

    public async Task<RebuildMovementsResultDto> RebuildMissingMovementsFromDocumentsAsync(
        RebuildMovementsRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate cannot be after ToDate.");
        }

        var result = new RebuildMovementsResultDto { IsDryRun = request.DryRun };

        // Resolve effective status filters (defaults: Approved, Closed)
        var approvalStatusFilter = (request.ApprovalStatuses != null && request.ApprovalStatuses.Count > 0)
            ? request.ApprovalStatuses.Select(v => (Data.Entities.Documents.ApprovalStatus)v).ToList()
            : new List<Data.Entities.Documents.ApprovalStatus> { Data.Entities.Documents.ApprovalStatus.Approved };

        var documentStatusFilter = (request.DocumentStatuses != null && request.DocumentStatuses.Count > 0)
            ? request.DocumentStatuses.Select(v => (EventForge.DTOs.Common.DocumentStatus)v).ToList()
            : new List<EventForge.DTOs.Common.DocumentStatus> { EventForge.DTOs.Common.DocumentStatus.Closed };

        // Build query on DocumentHeaders
        var headersQuery = _context.DocumentHeaders
            .AsNoTracking()
            .Include(dh => dh.DocumentType)
            .Include(dh => dh.Rows!.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Product)
            .Where(dh => dh.TenantId == currentTenantId.Value
                      && !dh.IsDeleted
                      && (approvalStatusFilter.Contains(dh.ApprovalStatus)
                          || documentStatusFilter.Contains(dh.Status)));

        if (request.FromDate.HasValue)
            headersQuery = headersQuery.Where(dh => dh.Date >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            headersQuery = headersQuery.Where(dh => dh.Date <= request.ToDate.Value);

        if (request.WarehouseId.HasValue)
            headersQuery = headersQuery.Where(dh =>
                dh.SourceWarehouseId == request.WarehouseId.Value ||
                dh.DestinationWarehouseId == request.WarehouseId.Value);

        if (request.DocumentTypeId.HasValue)
            headersQuery = headersQuery.Where(dh => dh.DocumentTypeId == request.DocumentTypeId.Value);

        var documentHeaders = await headersQuery.ToListAsync(cancellationToken);
        result.DocumentsScanned = documentHeaders.Count;

        if (documentHeaders.Count == 0)
        {
            return result;
        }

        // Collect all eligible row IDs up-front to batch the movement-existence check
        var allEligibleRows = documentHeaders
            .Where(dh => dh.DocumentType != null && dh.Rows != null)
            .SelectMany(dh => dh.Rows!.Where(r => !r.IsDeleted && r.ProductId.HasValue))
            .ToList();

        var allRowIds = allEligibleRows.Select(r => r.Id).ToHashSet();

        // Batch query: which of these rows already have a movement?
        var rowIdsWithMovement = await _context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.DocumentRowId.HasValue && allRowIds.Contains(sm.DocumentRowId!.Value) && !sm.IsDeleted)
            .Select(sm => sm.DocumentRowId!.Value)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        // Pre-fetch all storage locations for warehouses referenced by these documents
        var allWarehouseIds = documentHeaders
            .Where(dh => dh.DocumentType != null)
            .SelectMany(dh => new[]
            {
                dh.SourceWarehouseId,
                dh.DestinationWarehouseId,
                dh.DocumentType!.DefaultWarehouseId
            })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        // Also add row-level warehouse IDs
        foreach (var row in allEligibleRows)
        {
            if (row.SourceWarehouseId.HasValue) allWarehouseIds.Add(row.SourceWarehouseId.Value);
            if (row.DestinationWarehouseId.HasValue) allWarehouseIds.Add(row.DestinationWarehouseId.Value);
        }

        // Batch: fetch first storage location per warehouse
        var storageLocationsByWarehouse = await _context.StorageLocations
            .AsNoTracking()
            .Where(sl => allWarehouseIds.Contains(sl.WarehouseId) && !sl.IsDeleted)
            .GroupBy(sl => sl.WarehouseId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.First().Id,
                cancellationToken);

        foreach (var documentHeader in documentHeaders)
        {
            if (documentHeader.DocumentType == null || documentHeader.Rows == null)
                continue;

            var eligibleRows = documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue).ToList();

            foreach (var row in eligibleRows)
            {
                result.RowsScanned++;

                // Per-row guard: check if a movement already exists for this row
                if (rowIdsWithMovement.Contains(row.Id))
                {
                    result.RowsAlreadyHadMovement++;
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = documentHeader.Id,
                        DocumentNumber = documentHeader.Number,
                        DocumentRowId = row.Id,
                        ProductId = row.ProductId,
                        ProductName = row.Product?.Name,
                        Quantity = row.BaseQuantity ?? row.Quantity,
                        Status = "AlreadyExists"
                    });
                    continue;
                }

                bool isInbound = documentHeader.DocumentType.IsStockIncrease;

                // Determine storage location ID
                Guid? storageLocationId = null;

                if (row.LocationId.HasValue)
                {
                    // Row has an explicit storage location
                    storageLocationId = row.LocationId.Value;
                }
                else
                {
                    // Determine warehouse ID, then find first storage location in it (from pre-fetched cache)
                    Guid? warehouseId = isInbound
                        ? row.DestinationWarehouseId ?? documentHeader.DestinationWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId
                        : row.SourceWarehouseId ?? documentHeader.SourceWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId;

                    if (warehouseId.HasValue && storageLocationsByWarehouse.TryGetValue(warehouseId.Value, out var locId))
                    {
                        storageLocationId = locId;
                    }
                }

                if (!storageLocationId.HasValue)
                {
                    result.RowsSkippedNoLocation++;
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = documentHeader.Id,
                        DocumentNumber = documentHeader.Number,
                        DocumentRowId = row.Id,
                        ProductId = row.ProductId,
                        ProductName = row.Product?.Name,
                        Quantity = row.BaseQuantity ?? row.Quantity,
                        Status = "SkippedNoLocation"
                    });
                    continue;
                }

                var quantity = row.BaseQuantity ?? row.Quantity;
                var movementDate = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);

                if (!request.DryRun)
                {
                    try
                    {
                        if (isInbound)
                        {
                            await _stockMovementService.ProcessInboundMovementAsync(
                                productId: row.ProductId!.Value,
                                toLocationId: storageLocationId.Value,
                                quantity: quantity,
                                lotId: null,
                                serialId: null,
                                documentHeaderId: documentHeader.Id,
                                documentRowId: row.Id,
                                notes: $"Rebuilt missing movement for document {documentHeader.Number} row {row.Id}",
                                currentUser: currentUser,
                                movementDate: movementDate,
                                cancellationToken: CancellationToken.None);
                        }
                        else
                        {
                            await _stockMovementService.ProcessOutboundMovementAsync(
                                productId: row.ProductId!.Value,
                                fromLocationId: storageLocationId.Value,
                                quantity: quantity,
                                lotId: null,
                                serialId: null,
                                documentHeaderId: documentHeader.Id,
                                documentRowId: row.Id,
                                notes: $"Rebuilt missing movement for document {documentHeader.Number} row {row.Id}",
                                currentUser: currentUser,
                                movementDate: movementDate,
                                cancellationToken: CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating rebuilt movement for document {DocumentId} row {RowId}",
                            documentHeader.Id, row.Id);
                        result.Errors++;
                        result.Items.Add(new RebuildMovementsRowResultDto
                        {
                            DocumentHeaderId = documentHeader.Id,
                            DocumentNumber = documentHeader.Number,
                            DocumentRowId = row.Id,
                            ProductId = row.ProductId,
                            ProductName = row.Product?.Name,
                            Quantity = quantity,
                            Status = "Error",
                            ErrorMessage = ex.Message,
                            MovementType = isInbound ? "Inbound" : "Outbound"
                        });
                        continue;
                    }
                }

                result.MovementsCreated++;
                result.Items.Add(new RebuildMovementsRowResultDto
                {
                    DocumentHeaderId = documentHeader.Id,
                    DocumentNumber = documentHeader.Number,
                    DocumentRowId = row.Id,
                    ProductId = row.ProductId,
                    ProductName = row.Product?.Name,
                    Quantity = quantity,
                    Status = "Created",
                    MovementType = isInbound ? "Inbound" : "Outbound"
                });
            }
        }

        _logger.LogInformation(
            "RebuildMissingMovements: scanned {docs} documents, {rows} rows. Created: {created}, AlreadyExists: {exists}, SkippedNoLocation: {skipped}, Errors: {errors}. DryRun={dryRun}",
            result.DocumentsScanned, result.RowsScanned, result.MovementsCreated,
            result.RowsAlreadyHadMovement, result.RowsSkippedNoLocation, result.Errors, result.IsDryRun);

        return result;
    }
}
