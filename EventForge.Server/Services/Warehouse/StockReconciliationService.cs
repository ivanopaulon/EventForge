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

            // Process each stock
            foreach (var stock in stocks)
            {
                var item = await CalculateStockItemAsync(stock, request, currentTenantId.Value, cancellationToken);

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

    private async Task<StockReconciliationItemDto> CalculateStockItemAsync(
        Data.Entities.Warehouse.Stock stock,
        StockReconciliationRequestDto request,
        Guid tenantId,
        CancellationToken cancellationToken)
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

        // 1. Check for inventory movement first (it replaces the starting quantity)
        StockMovementSourceDto? inventoryMovement = null;
        if (request.IncludeInventories)
        {
            inventoryMovement = await GetLastInventoryMovementAsync(
                stock.ProductId,
                stock.StorageLocationId,
                request.FromDate,
                request.ToDate,
                tenantId,
                cancellationToken);

            if (inventoryMovement != null)
            {
                sourceMovements.Add(inventoryMovement);
                // Inventory replaces the calculated quantity and sets new effective starting point
                calculatedQuantity = inventoryMovement.Quantity;
                effectiveFromDate = inventoryMovement.Date;
                item.TotalInventories = 1;
            }
        }

        // 2. Process document movements from effective starting date
        if (request.IncludeDocuments)
        {
            var documentMovements = await GetDocumentMovementsAsync(
                stock.ProductId,
                stock.StorageLocationId,
                effectiveFromDate,
                request.ToDate,
                tenantId,
                cancellationToken);

            foreach (var movement in documentMovements)
            {
                sourceMovements.Add(movement);
                calculatedQuantity += movement.Quantity;
            }

            item.TotalDocuments = documentMovements.Count;
        }

        // 3. Process manual stock movements from effective starting date
        var manualMovements = await GetManualMovementsAsync(
            stock.ProductId,
            stock.StorageLocationId,
            effectiveFromDate,
            request.ToDate,
            tenantId,
            cancellationToken);

        foreach (var movement in manualMovements)
        {
            sourceMovements.Add(movement);
            calculatedQuantity += movement.Quantity;
        }

        item.TotalManualMovements = manualMovements.Count;

        // Set calculated values
        item.CalculatedQuantity = calculatedQuantity;
        item.Difference = calculatedQuantity - stock.Quantity;

        // Fix percentage calculation: use the larger of current or calculated as base
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

    private async Task<List<StockMovementSourceDto>> GetDocumentMovementsAsync(
        Guid productId,
        Guid locationId,
        DateTime? fromDate,
        DateTime? toDate,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var query = _context.DocumentRows
            .AsNoTracking()
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Where(dr => dr.TenantId == tenantId &&
                        !dr.IsDeleted &&
                        dr.ProductId == productId &&
                        dr.LocationId == locationId &&
                        dr.DocumentHeader != null &&
                        dr.DocumentHeader.DocumentType != null);

        if (fromDate.HasValue)
        {
            query = query.Where(dr => dr.DocumentHeader!.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(dr => dr.DocumentHeader!.Date <= toDate.Value);
        }

        var documentRows = await query.ToListAsync(cancellationToken);

        return documentRows.Select(dr => new StockMovementSourceDto
        {
            Type = "Document",
            Reference = $"{dr.DocumentHeader!.DocumentType!.Code}-{dr.DocumentHeader.Number}",
            Quantity = dr.DocumentHeader.DocumentType.IsStockIncrease
                ? dr.Quantity
                : -dr.Quantity,
            Date = dr.DocumentHeader.Date,
            IsReplacement = false
        }).ToList();
    }

    private async Task<StockMovementSourceDto?> GetLastInventoryMovementAsync(
        Guid productId,
        Guid locationId,
        DateTime? fromDate,
        DateTime? toDate,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Query for the last finalized inventory document within the date range
        // Uses IsInventoryDocument property to identify inventory document types
        var query = _context.DocumentRows
            .AsNoTracking()
            .Include(dr => dr.DocumentHeader)
                .ThenInclude(dh => dh!.DocumentType)
            .Where(dr => dr.TenantId == tenantId &&
                        !dr.IsDeleted &&
                        dr.ProductId == productId &&
                        dr.LocationId == locationId &&
                        dr.DocumentHeader != null &&
                        dr.DocumentHeader.DocumentType != null &&
                        dr.DocumentHeader.DocumentType.IsInventoryDocument &&
                        dr.DocumentHeader.Status == EventForge.DTOs.Common.DocumentStatus.Closed);

        // Apply date filters
        if (fromDate.HasValue)
        {
            query = query.Where(dr => dr.DocumentHeader!.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(dr => dr.DocumentHeader!.Date <= toDate.Value);
        }

        var lastInventoryRow = await query
            .OrderByDescending(dr => dr.DocumentHeader!.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastInventoryRow == null)
            return null;

        return new StockMovementSourceDto
        {
            Type = "Inventory",
            Reference = $"{lastInventoryRow.DocumentHeader!.DocumentType!.Code}-{lastInventoryRow.DocumentHeader.Number}",
            Quantity = lastInventoryRow.Quantity, // Counted quantity
            Date = lastInventoryRow.DocumentHeader.Date,
            IsReplacement = true // Indicates this replaces the stock quantity
        };
    }

    private async Task<List<StockMovementSourceDto>> GetManualMovementsAsync(
        Guid productId,
        Guid locationId,
        DateTime? fromDate,
        DateTime? toDate,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == tenantId &&
                        !sm.IsDeleted &&
                        sm.ProductId == productId &&
                        (sm.FromLocationId == locationId || sm.ToLocationId == locationId));

        if (fromDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate <= toDate.Value);
        }

        var movements = await query.ToListAsync(cancellationToken);

        return movements.Select(sm => new StockMovementSourceDto
        {
            Type = "Manual",
            Reference = sm.Reference ?? "Manual Adjustment",
            Quantity = sm.ToLocationId == locationId ? sm.Quantity : -sm.Quantity,
            Date = sm.MovementDate,
            IsReplacement = false
        }).ToList();
    }

    private static ReconciliationSeverity DetermineSeverity(
        decimal currentQuantity,
        decimal calculatedQuantity,
        decimal differencePercentage,
        decimal threshold)
    {
        // Missing: Current is 0 but calculated is > 0
        if (currentQuantity == 0 && calculatedQuantity > 0)
        {
            return ReconciliationSeverity.Missing;
        }

        // Correct: No difference
        if (currentQuantity == calculatedQuantity)
        {
            return ReconciliationSeverity.Correct;
        }

        // Major: Difference > threshold
        if (differencePercentage > threshold)
        {
            return ReconciliationSeverity.Major;
        }

        // Minor: Difference <= threshold
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
                // First, recalculate to get current values
                var recalcRequest = new StockReconciliationRequestDto
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

                    // Update stock quantity
                    stock.Quantity = newQuantity;
                    stock.ModifiedAt = DateTime.UtcNow;
                    stock.ModifiedBy = currentUser;

                    result.UpdatedCount++;
                    result.UpdatedStockIds.Add(stock.Id);
                    result.TotalAdjustmentValue += Math.Abs(adjustment);

                    // Create adjustment movement if requested
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
                    }

                    // Audit log
                    await _auditLogService.LogEntityChangeAsync(
                        entityName: "Stock",
                        entityId: stock.Id,
                        propertyName: "Quantity",
                        operationType: "Reconciliation",
                        oldValue: oldQuantity.ToString(),
                        newValue: newQuantity.ToString(),
                        changedBy: currentUser,
                        entityDisplayName: $"{stock.Product?.Name ?? "Unknown"} @ {stock.StorageLocation?.Code ?? "Unknown"}",
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

    public async Task<byte[]> ExportReconciliationReportAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting reconciliation report");

            // Calculate reconciliation
            var reconciliation = await CalculateReconciledStockAsync(request, cancellationToken);

            // TODO: Implement Excel export using EPPlus or ClosedXML
            // For now, this feature is not implemented and returns empty array
            // This should be enhanced in a future iteration to generate a proper Excel file
            // with Summary, Details, and Movements sheets
            await Task.CompletedTask;

            _logger.LogWarning("Excel export not yet implemented - returning empty array");
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting reconciliation report");
            throw;
        }
    }
}
