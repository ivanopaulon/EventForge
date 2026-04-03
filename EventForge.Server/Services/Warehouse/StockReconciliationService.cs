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
            var allManualMovements = new List<StockMovement>();
            if (request.IncludeStockMovements)
            {
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

                allManualMovements = await manualMovQuery.ToListAsync(cancellationToken);
            }

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

        // Phase 1 (in-memory): build the lists of movements to create and update
        var movementsToCreate = new List<(CreateStockMovementDto Dto, Guid DocHeaderId, string? DocNumber, Guid DocRowId, Guid? ProductId, string? ProductName, decimal Quantity, bool IsInbound)>();
        var movementsToUpdate = new List<(CreateStockMovementDto Dto, Guid DocHeaderId, string? DocNumber, Guid DocRowId, Guid? ProductId, string? ProductName, decimal Quantity, bool IsInbound)>();

        foreach (var documentHeader in documentHeaders)
        {
            if (documentHeader.DocumentType == null || documentHeader.Rows == null)
                continue;

            var eligibleRows = documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue).ToList();

            foreach (var row in eligibleRows)
            {
                result.RowsScanned++;

                bool alreadyExists = rowIdsWithMovement.Contains(row.Id);

                // Quick skip for existing movements when update is not requested
                if (alreadyExists && !request.UpdateExisting)
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

                Guid? storageLocationId = null;

                if (row.LocationId.HasValue)
                {
                    storageLocationId = row.LocationId.Value;
                }
                else
                {
                    Guid? warehouseId = isInbound
                        ? row.DestinationWarehouseId ?? documentHeader.DestinationWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId
                        : row.SourceWarehouseId ?? documentHeader.SourceWarehouseId ?? documentHeader.DocumentType.DefaultWarehouseId;

                    if (warehouseId.HasValue && storageLocationsByWarehouse.TryGetValue(warehouseId.Value, out var locId))
                        storageLocationId = locId;
                }

                if (!storageLocationId.HasValue)
                {
                    result.RowsSkippedNoLocation++;
                    if (alreadyExists) result.RowsAlreadyHadMovement++;
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = documentHeader.Id,
                        DocumentNumber = documentHeader.Number,
                        DocumentRowId = row.Id,
                        ProductId = row.ProductId,
                        ProductName = row.Product?.Name,
                        Quantity = row.BaseQuantity ?? row.Quantity,
                        Status = alreadyExists ? "AlreadyExists" : "SkippedNoLocation"
                    });
                    continue;
                }

                var quantity = row.BaseQuantity ?? row.Quantity;
                var movementDate = DateTime.SpecifyKind(documentHeader.Date, DateTimeKind.Utc);

                var dto = new CreateStockMovementDto
                {
                    MovementType = (isInbound ? StockMovementType.Inbound : StockMovementType.Outbound).ToString(),
                    ProductId = row.ProductId!.Value,
                    FromLocationId = isInbound ? null : storageLocationId,
                    ToLocationId = isInbound ? storageLocationId : null,
                    Quantity = quantity,
                    DocumentHeaderId = documentHeader.Id,
                    DocumentRowId = row.Id,
                    Notes = alreadyExists
                        ? $"Aggiornamento movimento per documento {documentHeader.Number} riga {row.Id}"
                        : $"Rebuilt missing movement for document {documentHeader.Number} row {row.Id}",
                    Reason = isInbound ? "Purchase" : "Sale",
                    MovementDate = movementDate
                };

                if (alreadyExists)
                    movementsToUpdate.Add((dto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, isInbound));
                else
                    movementsToCreate.Add((dto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, isInbound));
            }
        }

        // Phase 2: batch DB operations

        // Phase 2a: Update existing movements (UpdateExisting = true)
        if (movementsToUpdate.Count > 0)
        {
            if (!request.DryRun)
            {
                // Batch-load existing StockMovement entities by DocumentRowId
                var rowIdsToUpdate = movementsToUpdate.Select(m => m.DocRowId).ToHashSet();
                var existingByRowId = await _context.StockMovements
                    .Where(sm => sm.DocumentRowId.HasValue && rowIdsToUpdate.Contains(sm.DocumentRowId!.Value) && !sm.IsDeleted)
                    .ToDictionaryAsync(sm => sm.DocumentRowId!.Value, cancellationToken);

                // Pre-load all stock records that may need adjustment
                var updateProductIds = movementsToUpdate.Select(m => m.Dto.ProductId).ToHashSet();
                var updateLocationIds = movementsToUpdate
                    .SelectMany(m => new[] { m.Dto.FromLocationId, m.Dto.ToLocationId })
                    .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
                foreach (var ex in existingByRowId.Values)
                {
                    if (ex.FromLocationId.HasValue) updateLocationIds.Add(ex.FromLocationId.Value);
                    if (ex.ToLocationId.HasValue) updateLocationIds.Add(ex.ToLocationId.Value);
                }

                var stocksForUpdate = await _context.Stocks
                    .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted
                                && updateProductIds.Contains(s.ProductId)
                                && updateLocationIds.Contains(s.StorageLocationId))
                    .ToListAsync(cancellationToken);
                var stockForUpdateByKey = stocksForUpdate.ToDictionary(s => (s.ProductId, s.StorageLocationId));

                const int updateChunkSize = 25;
                var updatedCount = 0;
                try
                {
                    foreach (var updateChunk in movementsToUpdate.Chunk(updateChunkSize))
                    {
                        foreach (var (dto, docHeaderId, docNumber, docRowId, productId, productName, quantity, isInbound) in updateChunk)
                        {
                            if (!existingByRowId.TryGetValue(docRowId, out var existing))
                            {
                                result.Errors++;
                                result.Items.Add(new RebuildMovementsRowResultDto
                                {
                                    DocumentHeaderId = docHeaderId,
                                    DocumentNumber = docNumber,
                                    DocumentRowId = docRowId,
                                    ProductId = productId,
                                    ProductName = productName,
                                    Quantity = quantity,
                                    Status = "Error",
                                    ErrorMessage = "Movimento esistente non trovato durante l'aggiornamento.",
                                    MovementType = isInbound ? "Inbound" : "Outbound"
                                });
                                continue;
                            }

                            var newType = Enum.Parse<StockMovementType>(dto.MovementType);
                            var oldQty = existing.Quantity;
                            var oldType = existing.MovementType;
                            var oldFrom = existing.FromLocationId;
                            var oldTo = existing.ToLocationId;

                            bool needsStockAdjust = oldQty != dto.Quantity
                                || oldType != newType
                                || oldFrom != dto.FromLocationId
                                || oldTo != dto.ToLocationId;

                            if (needsStockAdjust)
                            {
                                ApplyMovementStockDelta(stockForUpdateByKey, existing.ProductId, oldType, oldFrom, oldTo, oldQty, reverse: true);
                                ApplyMovementStockDelta(stockForUpdateByKey, existing.ProductId, newType, dto.FromLocationId, dto.ToLocationId, dto.Quantity, reverse: false);
                                existing.Quantity = dto.Quantity;
                                existing.MovementType = newType;
                                existing.FromLocationId = dto.FromLocationId;
                                existing.ToLocationId = dto.ToLocationId;
                            }

                            existing.MovementDate = dto.MovementDate;
                            existing.Notes = dto.Notes;
                            existing.ModifiedAt = DateTime.UtcNow;
                            existing.ModifiedBy = currentUser ?? "System";
                            updatedCount++;
                        }

                        _ = await _context.SaveChangesAsync(cancellationToken);
                    }

                    result.MovementsUpdated += updatedCount;
                    foreach (var m in movementsToUpdate.Where(_ => updatedCount > 0 || result.Errors == 0))
                    {
                        if (existingByRowId.ContainsKey(m.DocRowId))
                        {
                            result.Items.Add(new RebuildMovementsRowResultDto
                            {
                                DocumentHeaderId = m.DocHeaderId,
                                DocumentNumber = m.DocNumber,
                                DocumentRowId = m.DocRowId,
                                ProductId = m.ProductId,
                                ProductName = m.ProductName,
                                Quantity = m.Quantity,
                                Status = "Updated",
                                MovementType = m.IsInbound ? "Inbound" : "Outbound"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in batch movement update during rebuild");
                    result.Errors += movementsToUpdate.Count - updatedCount;
                    foreach (var m in movementsToUpdate)
                    {
                        if (!result.Items.Any(i => i.DocumentRowId == m.DocRowId))
                        {
                            result.Items.Add(new RebuildMovementsRowResultDto
                            {
                                DocumentHeaderId = m.DocHeaderId,
                                DocumentNumber = m.DocNumber,
                                DocumentRowId = m.DocRowId,
                                ProductId = m.ProductId,
                                ProductName = m.ProductName,
                                Quantity = m.Quantity,
                                Status = "Error",
                                ErrorMessage = ex.Message,
                                MovementType = m.IsInbound ? "Inbound" : "Outbound"
                            });
                        }
                    }
                }
            }
            else
            {
                // DryRun: report what would be updated
                result.MovementsUpdated += movementsToUpdate.Count;
                foreach (var m in movementsToUpdate)
                {
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = m.DocHeaderId,
                        DocumentNumber = m.DocNumber,
                        DocumentRowId = m.DocRowId,
                        ProductId = m.ProductId,
                        ProductName = m.ProductName,
                        Quantity = m.Quantity,
                        Status = "Updated",
                        MovementType = m.IsInbound ? "Inbound" : "Outbound"
                    });
                }
            }
        }

        // Phase 2b: Create missing movements
        if (movementsToCreate.Count > 0)
        {
            if (!request.DryRun)
            {
                // Pre-fetch stocks for all outbound (productId, locationId) pairs in a single query,
                // then batch-create zero-quantity records where missing to avoid per-row roundtrips.
                var outboundMovements = movementsToCreate.Where(m => !m.IsInbound).ToList();
                if (outboundMovements.Count > 0)
                {
                    var outboundProductIds = outboundMovements.Select(m => m.Dto.ProductId).ToHashSet();
                    var outboundLocationIds = outboundMovements.Select(m => m.Dto.FromLocationId!.Value).ToHashSet();

                    var existingStockKeys = (await _context.Stocks
                        .Where(s => s.TenantId == currentTenantId.Value
                                    && !s.IsDeleted
                                    && outboundProductIds.Contains(s.ProductId)
                                    && outboundLocationIds.Contains(s.StorageLocationId))
                        .Select(s => new { s.ProductId, s.StorageLocationId })
                        .ToListAsync(cancellationToken))
                        .Select(s => (s.ProductId, s.StorageLocationId))
                        .ToHashSet();

                    var missingStockPairs = outboundMovements
                        .Select(m => (ProductId: m.Dto.ProductId, LocationId: m.Dto.FromLocationId!.Value))
                        .Distinct()
                        .Where(pair => !existingStockKeys.Contains((pair.ProductId, pair.LocationId)))
                        .ToList();

                    if (missingStockPairs.Count > 0)
                    {
                        _logger.LogWarning(
                            "Rebuild: creating {Count} zero-quantity stock record(s) for outbound movements with no existing stock.",
                            missingStockPairs.Count);

                        _context.Stocks.AddRange(missingStockPairs.Select(pair => new Stock
                        {
                            TenantId = currentTenantId.Value,
                            ProductId = pair.ProductId,
                            StorageLocationId = pair.LocationId,
                            Quantity = 0,
                            ReservedQuantity = 0,
                            LastMovementDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUser ?? "System"
                        }));
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                // Batch create all movements: single SaveChangesAsync + single audit log entry
                try
                {
                    await _stockMovementService.CreateMovementsBatchAsync(
                        movementsToCreate.Select(m => m.Dto),
                        currentUser,
                        cancellationToken);

                    result.MovementsCreated += movementsToCreate.Count;
                    foreach (var m in movementsToCreate)
                    {
                        result.Items.Add(new RebuildMovementsRowResultDto
                        {
                            DocumentHeaderId = m.DocHeaderId,
                            DocumentNumber = m.DocNumber,
                            DocumentRowId = m.DocRowId,
                            ProductId = m.ProductId,
                            ProductName = m.ProductName,
                            Quantity = m.Quantity,
                            Status = "Created",
                            MovementType = m.IsInbound ? "Inbound" : "Outbound"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in batch movement creation during rebuild");
                    result.Errors += movementsToCreate.Count;
                    foreach (var m in movementsToCreate)
                    {
                        result.Items.Add(new RebuildMovementsRowResultDto
                        {
                            DocumentHeaderId = m.DocHeaderId,
                            DocumentNumber = m.DocNumber,
                            DocumentRowId = m.DocRowId,
                            ProductId = m.ProductId,
                            ProductName = m.ProductName,
                            Quantity = m.Quantity,
                            Status = "Error",
                            ErrorMessage = ex.Message,
                            MovementType = m.IsInbound ? "Inbound" : "Outbound"
                        });
                    }
                }
            }
            else
            {
                // DryRun: report what would be created without executing any DB writes
                result.MovementsCreated += movementsToCreate.Count;
                foreach (var m in movementsToCreate)
                {
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = m.DocHeaderId,
                        DocumentNumber = m.DocNumber,
                        DocumentRowId = m.DocRowId,
                        ProductId = m.ProductId,
                        ProductName = m.ProductName,
                        Quantity = m.Quantity,
                        Status = "Created",
                        MovementType = m.IsInbound ? "Inbound" : "Outbound"
                    });
                }
            }
        }

        // Phase 3: Recalculate stock levels from the complete movement history for every
        // product/location pair touched by this rebuild. This is the authoritative fix for:
        //  a) Phase 2a: ApplyMovementStockDelta silently skips pairs absent from the
        //     pre-loaded dictionary (e.g. when the Stock table was empty/reset).
        //  b) Phase 2b: UpdateStockLevelsForMovementAsync may insert duplicate Stock rows
        //     for multiple inbound movements to the same location within one SaveChanges chunk.
        if (!request.DryRun)
        {
            var affectedPairs = new HashSet<(Guid ProductId, Guid LocationId)>();
            foreach (var m in movementsToCreate.Concat(movementsToUpdate))
            {
                if (m.Dto.ToLocationId.HasValue)
                    affectedPairs.Add((m.Dto.ProductId, m.Dto.ToLocationId.Value));
                if (m.Dto.FromLocationId.HasValue)
                    affectedPairs.Add((m.Dto.ProductId, m.Dto.FromLocationId.Value));
            }
            if (affectedPairs.Count > 0)
            {
                await RecalculateStockForAffectedPairsAsync(affectedPairs, currentTenantId.Value, currentUser, cancellationToken);
                _logger.LogInformation("RebuildMissingMovements Phase 3: recalculated stock for {Count} product/location pair(s).", affectedPairs.Count);
            }
        }

        _logger.LogInformation(
            "RebuildMissingMovements: scanned {docs} documents, {rows} rows. Created: {created}, Updated: {updated}, AlreadyExists: {exists}, SkippedNoLocation: {skipped}, Errors: {errors}. DryRun={dryRun}",
            result.DocumentsScanned, result.RowsScanned, result.MovementsCreated, result.MovementsUpdated,
            result.RowsAlreadyHadMovement, result.RowsSkippedNoLocation, result.Errors, result.IsDryRun);

        return result;
    }

    /// <summary>
    /// Applies or reverses the stock-level impact of a movement on the pre-loaded stock dictionary.
    /// Used when updating existing movements to keep stock quantities consistent.
    /// </summary>
    private static void ApplyMovementStockDelta(
        Dictionary<(Guid ProductId, Guid LocationId), Stock> stockByKey,
        Guid productId,
        StockMovementType movementType,
        Guid? fromLocationId,
        Guid? toLocationId,
        decimal quantity,
        bool reverse)
    {
        decimal sign = reverse ? -1m : 1m;

        if ((movementType == StockMovementType.Inbound || movementType == StockMovementType.Transfer)
            && toLocationId.HasValue
            && stockByKey.TryGetValue((productId, toLocationId.Value), out var toStock))
        {
            toStock.Quantity += sign * quantity;
            toStock.ModifiedAt = DateTime.UtcNow;
        }

        if ((movementType == StockMovementType.Outbound || movementType == StockMovementType.Transfer)
            && fromLocationId.HasValue
            && stockByKey.TryGetValue((productId, fromLocationId.Value), out var fromStock))
        {
            fromStock.Quantity -= sign * quantity;
            fromStock.ModifiedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Recalculates stock levels from the complete movement history for the given
    /// product/location pairs and upserts the Stock table accordingly.
    /// Handles deduplication of Stock rows that may have been inserted in parallel
    /// by an earlier phase of the rebuild.
    /// </summary>
    private async Task RecalculateStockForAffectedPairsAsync(
        HashSet<(Guid ProductId, Guid LocationId)> pairs,
        Guid tenantId,
        string? currentUser,
        CancellationToken cancellationToken)
    {
        var productIds = pairs.Select(p => p.ProductId).ToHashSet();
        var locationIds = pairs.Select(p => p.LocationId).ToHashSet();

        // Movements that bring stock INTO a location
        var inboundTotals = await _context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.ToLocationId.HasValue
                         && productIds.Contains(sm.ProductId)
                         && locationIds.Contains(sm.ToLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => sm.Quantity) })
            .ToListAsync(cancellationToken);

        // Movements that take stock FROM a location
        var outboundTotals = await _context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.FromLocationId.HasValue
                         && productIds.Contains(sm.ProductId)
                         && locationIds.Contains(sm.FromLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => sm.Quantity) })
            .ToListAsync(cancellationToken);

        // Compute net quantity per (ProductId, LocationId)
        var netByPair = new Dictionary<(Guid, Guid), decimal>();
        foreach (var item in inboundTotals)
            netByPair[(item.ProductId, item.LocationId)] = item.Total;
        foreach (var item in outboundTotals)
        {
            var key = (item.ProductId, item.LocationId);
            netByPair[key] = netByPair.GetValueOrDefault(key, 0m) - item.Total;
        }

        // Load all active Stock records for the affected pairs
        var existingStocks = await _context.Stocks
            .Where(s => s.TenantId == tenantId
                        && productIds.Contains(s.ProductId)
                        && locationIds.Contains(s.StorageLocationId))
            .ToListAsync(cancellationToken);

        var stocksByKey = existingStocks
            .GroupBy(s => (s.ProductId, s.StorageLocationId))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var pair in pairs)
        {
            var netQty = netByPair.GetValueOrDefault(pair, 0m);

            if (stocksByKey.TryGetValue(pair, out var stocks))
            {
                // Update the canonical record; soft-delete any duplicates created by earlier phases
                var primary = stocks[0];
                primary.Quantity = netQty;
                primary.LastMovementDate = DateTime.UtcNow;
                primary.ModifiedAt = DateTime.UtcNow;
                primary.ModifiedBy = currentUser ?? "System";

                for (var i = 1; i < stocks.Count; i++)
                {
                    stocks[i].IsDeleted = true;
                    stocks[i].DeletedAt = DateTime.UtcNow;
                    stocks[i].DeletedBy = currentUser ?? "System";
                }
            }
            else if (netQty != 0m)
            {
                // No stock record exists yet — create one with the recalculated quantity
                _context.Stocks.Add(new Stock
                {
                    TenantId = tenantId,
                    ProductId = pair.ProductId,
                    StorageLocationId = pair.LocationId,
                    Quantity = netQty,
                    ReservedQuantity = 0,
                    LastMovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser ?? "System"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
