using ClosedXML.Excel;
using Prym.DTOs.Warehouse;
using Microsoft.EntityFrameworkCore;

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

    public async Task<StockReconciliationResultDto> CalculateReconciledStockAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Server-side date validation
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            {
                throw new ArgumentException("FromDate cannot be after ToDate.");
            }


            var result = new StockReconciliationResultDto();

            // Get stocks based on filters
            var stocksQuery = context.Stocks
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
                var invQuery = context.DocumentRows
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
                                dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Closed);

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
                var docQuery = context.DocumentRows
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
                                (dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Open ||
                                 dr.DocumentHeader.Status == Prym.DTOs.Common.DocumentStatus.Closed));

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
                var manualMovQuery = context.StockMovements
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

            logger.LogInformation("Reconciliation calculation completed. Total items: {Total}, With discrepancies: {Discrepancies}",
                result.Summary.TotalProducts, result.Summary.TotalProducts - result.Summary.CorrectCount);

            return result;
        }
        catch (Exception ex)
        {
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

                var itemsToUpdate = reconciliation.Items
                    .Where(i => request.ItemsToApply.Contains(i.StockId))
                    .ToList();

                foreach (var item in itemsToUpdate)
                {
                    var stock = await context.Stocks
                        .FirstOrDefaultAsync(s => s.Id == item.StockId && s.TenantId == currentTenantId.Value, cancellationToken);

                    if (stock is null)
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
                    await auditLogService.LogEntityChangeAsync(
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

        // Resolve effective status filters (defaults: Approved, Closed)
        var approvalStatusFilter = (request.ApprovalStatuses is not null && request.ApprovalStatuses.Count > 0)
            ? request.ApprovalStatuses.Select(v => (Data.Entities.Documents.ApprovalStatus)v).ToList()
            : new List<Data.Entities.Documents.ApprovalStatus> { Data.Entities.Documents.ApprovalStatus.Approved };

        var documentStatusFilter = (request.DocumentStatuses is not null && request.DocumentStatuses.Count > 0)
            ? request.DocumentStatuses.Select(v => (Prym.DTOs.Common.DocumentStatus)v).ToList()
            : new List<Prym.DTOs.Common.DocumentStatus> { Prym.DTOs.Common.DocumentStatus.Closed };

        // Build query on DocumentHeaders
        var headersQuery = context.DocumentHeaders
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

        // Batch: fetch first storage location per warehouse
        var storageLocationsByWarehouse = await context.StorageLocations
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
            if (documentHeader.DocumentType is null || documentHeader.Rows is null)
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

                        _ = await context.SaveChangesAsync(cancellationToken);
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
                // Batch create all movements: single SaveChangesAsync + single audit log entry
                try
                {
                    await stockMovementService.CreateMovementsBatchAsync(
                        movementsToCreate.Select(m => m.Dto),
                        currentUser!,
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
                    logger.LogError(ex, "Error in batch movement creation during rebuild");
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
                logger.LogInformation("RebuildMissingMovements Phase 3: recalculated stock for {Count} product/location pair(s).", affectedPairs.Count);
            }
        }

        logger.LogInformation(
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
    /// Phase 3 post-rebuild stock cleanup:
    ///  - Deduplicates Stock rows created in parallel by Phase 2 (soft-deletes extras).
    ///  - Refreshes <see cref="Stock.LastMovementDate"/> on the canonical row so the UI
    ///    shows an up-to-date timestamp.
    ///  - For product/location pairs where Phase 2 could not find any existing Stock row
    ///    (e.g. the Stock table was externally reset), computes the movement-based net and
    ///    creates a new record capped at 0 to avoid persisting negative balances when the
    ///    document history is incomplete.
    /// NOTE: The quantity of any <em>existing</em> Stock row is intentionally NOT overridden
    /// here — Phase 2 already applied the correct delta.  Overriding with an all-time movement
    /// net would erase any initial inventory that was set outside the document system.
    /// </summary>
    private async Task RecalculateStockForAffectedPairsAsync(
        HashSet<(Guid ProductId, Guid LocationId)> pairs,
        Guid tenantId,
        string? currentUser,
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

        // For missing pairs only: compute net from movement history.
        var netForMissingPairs = new Dictionary<(Guid, Guid), decimal>();
        if (missingPairs.Count > 0)
        {
            var missingProductIds = missingPairs.Select(p => p.ProductId).ToHashSet();
            var missingLocationIds = missingPairs.Select(p => p.LocationId).ToHashSet();

            var inboundTotals = await context.StockMovements
                .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                             && sm.ToLocationId.HasValue
                             && missingProductIds.Contains(sm.ProductId)
                             && missingLocationIds.Contains(sm.ToLocationId.Value))
                .GroupBy(sm => new { sm.ProductId, LocationId = sm.ToLocationId!.Value })
                .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => sm.Quantity) })
                .ToListAsync(cancellationToken);

            var outboundTotals = await context.StockMovements
                .Where(sm => sm.TenantId == tenantId && !sm.IsDeleted
                             && sm.FromLocationId.HasValue
                             && missingProductIds.Contains(sm.ProductId)
                             && missingLocationIds.Contains(sm.FromLocationId.Value))
                .GroupBy(sm => new { sm.ProductId, LocationId = sm.FromLocationId!.Value })
                .Select(g => new { g.Key.ProductId, g.Key.LocationId, Total = g.Sum(sm => sm.Quantity) })
                .ToListAsync(cancellationToken);

            foreach (var item in inboundTotals)
                netForMissingPairs[(item.ProductId, item.LocationId)] = item.Total;
            foreach (var item in outboundTotals)
            {
                var key = (item.ProductId, item.LocationId);
                netForMissingPairs[key] = netForMissingPairs.GetValueOrDefault(key, 0m) - item.Total;
            }
        }

        foreach (var pair in pairs)
        {
            if (stocksByKey.TryGetValue(pair, out var stocks))
            {
                // Phase 2 already updated the canonical record's quantity correctly.
                // Only refresh the movement timestamp and soft-delete any duplicate rows.
                var primary = stocks[0];
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
                // No stock record exists — create from movement net.
                // Cap at 0 to avoid negative balances when outbound-only document history is incomplete.
                var netQty = Math.Max(0m, netForMissingPairs.GetValueOrDefault(pair, 0m));
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
    }

}
