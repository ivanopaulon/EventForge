using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for stock reconciliation operations.
/// </summary>
public class StockReconciliationService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    IStockMovementService stockMovementService,
    ITenantContext tenantContext,
    ILogger<StockReconciliationService> logger) : IStockReconciliationService
{
    private const int ReconciliationAuditDisplayNameMaxLength = 500;

    public async Task<StockReconciliationResultDto> CalculateReconciledStockAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = GetCurrentTenantId();
        ValidateRequest(request);

        var stocks = await BuildFilteredStocksQuery(currentTenantId, request, includeDetails: true)
            .ToListAsync(cancellationToken);

        return await CalculateForStocksAsync(currentTenantId, request, stocks, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetStockIdsForReconciliationAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = GetCurrentTenantId();
        ValidateRequest(request);

        return await BuildFilteredStocksQuery(currentTenantId, request, includeDetails: false)
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<StockReconciliationResultDto> CalculateReconciledStockForStocksAsync(
        IReadOnlyCollection<Guid> stockIds,
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stockIds);

        if (stockIds.Count == 0)
        {
            return new StockReconciliationResultDto
            {
                Summary = new StockReconciliationSummaryDto()
            };
        }

        var currentTenantId = GetCurrentTenantId();
        ValidateRequest(request);

        var stocks = await BuildFilteredStocksQuery(currentTenantId, request, stockIds, includeDetails: true)
            .ToListAsync(cancellationToken);

        return await CalculateForStocksAsync(currentTenantId, request, stocks, cancellationToken);
    }

    private Guid GetCurrentTenantId()
        => tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

    private static void ValidateRequest(StockReconciliationRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate cannot be after ToDate.");
        }
    }

    private IQueryable<Data.Entities.Warehouse.Stock> BuildFilteredStocksQuery(
        Guid tenantId,
        StockReconciliationRequestDto request,
        IReadOnlyCollection<Guid>? stockIds = null,
        bool includeDetails = true)
    {
        IQueryable<Data.Entities.Warehouse.Stock> stocksQuery = context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IsDeleted);

        if (includeDetails)
        {
            stocksQuery = stocksQuery
                .Include(s => s.Product)
                .Include(s => s.StorageLocation)
                    .ThenInclude(sl => sl!.Warehouse);
        }

        if (stockIds is { Count: > 0 })
        {
            // Defensive sanitization: tolerate duplicates and empty identifiers from client payloads.
            var stockIdList = stockIds
                .Distinct()
                .Where(id => id != Guid.Empty)
                .ToList();

            if (stockIdList.Count == 0)
            {
                return stocksQuery.Where(_ => false);
            }

            stocksQuery = stocksQuery.Where(s => stockIdList.Contains(s.Id));
        }

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

        return stocksQuery;
    }

    private async Task<StockReconciliationResultDto> CalculateForStocksAsync(
        Guid tenantId,
        StockReconciliationRequestDto request,
        List<Data.Entities.Warehouse.Stock> stocks,
        CancellationToken cancellationToken)
    {
        var result = new StockReconciliationResultDto();

        if (stocks.Count == 0)
        {
            result.Summary = CalculateSummary(result.Items);
            return result;
        }

        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var locationIds = stocks.Select(s => s.StorageLocationId).Distinct().ToList();
        // Warehouse IDs needed for the fallback inventory matching when LocationId is null on document rows.
        var warehouseIds = stocks
            .Where(s => s.StorageLocation != null)
            .Select(s => s.StorageLocation!.WarehouseId)
            .Distinct()
            .ToList();

        var allInventoryRows = new List<Data.Entities.Documents.DocumentRow>();
        if (request.IncludeInventories)
        {
            var invQuery = context.DocumentRows
                .AsNoTracking()
                .Include(dr => dr.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .Where(dr => dr.TenantId == tenantId &&
                            !dr.IsDeleted &&
                            dr.ProductId.HasValue && productIds.Contains(dr.ProductId.Value) &&
                            dr.DocumentHeader != null &&
                            dr.DocumentHeader.DocumentType != null &&
                            dr.DocumentHeader.DocumentType.IsInventoryDocument &&
                            (dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived) &&
                            // Primary match: exact storage location.
                            // Fallback match: when LocationId is null on the row (legacy inventory data),
                            // match using the warehouse from the row or document header.
                            (dr.LocationId.HasValue && locationIds.Contains(dr.LocationId.Value) ||
                             !dr.LocationId.HasValue && (
                                 (dr.SourceWarehouseId.HasValue && warehouseIds.Contains(dr.SourceWarehouseId.Value)) ||
                                 (dr.DestinationWarehouseId.HasValue && warehouseIds.Contains(dr.DestinationWarehouseId.Value)) ||
                                 (dr.DocumentHeader.SourceWarehouseId.HasValue && warehouseIds.Contains(dr.DocumentHeader.SourceWarehouseId.Value)) ||
                                 (dr.DocumentHeader.DestinationWarehouseId.HasValue && warehouseIds.Contains(dr.DocumentHeader.DestinationWarehouseId.Value)) ||
                                 (dr.DocumentHeader.DocumentType.DefaultWarehouseId.HasValue && warehouseIds.Contains(dr.DocumentHeader.DocumentType.DefaultWarehouseId.Value)))));

            if (request.FromDate.HasValue)
                invQuery = invQuery.Where(dr => dr.DocumentHeader!.Date >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                invQuery = invQuery.Where(dr => dr.DocumentHeader!.Date <= request.ToDate.Value);

            allInventoryRows = await invQuery.ToListAsync(cancellationToken);

            // Diagnostic: warn about null-LocationId rows so operators can identify data quality issues.
            var nullLocationCount = allInventoryRows.Count(dr => !dr.LocationId.HasValue);
            if (nullLocationCount > 0)
            {
                logger.LogWarning(
                    "Found {NullLocationCount} inventory document row(s) with null LocationId; " +
                    "warehouse-level matching will be used as fallback. " +
                    "Consider back-filling LocationId in inventory documents to improve accuracy.",
                    nullLocationCount);
            }
        }

        var allDocumentRows = new List<Data.Entities.Documents.DocumentRow>();
        if (request.IncludeDocuments)
        {
            var docQuery = context.DocumentRows
                .AsNoTracking()
                .Include(dr => dr.DocumentHeader)
                    .ThenInclude(dh => dh!.DocumentType)
                .Where(dr => dr.TenantId == tenantId &&
                            !dr.IsDeleted &&
                            dr.ProductId.HasValue && productIds.Contains(dr.ProductId.Value) &&
                            dr.LocationId.HasValue && locationIds.Contains(dr.LocationId.Value) &&
                            dr.DocumentHeader != null &&
                            dr.DocumentHeader.DocumentType != null &&
                            !dr.DocumentHeader.DocumentType.IsInventoryDocument &&
                            (dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Active ||
                             dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Archived));

            if (request.FromDate.HasValue)
                docQuery = docQuery.Where(dr => dr.DocumentHeader!.Date >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                docQuery = docQuery.Where(dr => dr.DocumentHeader!.Date <= request.ToDate.Value);

            allDocumentRows = await docQuery.ToListAsync(cancellationToken);
        }

        var allManualMovements = new List<StockMovement>();
        if (request.IncludeStockMovements)
        {
            var manualMovQuery = context.StockMovements
                .AsNoTracking()
                .Where(sm => sm.TenantId == tenantId &&
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

        // Primary lookup: exact (ProductId, LocationId) key for rows that have a location.
        var latestInventoryByLocationKey = allInventoryRows
            .Where(dr => dr.LocationId.HasValue)
            .GroupBy(dr => (dr.ProductId!.Value, dr.LocationId!.Value))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(dr => dr.DocumentHeader!.Date).First());

        // Fallback lookup: (ProductId, WarehouseId) for legacy rows that have no LocationId.
        // The warehouse is resolved from the most specific source available on the row or its header.
        var latestInventoryByWarehouseKey = allInventoryRows
            .Where(dr => !dr.LocationId.HasValue)
            .Select(dr => new
            {
                Row = dr,
                WarehouseId = dr.SourceWarehouseId
                    ?? dr.DestinationWarehouseId
                    ?? dr.DocumentHeader!.SourceWarehouseId
                    ?? dr.DocumentHeader!.DestinationWarehouseId
                    ?? dr.DocumentHeader!.DocumentType!.DefaultWarehouseId
            })
            .Where(x => x.WarehouseId.HasValue)
            .GroupBy(x => (ProductId: x.Row.ProductId!.Value, WarehouseId: x.WarehouseId!.Value))
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Row.DocumentHeader!.Date).Select(x => x.Row).First());

        logger.LogInformation(
            "Inventory lookup built: {LocationKeys} location-level entries, {WarehouseKeys} warehouse-level fallback entries.",
            latestInventoryByLocationKey.Count, latestInventoryByWarehouseKey.Count);

        foreach (var stock in stocks)
        {
            var item = CalculateStockItem(stock, request, latestInventoryByLocationKey, latestInventoryByWarehouseKey, allDocumentRows, allManualMovements);

            if (!request.OnlyWithDiscrepancies || item.Severity != ReconciliationSeverity.Correct)
            {
                result.Items.Add(item);
            }
        }

        result.Summary = CalculateSummary(result.Items);

        logger.LogInformation("Reconciliation calculation completed. Total items: {Total}, With discrepancies: {Discrepancies}",
            result.Summary.TotalProducts, result.Summary.TotalProducts - result.Summary.CorrectCount);

        return result;
    }

    /// <summary>
    /// Calculates the reconciliation item for a single stock record using pre-loaded in-memory data.
    /// </summary>
    private StockReconciliationItemDto CalculateStockItem(
        Data.Entities.Warehouse.Stock stock,
        StockReconciliationRequestDto request,
        Dictionary<(Guid ProductId, Guid LocationId), Data.Entities.Documents.DocumentRow> latestInventoryByLocationKey,
        Dictionary<(Guid ProductId, Guid WarehouseId), Data.Entities.Documents.DocumentRow> latestInventoryByWarehouseKey,
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
            // Try exact location match first; fall back to warehouse-level match for legacy rows without LocationId.
            if (!latestInventoryByLocationKey.TryGetValue((stock.ProductId, stock.StorageLocationId), out var lastInventoryRow))
            {
                var warehouseId = stock.StorageLocation?.WarehouseId;
                if (warehouseId.HasValue &&
                    latestInventoryByWarehouseKey.TryGetValue((stock.ProductId, warehouseId.Value), out lastInventoryRow))
                {
                    logger.LogWarning(
                        "Inventory matched at warehouse level (null LocationId) for product {ProductId} in warehouse {WarehouseId}. " +
                        "Consider back-filling LocationId on inventory document rows for more precise matching.",
                        stock.ProductId, warehouseId.Value);
                }
            }

            if (lastInventoryRow is not null)
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
                    logger.LogDebug(
                        "StartingQuantity {Qty} overridden by inventory document for product {ProductId} at location {LocationId}",
                        request.StartingQuantity.Value, stock.ProductId, stock.StorageLocationId);
                }
            }
            else
            {
                logger.LogDebug(
                    "No inventory document found for product {ProductId} at location {LocationId} (warehouse {WarehouseId}). " +
                    "Quantity will be calculated from movements only.",
                    stock.ProductId, stock.StorageLocationId, stock.StorageLocation?.WarehouseId);
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
        //    When documents are already included (IncludeDocuments=true), skip movements that were
        //    generated from a document row to avoid double-counting the same quantity.
        var manualMovements = allManualMovements
            .Where(sm => sm.ProductId == stock.ProductId &&
                         // Exclude technical adjustments created by reconciliation runs.
                         // IsReconciliationAdjustment is the canonical flag (set since the flag was introduced).
                         !sm.IsReconciliationAdjustment &&
                         // Fallback for legacy movements created before the flag was introduced:
                         // movements with Reference containing "Stock Reconciliation" are also excluded.
                         !(sm.Reference != null && sm.Reference.Contains("Stock Reconciliation", StringComparison.OrdinalIgnoreCase)) &&
                         (!request.IncludeDocuments || !sm.DocumentRowId.HasValue) &&
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
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }


            var result = new StockReconciliationApplyResultDto { Success = true };

            // Use transaction for atomicity
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
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

                var requestedStockIds = request.ItemsToApply
                    .Where(id => id != Guid.Empty)
                    .ToHashSet();

                var itemsToUpdate = reconciliation.Items
                    .Where(i => requestedStockIds.Contains(i.StockId))
                    .ToList();

                var stockIdsToUpdate = itemsToUpdate.Select(i => i.StockId).ToList();
                var stocksById = await context.Stocks
                    .Include(s => s.Product)
                    .Include(s => s.StorageLocation)
                    .Where(s => stockIdsToUpdate.Contains(s.Id) && s.TenantId == currentTenantId.Value)
                    .ToDictionaryAsync(s => s.Id, cancellationToken);

                var auditEntries = new List<EntityChangeLog>(itemsToUpdate.Count);
                var reconciliationRunId = Guid.NewGuid();

                foreach (var item in itemsToUpdate)
                {
                    if (!stocksById.TryGetValue(item.StockId, out var stock))
                    {
                        logger.LogWarning("Stock {StockId} not found", item.StockId);
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
                        await stockMovementService.ProcessAdjustmentMovementAsync(
                            productId: stock.ProductId,
                            locationId: stock.StorageLocationId,
                            adjustmentQuantity: adjustment,
                            reason: "Stock Reconciliation",
                            lotId: stock.LotId,
                            notes: $"{request.Reason}. Adjusted from {oldQuantity} to {newQuantity}",
                            currentUser: currentUser,
                            movementDate: DateTime.UtcNow,
                            isReconciliationAdjustment: true,
                            reconciliationRunId: reconciliationRunId,
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

                    var entityDisplayName = BuildReconciliationAuditDisplayName(
                        stock.Product?.Name,
                        stock.StorageLocation?.Code,
                        request.Reason);

                    auditEntries.Add(new EntityChangeLog
                    {
                        EntityName = "Stock",
                        EntityId = stock.Id,
                        PropertyName = "Quantity",
                        OperationType = "Reconciliation",
                        OldValue = oldQuantity.ToString(),
                        NewValue = newQuantity.ToString(),
                        ChangedBy = currentUser,
                        ChangedAt = DateTime.UtcNow,
                        TenantId = currentTenantId.Value,
                        EntityDisplayName = entityDisplayName
                    });
                }

                if (auditEntries.Count > 0)
                {
                    context.EntityChangeLogs.AddRange(auditEntries);
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation("Stock reconciliation applied successfully. Updated: {Updated}, Movements: {Movements}",
                    result.UpdatedCount, result.MovementsCreated);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error applying stock reconciliation, transaction rolled back");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ApplyReconciliationAsync");
            return new StockReconciliationApplyResultDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string BuildReconciliationAuditDisplayName(string? productName, string? locationCode, string? reason)
    {
        var displayName = $"{productName ?? "Unknown"} @ {locationCode ?? "Unknown"}";
        if (!string.IsNullOrWhiteSpace(reason))
        {
            displayName = $"{displayName} - {reason}";
        }

        if (displayName.Length <= ReconciliationAuditDisplayNameMaxLength)
        {
            return displayName;
        }

        return displayName[..(ReconciliationAuditDisplayNameMaxLength - 1)] + "…";
    }

    public async Task<byte[]> ExportReconciliationReportAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await CalculateReconciledStockAsync(request, cancellationToken);

        using var workbook = new XLWorkbook();

        // ── Sheet 1: Summary ──────────────────────────────────────────────────
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Stock Reconciliation Report";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 14;

        var generatedAt = DateTime.UtcNow;
        summarySheet.Cell(2, 1).Value = "Generated At (UTC)";
        summarySheet.Cell(2, 2).Value = generatedAt.ToString("yyyy-MM-dd HH:mm:ss");

        if (request.FromDate.HasValue)
        {
            summarySheet.Cell(3, 1).Value = "From Date";
            summarySheet.Cell(3, 2).Value = request.FromDate.Value.ToString("yyyy-MM-dd");
        }
        if (request.ToDate.HasValue)
        {
            summarySheet.Cell(4, 1).Value = "To Date";
            summarySheet.Cell(4, 2).Value = request.ToDate.Value.ToString("yyyy-MM-dd");
        }

        int summaryRow = 6;
        summarySheet.Cell(summaryRow, 1).Value = "Metric";
        summarySheet.Cell(summaryRow, 2).Value = "Value";
        summarySheet.Cell(summaryRow, 1).Style.Font.Bold = true;
        summarySheet.Cell(summaryRow, 2).Style.Font.Bold = true;

        var summaryData = new[]
        {
            ("Total Products Analyzed", (object)result.Summary.TotalProducts),
            ("Correct (No Discrepancy)", result.Summary.CorrectCount),
            ("Minor Discrepancies (< Threshold)", result.Summary.MinorDiscrepancyCount),
            ("Major Discrepancies (>= Threshold)", result.Summary.MajorDiscrepancyCount),
            ("Missing Stock", result.Summary.MissingCount),
            ("Total Absolute Difference", result.Summary.TotalDifferenceValue)
        };

        foreach (var (label, value) in summaryData)
        {
            summaryRow++;
            summarySheet.Cell(summaryRow, 1).Value = label;
            summarySheet.Cell(summaryRow, 2).Value = value?.ToString() ?? string.Empty;
        }

        summarySheet.Column(1).AdjustToContents();
        summarySheet.Column(2).AdjustToContents();

        // ── Sheet 2: Details ──────────────────────────────────────────────────
        var detailsSheet = workbook.Worksheets.Add("Details");
        var detailsHeaders = new[]
        {
            "Product Code", "Product Name", "Warehouse", "Location",
            "Current Qty", "Calculated Qty", "Difference", "Diff %",
            "Severity", "Total Docs", "Total Inventories", "Total Manual"
        };
        for (int col = 0; col < detailsHeaders.Length; col++)
        {
            detailsSheet.Cell(1, col + 1).Value = detailsHeaders[col];
            detailsSheet.Cell(1, col + 1).Style.Font.Bold = true;
        }
        detailsSheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;

        int detailRow = 2;
        foreach (var item in result.Items)
        {
            detailsSheet.Cell(detailRow, 1).Value = item.ProductCode;
            detailsSheet.Cell(detailRow, 2).Value = item.ProductName;
            detailsSheet.Cell(detailRow, 3).Value = item.WarehouseName;
            detailsSheet.Cell(detailRow, 4).Value = item.LocationCode;
            detailsSheet.Cell(detailRow, 5).Value = item.CurrentQuantity;
            detailsSheet.Cell(detailRow, 6).Value = item.CalculatedQuantity;
            detailsSheet.Cell(detailRow, 7).Value = item.Difference;
            detailsSheet.Cell(detailRow, 8).Value = item.DifferencePercentage;
            detailsSheet.Cell(detailRow, 9).Value = item.Severity.ToString();
            detailsSheet.Cell(detailRow, 10).Value = item.TotalDocuments;
            detailsSheet.Cell(detailRow, 11).Value = item.TotalInventories;
            detailsSheet.Cell(detailRow, 12).Value = item.TotalManualMovements;

            // Highlight rows with major discrepancies
            if (item.Severity == ReconciliationSeverity.Major || item.Severity == ReconciliationSeverity.Missing)
            {
                detailsSheet.Row(detailRow).Style.Fill.BackgroundColor = XLColor.LightSalmon;
            }
            else if (item.Severity == ReconciliationSeverity.Minor)
            {
                detailsSheet.Row(detailRow).Style.Fill.BackgroundColor = XLColor.LightYellow;
            }

            detailRow++;
        }

        detailsSheet.Columns().AdjustToContents();

        // ── Sheet 3: Movements ────────────────────────────────────────────────
        var movementsSheet = workbook.Worksheets.Add("Movements");
        var movementsHeaders = new[]
        {
            "Product Code", "Product Name", "Movement Type", "Reference", "Quantity", "Date", "Is Replacement"
        };
        for (int col = 0; col < movementsHeaders.Length; col++)
        {
            movementsSheet.Cell(1, col + 1).Value = movementsHeaders[col];
            movementsSheet.Cell(1, col + 1).Style.Font.Bold = true;
        }
        movementsSheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;

        int movRow = 2;
        foreach (var item in result.Items)
        {
            foreach (var mv in item.SourceMovements)
            {
                movementsSheet.Cell(movRow, 1).Value = item.ProductCode;
                movementsSheet.Cell(movRow, 2).Value = item.ProductName;
                movementsSheet.Cell(movRow, 3).Value = mv.Type;
                movementsSheet.Cell(movRow, 4).Value = mv.Reference;
                movementsSheet.Cell(movRow, 5).Value = mv.Quantity;
                movementsSheet.Cell(movRow, 6).Value = mv.Date.ToString("yyyy-MM-dd HH:mm:ss");
                movementsSheet.Cell(movRow, 7).Value = mv.IsReplacement ? "Yes" : "No";
                movRow++;
            }
        }

        movementsSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        logger.LogInformation(
            "Stock reconciliation Excel report generated: {ProductCount} products, {MovementCount} movements",
            result.Items.Count, result.Items.Sum(i => i.SourceMovements.Count));

        return ms.ToArray();
    }

    public async Task<RebuildMovementsResultDto> RebuildMissingMovementsFromDocumentsAsync(
        RebuildMovementsRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate cannot be after ToDate.");
        }

        var result = new RebuildMovementsResultDto { IsDryRun = request.DryRun };

        // Resolve effective status filters (default: Archived)
        var documentStatusFilter = (request.DocumentStatuses is not null && request.DocumentStatuses.Count > 0)
            ? request.DocumentStatuses.Select(v => (Prym.DTOs.Common.DocumentStatus)v).ToList()
            : new List<Prym.DTOs.Common.DocumentStatus> { Prym.DTOs.Common.DocumentStatus.Archived };

        // Build query on DocumentHeaders.
        // A document must satisfy the selected document status filter before its rows
        // are eligible for movement rebuild.
        var headersQuery = context.DocumentHeaders
            .AsNoTracking()
            .Include(dh => dh.DocumentType)
            .Include(dh => dh.Rows!.Where(r => !r.IsDeleted))
                .ThenInclude(r => r.Product)
            .Where(dh => dh.TenantId == currentTenantId.Value
                      && !dh.IsDeleted
                      && documentStatusFilter.Contains(dh.Status)
                      // Inventory documents are quantity anchors, not incremental movements.
                      // Including them would create erroneous Inbound/Outbound movements.
                      && (dh.DocumentType == null || !dh.DocumentType.IsInventoryDocument));

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
        var rowIdsWithMovement = await context.StockMovements
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

        // Batch: fetch the first storage location per warehouse, ordered by Code so the
        // selection is deterministic regardless of insertion order or DB provider.
        // The GroupBy + Select pushes the per-warehouse min-Code selection to the database,
        // loading only the two fields (WarehouseId, LocationId) that are actually needed.
        var storageLocationsByWarehouse = await context.StorageLocations
            .AsNoTracking()
            .Where(sl => allWarehouseIds.Contains(sl.WarehouseId) && !sl.IsDeleted)
            .GroupBy(sl => sl.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                // Lexicographically smallest Code → stable across runs
                LocationId = g.OrderBy(sl => sl.Code).Select(sl => sl.Id).First()
            })
            .ToDictionaryAsync(g => g.WarehouseId, g => g.LocationId, cancellationToken);

        // Phase 1 (in-memory): build the lists of movements to create and update
        var movementsToCreate = new List<(CreateStockMovementDto Dto, Guid DocHeaderId, string? DocNumber, Guid DocRowId, Guid? ProductId, string? ProductName, decimal Quantity, bool IsInbound)>();
        var movementsToUpdate = new List<(CreateStockMovementDto Dto, Guid DocHeaderId, string? DocNumber, Guid DocRowId, Guid? ProductId, string? ProductName, decimal Quantity, bool IsInbound)>();
        // Transfer rows that already have movements: soft-delete old + recreate (handles both
        // the legacy 2-movement pattern and the current single-Transfer pattern safely).
        var transfersToDeleteAndRecreate = new List<(CreateStockMovementDto Dto, Guid DocHeaderId, string? DocNumber, Guid DocRowId, Guid? ProductId, string? ProductName, decimal Quantity, bool IsInbound)>();

        foreach (var documentHeader in documentHeaders)
        {
            if (documentHeader.DocumentType is null || documentHeader.Rows is null)
                continue;

            var eligibleRows = documentHeader.Rows.Where(r => !r.IsDeleted && r.ProductId.HasValue).ToList();
            bool isTransfer = documentHeader.DocumentType.IsTransferDocument;

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

                var quantity = row.BaseQuantity ?? row.Quantity;
                // BaseQuantity stores the quantity in the product's base unit of measure
                // (set by UnitConversionService when the document row has a UOM conversion).
                // Falling back to Quantity is correct when no UOM conversion is configured.
                var docDate = documentHeader.Date;
                var movementDate = docDate.Kind == DateTimeKind.Utc
                    ? docDate
                    : DateTime.SpecifyKind(docDate, DateTimeKind.Utc);
                var unitCost = row.UnitPrice > 0 ? ComputeNetUnitPrice(row) : (decimal?)null;

                if (isTransfer)
                {
                    // Transfer document: generate Outbound (source) + Inbound (destination).
                    // Both are keyed to the same DocumentRowId; when UpdateExisting=true only
                    // the first movement found by DocumentRowId is updated — for transfers the
                    // recommended pattern is to delete+recreate rather than update in place,
                    // so we fall through to the create path when existing == true.
                    var sourceWarehouseId = row.SourceWarehouseId
                        ?? documentHeader.SourceWarehouseId
                        ?? documentHeader.DocumentType.DefaultWarehouseId;
                    var destWarehouseId = row.DestinationWarehouseId
                        ?? documentHeader.DestinationWarehouseId
                        ?? documentHeader.DocumentType.DefaultWarehouseId;

                    Guid? fromLocationId = null;
                    Guid? toLocationId = null;

                    if (row.LocationId.HasValue)
                    {
                        // Row-level location used as destination; source resolved from header
                        toLocationId = row.LocationId.Value;
                    }
                    else
                    {
                        if (sourceWarehouseId.HasValue && storageLocationsByWarehouse.TryGetValue(sourceWarehouseId.Value, out var srcLoc))
                            fromLocationId = srcLoc;
                        if (destWarehouseId.HasValue && storageLocationsByWarehouse.TryGetValue(destWarehouseId.Value, out var dstLoc))
                            toLocationId = dstLoc;
                    }

                    if (!fromLocationId.HasValue && !toLocationId.HasValue)
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
                            Quantity = quantity,
                            UnitCost = unitCost,
                            Status = alreadyExists ? "AlreadyExists" : "SkippedNoLocation",
                            MovementType = "Transfer"
                        });
                        continue;
                    }

                    var transferDto = new CreateStockMovementDto
                    {
                        MovementType = StockMovementType.Transfer.ToString(),
                        ProductId = row.ProductId!.Value,
                        FromLocationId = fromLocationId,
                        ToLocationId = toLocationId,
                        Quantity = quantity,
                        UnitCost = unitCost,
                        DocumentHeaderId = documentHeader.Id,
                        DocumentRowId = row.Id,
                        Notes = alreadyExists
                            ? $"Aggiornamento movimento trasferimento per documento {documentHeader.Number} riga {row.Id}"
                            : $"Rebuilt transfer movement for document {documentHeader.Number} row {row.Id}",
                        Reason = ResolveMovementReason(documentHeader.DocumentType, isInbound: false),
                        MovementDate = movementDate
                    };

                    if (alreadyExists && request.UpdateExisting)
                        // Delete all existing movements for this transfer row and recreate a single
                        // Transfer-type movement.  This correctly handles both the legacy pattern
                        // (two separate Outbound+Inbound movements with the same DocumentRowId)
                        // and the current pattern (one Transfer-type movement per row).
                        transfersToDeleteAndRecreate.Add((transferDto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, false));
                    else if (!alreadyExists)
                        movementsToCreate.Add((transferDto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, false));
                }
                else
                {
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
                            Quantity = quantity,
                            UnitCost = unitCost,
                            Status = alreadyExists ? "AlreadyExists" : "SkippedNoLocation"
                        });
                        continue;
                    }

                    var dto = new CreateStockMovementDto
                    {
                        MovementType = (isInbound ? StockMovementType.Inbound : StockMovementType.Outbound).ToString(),
                        ProductId = row.ProductId!.Value,
                        FromLocationId = isInbound ? null : storageLocationId,
                        ToLocationId = isInbound ? storageLocationId : null,
                        Quantity = quantity,
                        UnitCost = unitCost,
                        DocumentHeaderId = documentHeader.Id,
                        DocumentRowId = row.Id,
                        Notes = alreadyExists
                            ? $"Aggiornamento movimento per documento {documentHeader.Number} riga {row.Id}"
                            : $"Rebuilt missing movement for document {documentHeader.Number} row {row.Id}",
                        Reason = ResolveMovementReason(documentHeader.DocumentType, isInbound),
                        MovementDate = movementDate
                    };

                    if (alreadyExists)
                        movementsToUpdate.Add((dto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, isInbound));
                    else
                        movementsToCreate.Add((dto, documentHeader.Id, documentHeader.Number, row.Id, row.ProductId, row.Product?.Name, quantity, isInbound));
                }
            }
        }

        // Phase 2: batch DB operations

        // Phase 2a-pre: Transfer delete+recreate
        // For transfer document rows that already had movements, soft-delete ALL existing movements
        // (handles legacy dual Outbound+Inbound pattern) and move the DTOs to movementsToCreate
        // so that Phase 2b creates a single canonical Transfer-type movement.
        if (transfersToDeleteAndRecreate.Count > 0)
        {
            if (!request.DryRun)
            {
                var transferRowIdsToDelete = transfersToDeleteAndRecreate.Select(m => m.DocRowId).ToHashSet();
                var existingTransferMovements = await context.StockMovements
                    .Where(sm => sm.DocumentRowId.HasValue
                                 && transferRowIdsToDelete.Contains(sm.DocumentRowId!.Value)
                                 && !sm.IsDeleted)
                    .ToListAsync(cancellationToken);

                if (existingTransferMovements.Count > 0)
                {
                    // Load stock records so we can reverse the impact of the deleted movements.
                    var tProductIds = existingTransferMovements.Select(m => m.ProductId).ToHashSet();
                    var tLocationIds = existingTransferMovements
                        .SelectMany(m => new[] { m.FromLocationId, m.ToLocationId })
                        .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
                    var stocksForTransferDelete = await context.Stocks
                        .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted
                                    && tProductIds.Contains(s.ProductId)
                                    && tLocationIds.Contains(s.StorageLocationId))
                        .ToListAsync(cancellationToken);
                    var stockForTransferDeleteByKey = stocksForTransferDelete
                        .GroupBy(s => (s.ProductId, s.StorageLocationId))
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var existing in existingTransferMovements)
                    {
                        // Reverse the stock delta that the now-deleted movement had applied.
                        ApplyMovementStockDelta(
                            stockForTransferDeleteByKey, existing.ProductId,
                            existing.MovementType, existing.FromLocationId, existing.ToLocationId,
                            Math.Abs(existing.Quantity), reverse: true);

                        existing.IsDeleted = true;
                        existing.DeletedAt = DateTime.UtcNow;
                        existing.DeletedBy = currentUser ?? "System";
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }

                // Queue the transfer DTOs for creation in Phase 2b
                movementsToCreate.AddRange(transfersToDeleteAndRecreate);

                result.MovementsUpdated += transfersToDeleteAndRecreate.Count;
                foreach (var t in transfersToDeleteAndRecreate)
                {
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = t.DocHeaderId,
                        DocumentNumber = t.DocNumber,
                        DocumentRowId = t.DocRowId,
                        ProductId = t.ProductId,
                        ProductName = t.ProductName,
                        Quantity = t.Quantity,
                        UnitCost = t.Dto.UnitCost,
                        Status = "Updated",
                        MovementType = "Transfer"
                    });
                }

                logger.LogInformation(
                    "RebuildMissingMovements Phase 2a-pre: deleted {Deleted} existing transfer movement(s) for {Rows} row(s); new movements queued for Phase 2b.",
                    existingTransferMovements.Count, transfersToDeleteAndRecreate.Count);
            }
            else
            {
                // DryRun: report what would be updated (delete+recreate) without any DB writes
                result.MovementsUpdated += transfersToDeleteAndRecreate.Count;
                foreach (var t in transfersToDeleteAndRecreate)
                {
                    result.Items.Add(new RebuildMovementsRowResultDto
                    {
                        DocumentHeaderId = t.DocHeaderId,
                        DocumentNumber = t.DocNumber,
                        DocumentRowId = t.DocRowId,
                        ProductId = t.ProductId,
                        ProductName = t.ProductName,
                        Quantity = t.Quantity,
                        UnitCost = t.Dto.UnitCost,
                        Status = "Updated",
                        MovementType = "Transfer"
                    });
                }
            }
        }

        // Phase 2a: Update existing movements (UpdateExisting = true)
        if (movementsToUpdate.Count > 0)
        {
            if (!request.DryRun)
            {
                // Batch-load existing StockMovement entities by DocumentRowId
                var rowIdsToUpdate = movementsToUpdate.Select(m => m.DocRowId).ToHashSet();
                var existingByRowId = await context.StockMovements
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

                var stocksForUpdate = await context.Stocks
                    .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted
                                && updateProductIds.Contains(s.ProductId)
                                && updateLocationIds.Contains(s.StorageLocationId))
                    .ToListAsync(cancellationToken);
                // Use GroupBy instead of ToDictionary to safely handle duplicate (ProductId, StorageLocationId)
                // pairs that may exist in the DB from previous buggy runs (no unique constraint on Stocks).
                var stockForUpdateByKey = stocksForUpdate
                    .GroupBy(s => (s.ProductId, s.StorageLocationId))
                    .ToDictionary(g => g.Key, g => g.First());

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
                                // Log a warning if either side of the delta cannot find its Stock row.
                                // Phase 3 (RecalculateStockForAffectedPairsAsync) will create the
                                // missing row from movement history, but the warning helps diagnose
                                // data-integrity issues (e.g. Stock table was reset externally).
                                LogMissingStockWarning(stockForUpdateByKey, existing.ProductId, oldType, oldFrom, oldTo, docNumber);
                                LogMissingStockWarning(stockForUpdateByKey, existing.ProductId, newType, dto.FromLocationId, dto.ToLocationId, docNumber);

                                ApplyMovementStockDelta(stockForUpdateByKey, existing.ProductId, oldType, oldFrom, oldTo, oldQty, reverse: true);
                                ApplyMovementStockDelta(stockForUpdateByKey, existing.ProductId, newType, dto.FromLocationId, dto.ToLocationId, dto.Quantity, reverse: false);
                                existing.Quantity = dto.Quantity;
                                existing.MovementType = newType;
                                existing.FromLocationId = dto.FromLocationId;
                                existing.ToLocationId = dto.ToLocationId;
                            }

                            existing.MovementDate = dto.MovementDate;
                            existing.Notes = dto.Notes;

                            // Audit UnitCost change when the value differs
                            var oldUnitCost = existing.UnitCost;
                            if (oldUnitCost != dto.UnitCost)
                            {
                                _ = auditLogService.LogEntityChangeAsync(
                                    entityName: "StockMovement",
                                    entityId: existing.Id,
                                    propertyName: "UnitCost",
                                    operationType: "RebuildUpdateUnitCost",
                                    oldValue: oldUnitCost?.ToString("G") ?? "(null)",
                                    newValue: dto.UnitCost?.ToString("G") ?? "(null)",
                                    changedBy: currentUser ?? "System",
                                    entityDisplayName: $"Doc={docNumber} ProductId={productId}",
                                    cancellationToken: cancellationToken);
                            }

                            existing.UnitCost = dto.UnitCost;
                            existing.ModifiedAt = DateTime.UtcNow;
                            existing.ModifiedBy = currentUser ?? "System";
                            updatedCount++;
                        }
                    }

                    // Single SaveChangesAsync after all entity mutations — reduces DB round-trips
                    _ = await context.SaveChangesAsync(cancellationToken);

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
                                UnitCost = m.Dto.UnitCost,
                                Status = "Updated",
                                MovementType = m.IsInbound ? "Inbound" : "Outbound"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in batch movement update during rebuild");
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
                                UnitCost = m.Dto.UnitCost,
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
                        UnitCost = m.Dto.UnitCost,
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
                // Sanity check: every DTO must carry a DocumentRowId so that future reconciliation
                // can distinguish document-linked movements from manual adjustments.  A missing ID
                // would cause double-counting in CalculateReconciledStockAsync.
                var orphanDtos = movementsToCreate.Where(m => !m.Dto.DocumentRowId.HasValue).ToList();
                if (orphanDtos.Count > 0)
                {
                    logger.LogError(
                        "RebuildMissingMovements: {Count} movement DTO(s) have no DocumentRowId set — they will be skipped to prevent double-counting. DocNumbers: {Docs}",
                        orphanDtos.Count,
                        string.Join(", ", orphanDtos.Select(m => m.DocNumber ?? "?")));

                    foreach (var orphan in orphanDtos)
                    {
                        result.Errors++;
                        result.Items.Add(new RebuildMovementsRowResultDto
                        {
                            DocumentHeaderId = orphan.DocHeaderId,
                            DocumentNumber = orphan.DocNumber,
                            DocumentRowId = orphan.DocRowId,
                            ProductId = orphan.ProductId,
                            ProductName = orphan.ProductName,
                            Quantity = orphan.Quantity,
                            UnitCost = orphan.Dto.UnitCost,
                            Status = "Error",
                            ErrorMessage = "DocumentRowId not set — movement skipped to prevent double-counting.",
                            MovementType = orphan.IsInbound ? "Inbound" : "Outbound"
                        });
                    }

                    movementsToCreate = movementsToCreate.Where(m => m.Dto.DocumentRowId.HasValue).ToList();
                }

                // Chunked batch creation: break the full list into configurable chunks so that very
                // large rebuilds do not hold a single giant DB transaction open.
                // The chunk size from the request is clamped to [10, 500] to prevent abuse.
                var effectiveChunkSize = Math.Clamp(request.BatchChunkSize, 10, 500);

                foreach (var createChunk in movementsToCreate.Chunk(effectiveChunkSize))
                {
                    try
                    {
                        await stockMovementService.CreateMovementsBatchAsync(
                            createChunk.Select(m => m.Dto),
                            currentUser!,
                            cancellationToken);

                        result.MovementsCreated += createChunk.Length;
                        foreach (var m in createChunk)
                        {
                            result.Items.Add(new RebuildMovementsRowResultDto
                            {
                                DocumentHeaderId = m.DocHeaderId,
                                DocumentNumber = m.DocNumber,
                                DocumentRowId = m.DocRowId,
                                ProductId = m.ProductId,
                                ProductName = m.ProductName,
                                Quantity = m.Quantity,
                                UnitCost = m.Dto.UnitCost,
                                Status = "Created",
                                MovementType = m.Dto.MovementType ?? (m.IsInbound ? "Inbound" : "Outbound")
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in batch movement creation during rebuild (chunk of {Size})", createChunk.Length);
                        result.Errors += createChunk.Length;
                        foreach (var m in createChunk)
                        {
                            result.Items.Add(new RebuildMovementsRowResultDto
                            {
                                DocumentHeaderId = m.DocHeaderId,
                                DocumentNumber = m.DocNumber,
                                DocumentRowId = m.DocRowId,
                                ProductId = m.ProductId,
                                ProductName = m.ProductName,
                                Quantity = m.Quantity,
                                UnitCost = m.Dto.UnitCost,
                                Status = "Error",
                                ErrorMessage = ex.Message,
                                MovementType = m.Dto.MovementType ?? (m.IsInbound ? "Inbound" : "Outbound")
                            });
                        }
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
                        UnitCost = m.Dto.UnitCost,
                        Status = "Created",
                        MovementType = m.Dto.MovementType ?? (m.IsInbound ? "Inbound" : "Outbound")
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
        //  c) ForceRecalculateFromMovements=true: overwrite existing Stock.Quantity with the
        //     net from the full movement history to correct balances that were already wrong
        //     before this rebuild.
        if (!request.DryRun)
        {
            var affectedPairs = new HashSet<(Guid ProductId, Guid LocationId)>();
            // movementsToCreate at this point already includes transfer DTOs queued by Phase 2a-pre.
            foreach (var m in movementsToCreate.Concat(movementsToUpdate))
            {
                if (m.Dto.ToLocationId.HasValue)
                    affectedPairs.Add((m.Dto.ProductId, m.Dto.ToLocationId.Value));
                if (m.Dto.FromLocationId.HasValue)
                    affectedPairs.Add((m.Dto.ProductId, m.Dto.FromLocationId.Value));
            }
            if (affectedPairs.Count > 0)
            {
                result.StocksForceRecalculated = await RecalculateStockForAffectedPairsAsync(
                    affectedPairs,
                    currentTenantId.Value,
                    currentUser,
                    request.ForceRecalculateFromMovements,
                    request.StartingQuantity,
                    cancellationToken);
                logger.LogInformation(
                    "RebuildMissingMovements Phase 3: recalculated stock for {Count} product/location pair(s). ForceRecalculate={Force}, StocksOverwritten={Overwritten}",
                    affectedPairs.Count, request.ForceRecalculateFromMovements, result.StocksForceRecalculated);
            }
        }
        else if (request.ForceRecalculateFromMovements)
        {
            // DryRun + ForceRecalculate: compute how many stock rows WOULD be overwritten.
            // Includes transfer pairs from Phase 2a-pre (no DB writes performed above in DryRun).
            var dryRunPairs = new HashSet<(Guid ProductId, Guid LocationId)>();
            foreach (var m in movementsToCreate.Concat(movementsToUpdate).Concat(transfersToDeleteAndRecreate))
            {
                if (m.Dto.ToLocationId.HasValue)
                    dryRunPairs.Add((m.Dto.ProductId, m.Dto.ToLocationId.Value));
                if (m.Dto.FromLocationId.HasValue)
                    dryRunPairs.Add((m.Dto.ProductId, m.Dto.FromLocationId.Value));
            }
            if (dryRunPairs.Count > 0)
            {
                result.StocksForceRecalculated = await CountStocksToRecalculateAsync(
                    dryRunPairs, currentTenantId.Value, request.StartingQuantity, cancellationToken);
            }
        }

        logger.LogInformation(
            "RebuildMissingMovements: scanned {docs} documents, {rows} rows. Created: {created}, Updated: {updated}, AlreadyExists: {exists}, SkippedNoLocation: {skipped}, Errors: {errors}, StocksForceRecalculated: {forceRecalc}. DryRun={dryRun}",
            result.DocumentsScanned, result.RowsScanned, result.MovementsCreated, result.MovementsUpdated,
            result.RowsAlreadyHadMovement, result.RowsSkippedNoLocation, result.Errors, result.StocksForceRecalculated, result.IsDryRun);

        return result;
    }

    /// <summary>
    /// Maps the document type to an appropriate <see cref="StockMovementReason"/>.
    /// <para>
    /// Prefers <see cref="Data.Entities.Documents.DocumentType.DefaultMovementReason"/> when set.
    /// Falls back to the legacy heuristic: Adjustment for inventory documents, Purchase for all
    /// other inbound documents, Sale for outbound.  A more granular mapping (e.g. Return, Transfer)
    /// requires setting <c>DefaultMovementReason</c> on the <see cref="DocumentType"/>.
    /// </para>
    /// </summary>
    private static string ResolveMovementReason(Data.Entities.Documents.DocumentType documentType, bool isInbound)
    {
        if (documentType.DefaultMovementReason.HasValue)
            return documentType.DefaultMovementReason.Value.ToString();

        if (documentType.IsInventoryDocument)
            return StockMovementReason.Adjustment.ToString();

        return isInbound
            ? StockMovementReason.Purchase.ToString()
            : StockMovementReason.Sale.ToString();
    }

    /// <summary>
    /// Emits a warning log when the stock dictionary does not contain the expected key for
    /// a movement side (inbound destination or outbound source).  This indicates that the
    /// Stock table is missing a record for the product/location pair and Phase 3 will need
    /// to create it.
    /// </summary>
    private void LogMissingStockWarning(
        Dictionary<(Guid ProductId, Guid LocationId), Stock> stockByKey,
        Guid productId,
        StockMovementType movementType,
        Guid? fromLocationId,
        Guid? toLocationId,
        string? documentNumber)
    {
        if ((movementType == StockMovementType.Inbound || movementType == StockMovementType.Transfer)
            && toLocationId.HasValue
            && !stockByKey.ContainsKey((productId, toLocationId.Value)))
        {
            logger.LogWarning(
                "RebuildMovements: no Stock record found for product {ProductId} at ToLocation {LocationId} (doc {DocNumber}). " +
                "Phase 3 will create the missing row from movement history.",
                productId, toLocationId.Value, documentNumber);
        }

        if ((movementType == StockMovementType.Outbound || movementType == StockMovementType.Transfer)
            && fromLocationId.HasValue
            && !stockByKey.ContainsKey((productId, fromLocationId.Value)))
        {
            logger.LogWarning(
                "RebuildMovements: no Stock record found for product {ProductId} at FromLocation {LocationId} (doc {DocNumber}). " +
                "Phase 3 will create the missing row from movement history.",
                productId, fromLocationId.Value, documentNumber);
        }
    }

    /// <summary>
    /// Applies or reverses the stock-level impact of a movement on the pre-loaded stock dictionary.
    /// Used when updating existing movements to keep stock quantities consistent.
    /// <para>
    /// NOTE: if the (ProductId, LocationId) key is absent from <paramref name="stockByKey"/>
    /// (e.g. the Stock table was externally reset or the record was never created), the delta
    /// for that side is silently skipped here.  Phase 3 (<see cref="RecalculateStockForAffectedPairsAsync"/>)
    /// creates the missing Stock row from movement history for such pairs.  A warning is logged at
    /// the call site to aid diagnostics.
    /// </para>
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
    /// Phase 3 post-rebuild stock cleanup:
    ///  - Deduplicates Stock rows created in parallel by Phase 2 (soft-deletes extras).
    ///  - Refreshes <see cref="Stock.LastMovementDate"/> on the canonical row so the UI
    ///    shows an up-to-date timestamp.
    ///  - For product/location pairs where Phase 2 could not find any existing Stock row
    ///    (e.g. the Stock table was externally reset), computes the movement-based net and
    ///    creates a new record (uncapped — negative balances are legitimate when document
    ///    history starts mid-lifecycle).
    ///  - When <paramref name="forceRecalculateFromMovements"/> is <c>true</c>, also
    ///    overwrites the quantity of <em>existing</em> Stock rows with the net computed
    ///    from the full movement history (inbound − outbound) plus <paramref name="startingQuantity"/>.
    ///    <see cref="Math.Abs(decimal)"/> is applied to each movement quantity to handle legacy rows that were
    ///    incorrectly persisted with a negative value (e.g. old TransferOrderService shipment movements).
    ///    Use this flag when the Stock balances were already wrong before the rebuild.
    /// NOTE: When <paramref name="forceRecalculateFromMovements"/> is <c>false</c>, the
    /// quantity of an <em>existing</em> Stock row is intentionally NOT overridden here —
    /// Phase 2 already applied the correct delta.
    /// </summary>
    /// <returns>
    /// The number of existing Stock rows whose quantity was overwritten
    /// (non-zero only when <paramref name="forceRecalculateFromMovements"/> is <c>true</c>).
    /// </returns>
    private async Task<int> RecalculateStockForAffectedPairsAsync(
        HashSet<(Guid ProductId, Guid LocationId)> pairs,
        Guid tenantId,
        string? currentUser,
        bool forceRecalculateFromMovements,
        decimal startingQuantity,
        CancellationToken cancellationToken)
    {
        var productIds = pairs.Select(p => p.ProductId).ToHashSet();
        var locationIds = pairs.Select(p => p.LocationId).ToHashSet();

        // Load all active Stock records for the affected pairs.
        var existingStocks = await context.Stocks
            .Where(s => s.TenantId == tenantId
                        && productIds.Contains(s.ProductId)
                        && locationIds.Contains(s.StorageLocationId))
            .ToListAsync(cancellationToken);

        var stocksByKey = existingStocks
            .GroupBy(s => (s.ProductId, s.StorageLocationId))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Pairs that have no Stock record at all — Phase 2 could not update what didn't exist.
        var missingPairs = pairs.Where(p => !stocksByKey.ContainsKey(p)).ToHashSet();

        // Determine which pairs need a movement-net calculation:
        //  - always needed for missingPairs (to create the Stock row)
        //  - also needed for existing pairs when forceRecalculateFromMovements = true
        var pairsNeedingNet = forceRecalculateFromMovements
            ? pairs
            : missingPairs.AsEnumerable();

        var netByPair = new Dictionary<(Guid, Guid), decimal>();
        if (pairsNeedingNet.Any())
        {
            var netProductIds = pairsNeedingNet.Select(p => p.ProductId).ToHashSet();
            var netLocationIds = pairsNeedingNet.Select(p => p.LocationId).ToHashSet();

            // Use Math.Abs(sm.Quantity) defensively: legacy movements from TransferOrderService
            // were persisted with a negative quantity even though the convention is always-positive.
            // Math.Abs ensures those rows are counted correctly regardless of sign.
            var inboundTotals = await context.StockMovements
                .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                             && sm.ToLocationId.HasValue
                             && netProductIds.Contains(sm.ProductId)
                             && netLocationIds.Contains(sm.ToLocationId.Value))
                .GroupBy(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.LocationId,
                    // Math.Abs: legacy TransferOrderService rows may have been stored with Quantity < 0
                    Total = g.Sum(sm => Math.Abs(sm.Quantity))
                })
                .ToListAsync(cancellationToken);

            var outboundTotals = await context.StockMovements
                .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                             && sm.FromLocationId.HasValue
                             && netProductIds.Contains(sm.ProductId)
                             && netLocationIds.Contains(sm.FromLocationId.Value))
                .GroupBy(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.LocationId,
                    // Math.Abs: same defensive handling as inboundTotals above
                    Total = g.Sum(sm => Math.Abs(sm.Quantity))
                })
                .ToListAsync(cancellationToken);

            foreach (var item in inboundTotals)
                netByPair[(item.ProductId, item.LocationId)] = item.Total;
            foreach (var item in outboundTotals)
            {
                var key = (item.ProductId, item.LocationId);
                netByPair[key] = netByPair.GetValueOrDefault(key, 0m) - item.Total;
            }
        }

        int overwrittenCount = 0;

        foreach (var pair in pairs)
        {
            if (stocksByKey.TryGetValue(pair, out var stocks))
            {
                var primary = stocks[0];

                if (forceRecalculateFromMovements)
                {
                    var netQty = netByPair.GetValueOrDefault(pair, 0m) + startingQuantity;
                    var oldQty = primary.Quantity;
                    if (oldQty != netQty)
                    {
                        primary.Quantity = netQty;
                        overwrittenCount++;
                        logger.LogInformation(
                            "Phase3 ForceRecalculate: Stock product={ProductId} location={LocationId}: {OldQty} → {NewQty} (startingQty={StartingQty})",
                            pair.ProductId, pair.LocationId, oldQty, netQty, startingQuantity);

                        _ = auditLogService.LogEntityChangeAsync(
                            entityName: "Stock",
                            entityId: primary.Id,
                            propertyName: "Quantity",
                            operationType: "RebuildForceRecalculate",
                            oldValue: oldQty.ToString(),
                            newValue: netQty.ToString(),
                            changedBy: currentUser ?? "System",
                            entityDisplayName: $"ProductId={pair.ProductId} LocationId={pair.LocationId}",
                            cancellationToken: cancellationToken);
                    }
                }

                // Always refresh the movement timestamp and soft-delete any duplicate rows.
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
            else
            {
                // No stock record exists — create from movement net (plus any starting balance).
                var netQty = netByPair.GetValueOrDefault(pair, 0m) + startingQuantity;
                context.Stocks.Add(new Stock
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

        await context.SaveChangesAsync(cancellationToken);
        return overwrittenCount;
    }

    /// <summary>
    /// Computes the effective net unit price from a document row, applying the line
    /// discount (if any) to the base unit price.
    /// <para>
    /// In purchase documents the operator enters a net supplier price and optional
    /// chained trade discounts (e.g. "10+5"). <see cref="DocumentRow.LineDiscount"/>
    /// already holds the cascaded equivalent percentage, so the true cost per unit is
    /// <c>UnitPrice × (1 − LineDiscount / 100)</c>. This is the value that must be
    /// recorded as <see cref="StockMovement.UnitCost"/> to keep price history accurate.
    /// </para>
    /// </summary>
    private static decimal ComputeNetUnitPrice(DocumentRow row)
    {
        if (row.LineDiscount <= 0m)
            return row.UnitPrice;

        var netPrice = row.UnitPrice * (1m - row.LineDiscount / 100m);
        return Math.Round(netPrice, 6, MidpointRounding.AwayFromZero);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Negative movements report + fix
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<NegativeMovementsReportDto> GetNegativeMovementsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var negatives = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.DocumentHeader)
            .Include(sm => sm.ToLocation)
                .ThenInclude(l => l != null ? l.Warehouse : null)
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted && sm.Quantity < 0)
            .OrderBy(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return new NegativeMovementsReportDto
        {
            TotalNegative = negatives.Count,
            Items = negatives.Select(sm => new NegativeMovementItemDto
            {
                Id = sm.Id,
                ProductId = sm.ProductId,
                ProductName = sm.Product?.Name,
                WarehouseName = sm.ToLocation?.Warehouse?.Name ?? sm.ToLocation?.WarehouseId.ToString(),
                Quantity = sm.Quantity,
                MovementType = sm.MovementType.ToString(),
                Reason = sm.Reason.ToString(),
                MovementDate = sm.MovementDate,
                DocumentNumber = sm.DocumentHeader?.Number,
                Notes = sm.Notes
            }).ToList()
        };
    }

    public async Task<FixNegativeMovementsResultDto> FixNegativeMovementsAsync(
        bool dryRun,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var result = new FixNegativeMovementsResultDto { IsDryRun = dryRun };

        var negatives = await context.StockMovements
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted && sm.Quantity < 0)
            .ToListAsync(cancellationToken);

        if (negatives.Count == 0)
            return result;

        if (dryRun)
        {
            result.MovementsFixed = negatives.Count;
            return result;
        }

        // Gather affected (product, location) pairs so we can correct stock levels.
        var affectedPairs = new HashSet<(Guid ProductId, Guid LocationId)>();
        foreach (var sm in negatives)
        {
            if (sm.ToLocationId.HasValue) affectedPairs.Add((sm.ProductId, sm.ToLocationId.Value));
            if (sm.FromLocationId.HasValue) affectedPairs.Add((sm.ProductId, sm.FromLocationId.Value));
        }

        // Pre-load stock records for affected pairs so we can apply the sign correction.
        var productIds = affectedPairs.Select(p => p.ProductId).ToHashSet();
        var locationIds = affectedPairs.Select(p => p.LocationId).ToHashSet();
        var stocks = await context.Stocks
            .Where(s => s.TenantId == tenantId && !s.IsDeleted
                        && productIds.Contains(s.ProductId)
                        && locationIds.Contains(s.StorageLocationId))
            .ToListAsync(cancellationToken);
        var stockByKey = stocks
            .GroupBy(s => (s.ProductId, s.StorageLocationId))
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var sm in negatives)
        {
            try
            {
                var absQty = Math.Abs(sm.Quantity);
                // The movement was stored with a negative quantity, so it was incorrectly
                // subtracting from the balance it should have been adding (or vice-versa).
                // Correct the Stock by applying the delta twice (undo the wrong sign + apply correct).
                var correction = 2m * absQty;  // = |qty| - (−|qty|) = 2|qty|

                var stocksModified = 0;
                if (sm.ToLocationId.HasValue && stockByKey.TryGetValue((sm.ProductId, sm.ToLocationId.Value), out var toStock))
                {
                    toStock.Quantity += correction;
                    toStock.ModifiedAt = DateTime.UtcNow;
                    toStock.ModifiedBy = currentUser;
                    stocksModified++;
                }
                if (sm.FromLocationId.HasValue && stockByKey.TryGetValue((sm.ProductId, sm.FromLocationId.Value), out var fromStock))
                {
                    fromStock.Quantity -= correction;
                    fromStock.ModifiedAt = DateTime.UtcNow;
                    fromStock.ModifiedBy = currentUser;
                    stocksModified++;
                }

                sm.Quantity = absQty;
                sm.ModifiedAt = DateTime.UtcNow;
                sm.ModifiedBy = currentUser;
                result.MovementsFixed++;
                result.StocksAdjusted += stocksModified;

                _ = auditLogService.LogEntityChangeAsync(
                    entityName: "StockMovement",
                    entityId: sm.Id,
                    propertyName: "Quantity",
                    operationType: "FixNegativeMovement",
                    oldValue: (-absQty).ToString("G"),
                    newValue: absQty.ToString("G"),
                    changedBy: currentUser ?? "System",
                    entityDisplayName: $"ProductId={sm.ProductId}",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing negative movement {MovementId}", sm.Id);
                result.Errors++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "FixNegativeMovements: fixed {Fixed}, stocksAdjusted {StocksAdjusted}, errors {Errors}. DryRun={DryRun}",
            result.MovementsFixed, result.StocksAdjusted, result.Errors, dryRun);

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recalculate all stocks
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DryRun helper: counts how many existing Stock rows in <paramref name="pairs"/> would
    /// have their quantity overwritten if <c>ForceRecalculateFromMovements</c> were executed.
    /// No data is modified.
    /// </summary>
    private async Task<int> CountStocksToRecalculateAsync(
        HashSet<(Guid ProductId, Guid LocationId)> pairs,
        Guid tenantId,
        decimal startingQuantity,
        CancellationToken cancellationToken)
    {
        var productIds = pairs.Select(p => p.ProductId).ToHashSet();
        var locationIds = pairs.Select(p => p.LocationId).ToHashSet();

        var existingStocks = await context.Stocks
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IsDeleted
                        && productIds.Contains(s.ProductId)
                        && locationIds.Contains(s.StorageLocationId))
            .Select(s => new { s.ProductId, s.StorageLocationId, s.Quantity })
            .ToListAsync(cancellationToken);

        var stocksByKey = existingStocks
            .GroupBy(s => (s.ProductId, s.StorageLocationId))
            .ToDictionary(g => g.Key, g => g.First().Quantity);

        var netProductIds = pairs.Select(p => p.ProductId).ToHashSet();
        var netLocationIds = pairs.Select(p => p.LocationId).ToHashSet();

        var inboundTotals = await context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.ToLocationId.HasValue
                         && netProductIds.Contains(sm.ProductId)
                         && netLocationIds.Contains(sm.ToLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => Math.Abs(sm.Quantity)) })
            .ToListAsync(cancellationToken);

        var outboundTotals = await context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.FromLocationId.HasValue
                         && netProductIds.Contains(sm.ProductId)
                         && netLocationIds.Contains(sm.FromLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => Math.Abs(sm.Quantity)) })
            .ToListAsync(cancellationToken);

        var netByPair = new Dictionary<(Guid, Guid), decimal>();
        foreach (var item in inboundTotals)
            netByPair[(item.ProductId, item.LocationId)] = item.Total;
        foreach (var item in outboundTotals)
        {
            var key = (item.ProductId, item.LocationId);
            netByPair[key] = netByPair.GetValueOrDefault(key, 0m) - item.Total;
        }

        var wouldChangeCount = 0;
        foreach (var pair in pairs)
        {
            var netQty = netByPair.GetValueOrDefault(pair, 0m) + startingQuantity;
            if (stocksByKey.TryGetValue(pair, out var existingQty))
            {
                if (existingQty != netQty) wouldChangeCount++;
            }
            else
            {
                // Missing stock row would be created
                wouldChangeCount++;
            }
        }
        return wouldChangeCount;
    }

    /// <inheritdoc/>
    public async Task<RecalculateAllStocksResultDto> RecalculateAllStocksFromMovementsAsync(
        bool dryRun,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var result = new RecalculateAllStocksResultDto { IsDryRun = dryRun };

        // Step 1: find all distinct (ProductId, LocationId) pairs in movement history.
        var inboundPairs = await context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted && sm.ToLocationId.HasValue)
            .Select(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        var outboundPairs = await context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted && sm.FromLocationId.HasValue)
            .Select(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
            .Distinct()
            .ToListAsync(cancellationToken);

        var allPairs = inboundPairs
            .Concat(outboundPairs)
            .Select(p => (p.ProductId, p.LocationId))
            .ToHashSet();

        result.PairsScanned = allPairs.Count;
        if (allPairs.Count == 0)
        {
            logger.LogInformation("RecalculateAllStocksFromMovements: no movement pairs found. DryRun={DryRun}", dryRun);
            return result;
        }

        // Step 2: compute net quantity per pair (Σ inbound − Σ outbound).
        var productIds = allPairs.Select(p => p.ProductId).ToHashSet();
        var locationIds = allPairs.Select(p => p.LocationId).ToHashSet();

        var inboundTotals = await context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.ToLocationId.HasValue
                         && productIds.Contains(sm.ProductId)
                         && locationIds.Contains(sm.ToLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => Math.Abs(sm.Quantity)) })
            .ToListAsync(cancellationToken);

        var outboundTotals = await context.StockMovements
            .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                         && sm.FromLocationId.HasValue
                         && productIds.Contains(sm.ProductId)
                         && locationIds.Contains(sm.FromLocationId.Value))
            .GroupBy(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => Math.Abs(sm.Quantity)) })
            .ToListAsync(cancellationToken);

        var netByPair = new Dictionary<(Guid, Guid), decimal>();
        foreach (var item in inboundTotals)
            netByPair[(item.ProductId, item.LocationId)] = item.Total;
        foreach (var item in outboundTotals)
        {
            var key = (item.ProductId, item.LocationId);
            netByPair[key] = netByPair.GetValueOrDefault(key, 0m) - item.Total;
        }

        // Step 3: load existing Stock records for all pairs.
        var existingStocks = await context.Stocks
            .Where(s => s.TenantId == tenantId && !s.IsDeleted
                        && productIds.Contains(s.ProductId)
                        && locationIds.Contains(s.StorageLocationId))
            .ToListAsync(cancellationToken);

        var stocksByKey = existingStocks
            .GroupBy(s => (s.ProductId, s.StorageLocationId))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Step 4: apply updates/creates.
        foreach (var pair in allPairs)
        {
            try
            {
                var netQty = netByPair.GetValueOrDefault(pair, 0m);

                if (stocksByKey.TryGetValue(pair, out var stocks))
                {
                    var primary = stocks[0];
                    var oldQty = primary.Quantity;

                    if (!dryRun)
                    {
                        // Soft-delete duplicates
                        for (var i = 1; i < stocks.Count; i++)
                        {
                            stocks[i].IsDeleted = true;
                            stocks[i].DeletedAt = DateTime.UtcNow;
                            stocks[i].DeletedBy = currentUser ?? "System";
                        }

                        primary.LastMovementDate = DateTime.UtcNow;
                        primary.ModifiedAt = DateTime.UtcNow;
                        primary.ModifiedBy = currentUser ?? "System";

                        if (oldQty != netQty)
                        {
                            primary.Quantity = netQty;
                            result.StocksUpdated++;

                            _ = auditLogService.LogEntityChangeAsync(
                                entityName: "Stock",
                                entityId: primary.Id,
                                propertyName: "Quantity",
                                operationType: "RecalculateAllStocks",
                                oldValue: oldQty.ToString(),
                                newValue: netQty.ToString(),
                                changedBy: currentUser ?? "System",
                                entityDisplayName: $"ProductId={pair.ProductId} LocationId={pair.LocationId}",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            result.StocksAlreadyCorrect++;
                        }
                    }
                    else
                    {
                        // DryRun: count what would change
                        if (oldQty != netQty) result.StocksUpdated++;
                        else result.StocksAlreadyCorrect++;
                    }
                }
                else
                {
                    // No existing stock record
                    if (!dryRun)
                    {
                        context.Stocks.Add(new Stock
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
                    result.StocksCreated++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing pair ProductId={ProductId} LocationId={LocationId} in RecalculateAllStocksFromMovements",
                    pair.ProductId, pair.LocationId);
                result.Errors++;
            }
        }

        if (!dryRun)
            await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "RecalculateAllStocksFromMovements: pairs={Pairs}, updated={Updated}, created={Created}, alreadyCorrect={Correct}, errors={Errors}. DryRun={DryRun}",
            result.PairsScanned, result.StocksUpdated, result.StocksCreated, result.StocksAlreadyCorrect, result.Errors, dryRun);

        return result;
    }

}
