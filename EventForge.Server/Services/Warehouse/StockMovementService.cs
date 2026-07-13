using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock movements and transaction history.
/// </summary>
public class StockMovementService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<StockMovementService> logger) : IStockMovementService
{

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? locationId = null,
        string? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var query = context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.Serial)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.TenantId == currentTenantId.Value && !sm.IsDeleted);

        // Apply filters
        if (productId.HasValue)
        {
            query = query.Where(sm => sm.ProductId == productId.Value);
        }

        if (lotId.HasValue)
        {
            query = query.Where(sm => sm.LotId == lotId.Value);
        }

        if (serialId.HasValue)
        {
            query = query.Where(sm => sm.SerialId == serialId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(sm => sm.FromLocationId == locationId.Value || sm.ToLocationId == locationId.Value);
        }

        if (!string.IsNullOrEmpty(movementType) && Enum.TryParse<StockMovementType>(movementType, true, out var type))
        {
            query = query.Where(sm => sm.MovementType == type);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(sm => sm.MovementDate <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var movements = await query
            .OrderByDescending(sm => sm.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var movementDtos = movements.Select(m => m.ToStockMovementDto()).ToList();

        return new PagedResult<StockMovementDto>
        {
            Items = movementDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockMovementDto?> GetMovementByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Current tenant ID is not available.");
        }

        var movement = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.Serial)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .FirstOrDefaultAsync(sm => sm.Id == id && sm.TenantId == currentTenantId.Value && !sm.IsDeleted, cancellationToken);

        return movement?.ToStockMovementDto();
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.ProductId == productId && sm.TenantId == currentTenantId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.LotId == lotId && sm.TenantId == currentTenantId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsBySerialIdAsync(Guid serialId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Serial)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.SerialId == serialId && sm.TenantId == currentTenantId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => (sm.FromLocationId == locationId || sm.ToLocationId == locationId) && sm.TenantId == currentTenantId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.DocumentHeaderId == documentId && sm.TenantId == currentTenantId && !sm.IsDeleted)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> CreateMovementAsync(CreateStockMovementDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            MovementType = Enum.Parse<StockMovementType>(createDto.MovementType, ignoreCase: true),
            ProductId = createDto.ProductId,
            LotId = createDto.LotId,
            SerialId = createDto.SerialId,
            FromLocationId = createDto.FromLocationId,
            ToLocationId = createDto.ToLocationId,
            Quantity = createDto.Quantity,
            UnitCost = createDto.UnitCost,
            MovementDate = createDto.MovementDate,
            DocumentHeaderId = createDto.DocumentHeaderId,
            DocumentRowId = createDto.DocumentRowId,
            Reason = !string.IsNullOrEmpty(createDto.Reason) && Enum.TryParse<StockMovementReason>(createDto.Reason, out var reasonEnum)
                ? reasonEnum
                : StockMovementReason.Other,
            Notes = createDto.Notes,
            UserId = currentUser,
            IsReconciliationAdjustment = createDto.IsReconciliationAdjustment,
            ReconciliationRunId = createDto.ReconciliationRunId,
            Status = MovementStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        _ = context.StockMovements.Add(movement);

        // Update stock levels
        await UpdateStockLevelsForMovementAsync(movement, cancellationToken);

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync("StockMovement", movement.Id, "Created", "Create",
            null, $"Created stock movement: {movement.MovementType}", currentUser);

        // Load navigation properties needed for DTO mapping
        await context.Entry(movement).Reference(m => m.Product).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.Lot).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.Serial).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.FromLocation).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.ToLocation).LoadAsync(cancellationToken);

        return movement.ToStockMovementDto();
    }

    public async Task<IEnumerable<StockMovementDto>> CreateMovementsBatchAsync(
        IEnumerable<CreateStockMovementDto> createDtos,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Process in chunks to keep each SaveChangesAsync batch manageable.
        // The custom SaveChangesAsync generates one EntityChangeLog row per property per entity;
        // large batches produce thousands of audit INSERTs that exceed SQL Server limits.
        const int chunkSize = 25;
        var allMovements = new List<StockMovement>();
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        foreach (var chunk in createDtos.Chunk(chunkSize))
        {
            var movements = new List<StockMovement>();

            foreach (var createDto in chunk)
            {
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId,
                    MovementType = Enum.Parse<StockMovementType>(createDto.MovementType, ignoreCase: true),
                    ProductId = createDto.ProductId,
                    LotId = createDto.LotId,
                    SerialId = createDto.SerialId,
                    FromLocationId = createDto.FromLocationId,
                    ToLocationId = createDto.ToLocationId,
                    Quantity = createDto.Quantity,
                    UnitCost = createDto.UnitCost,
                    MovementDate = createDto.MovementDate,
                    DocumentHeaderId = createDto.DocumentHeaderId,
                    DocumentRowId = createDto.DocumentRowId,
                    Reason = !string.IsNullOrEmpty(createDto.Reason) && Enum.TryParse<StockMovementReason>(createDto.Reason, out var reasonEnum)
                        ? reasonEnum
                        : StockMovementReason.Other,
                    Notes = createDto.Notes,
                    UserId = currentUser,
                    IsReconciliationAdjustment = createDto.IsReconciliationAdjustment,
                    ReconciliationRunId = createDto.ReconciliationRunId,
                    Status = MovementStatus.Completed,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser
                };

                movements.Add(movement);
                await UpdateStockLevelsForMovementAsync(movement, cancellationToken, throwOnMissingSourceStock: false);
            }

            context.StockMovements.AddRange(movements);
            _ = await context.SaveChangesAsync(cancellationToken);
            allMovements.AddRange(movements);
        }

        _ = await auditLogService.LogEntityChangeAsync("StockMovement", Guid.Empty, "BatchCreated", "BatchCreate",
            null, $"Created {allMovements.Count} stock movements", currentUser);

        return allMovements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> ProcessInboundMovementAsync(
        Guid productId,
        Guid toLocationId,
        decimal quantity,
        decimal? unitCost = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        Guid? documentRowId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default)
    {
        var createDto = new CreateStockMovementDto
        {
            MovementType = StockMovementType.Inbound.ToString(),
            ProductId = productId,
            ToLocationId = toLocationId,
            Quantity = quantity,
            UnitCost = unitCost,
            LotId = lotId,
            SerialId = serialId,
            DocumentHeaderId = documentHeaderId,
            DocumentRowId = documentRowId,
            Notes = notes,
            Reason = "Purchase",
            MovementDate = movementDate ?? DateTime.UtcNow
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ProcessOutboundMovementAsync(
        Guid productId,
        Guid fromLocationId,
        decimal quantity,
        decimal? unitCost = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        Guid? documentRowId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default)
    {
        var createDto = new CreateStockMovementDto
        {
            MovementType = StockMovementType.Outbound.ToString(),
            ProductId = productId,
            FromLocationId = fromLocationId,
            Quantity = quantity,
            UnitCost = unitCost,
            LotId = lotId,
            SerialId = serialId,
            DocumentHeaderId = documentHeaderId,
            DocumentRowId = documentRowId,
            Notes = notes,
            Reason = "Sale",
            MovementDate = movementDate ?? DateTime.UtcNow
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ProcessTransferMovementAsync(
        Guid productId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        Guid? lotId = null,
        Guid? serialId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default)
    {
        var createDto = new CreateStockMovementDto
        {
            MovementType = StockMovementType.Transfer.ToString(),
            ProductId = productId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            Quantity = quantity,
            LotId = lotId,
            SerialId = serialId,
            Notes = notes,
            Reason = "Transfer",
            MovementDate = movementDate ?? DateTime.UtcNow
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ProcessAdjustmentMovementAsync(
        Guid productId,
        Guid locationId,
        decimal adjustmentQuantity,
        string reason,
        Guid? lotId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        bool isReconciliationAdjustment = false,
        Guid? reconciliationRunId = null,
        CancellationToken cancellationToken = default)
    {
        var createDto = new CreateStockMovementDto
        {
            MovementType = StockMovementType.Adjustment.ToString(),
            ProductId = productId,
            ToLocationId = adjustmentQuantity > 0 ? locationId : null,
            FromLocationId = adjustmentQuantity < 0 ? locationId : null,
            Quantity = Math.Abs(adjustmentQuantity),
            LotId = lotId,
            Notes = $"{reason} - {notes}",
            Reason = "Adjustment",
            MovementDate = movementDate ?? DateTime.UtcNow,
            IsReconciliationAdjustment = isReconciliationAdjustment,
            ReconciliationRunId = reconciliationRunId
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ReverseMovementAsync(Guid movementId, string reason, string currentUser, CancellationToken cancellationToken = default)
    {
        var originalMovement = await context.StockMovements.FindAsync(new object[] { movementId }, cancellationToken);
        if (originalMovement is null)
        {
            throw new InvalidOperationException($"Stock movement {movementId} not found");
        }

        var reversedMovementType = originalMovement.MovementType switch
        {
            StockMovementType.Inbound => StockMovementType.Outbound,
            StockMovementType.Outbound => StockMovementType.Inbound,
            _ => originalMovement.MovementType // Transfer, Adjustment unchanged
        };

        var reverseDto = new CreateStockMovementDto
        {
            MovementType = reversedMovementType.ToString(),
            ProductId = originalMovement.ProductId,
            FromLocationId = originalMovement.ToLocationId,
            ToLocationId = originalMovement.FromLocationId,
            Quantity = originalMovement.Quantity,
            LotId = originalMovement.LotId,
            SerialId = originalMovement.SerialId,
            Notes = $"Reversal of movement {movementId}: {reason}",
            Reason = "Return"
        };

        return await CreateMovementAsync(reverseDto, currentUser, cancellationToken);
    }

    public async Task<MovementSummaryDto> GetMovementSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? productId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var query = context.StockMovements
            .AsNoTracking()
            .Where(sm => sm.TenantId == currentTenantId
                      && !sm.IsDeleted
                      && sm.MovementDate >= fromDate
                      && sm.MovementDate <= toDate);

        if (productId.HasValue)
        {
            query = query.Where(sm => sm.ProductId == productId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(sm => sm.FromLocationId == locationId.Value || sm.ToLocationId == locationId.Value);
        }

        var movements = await query.ToListAsync(cancellationToken);

        var inboundMovements = movements.Where(m => m.MovementType == StockMovementType.Inbound).ToList();
        var outboundMovements = movements.Where(m => m.MovementType == StockMovementType.Outbound).ToList();

        return new MovementSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            ProductId = productId,
            LocationId = locationId,
            TotalInbound = inboundMovements.Sum(m => m.Quantity),
            TotalOutbound = outboundMovements.Sum(m => m.Quantity),
            InboundTransactionCount = inboundMovements.Count,
            OutboundTransactionCount = outboundMovements.Count,
            TotalInboundValue = inboundMovements.Sum(m => m.TotalValue),
            TotalOutboundValue = outboundMovements.Sum(m => m.TotalValue)
        };
    }

    public async Task<MovementValidationResult> ValidateMovementAsync(CreateStockMovementDto movementDto, CancellationToken cancellationToken = default)
    {
        var result = new MovementValidationResult { IsValid = true };

        // Validate product exists
        var product = await context.Products.FindAsync(new object[] { movementDto.ProductId }, cancellationToken);
        if (product is null)
        {
            result.IsValid = false;
            result.Errors.Add($"Product {movementDto.ProductId} not found");
            return result;
        }

        // Validate locations exist
        if (movementDto.FromLocationId.HasValue)
        {
            var fromLocation = await context.StorageLocations.FindAsync(new object[] { movementDto.FromLocationId.Value }, cancellationToken);
            if (fromLocation is null)
            {
                result.IsValid = false;
                result.Errors.Add($"From location {movementDto.FromLocationId} not found");
            }
        }

        if (movementDto.ToLocationId.HasValue)
        {
            var toLocation = await context.StorageLocations.FindAsync(new object[] { movementDto.ToLocationId.Value }, cancellationToken);
            if (toLocation is null)
            {
                result.IsValid = false;
                result.Errors.Add($"To location {movementDto.ToLocationId} not found");
            }
        }

        // For outbound movements, check if sufficient stock is available
        if (movementDto.MovementType == StockMovementType.Outbound.ToString() && movementDto.FromLocationId.HasValue)
        {
            var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var availableStock = await context.Stocks
                .Where(s => s.TenantId == currentTenantId
                         && s.ProductId == movementDto.ProductId
                         && s.StorageLocationId == movementDto.FromLocationId.Value
                         && (!movementDto.LotId.HasValue || s.LotId == movementDto.LotId.Value))
                .SumAsync(s => s.AvailableQuantity, cancellationToken);

            if (availableStock < movementDto.Quantity)
            {
                result.IsValid = false;
                result.Errors.Add($"Insufficient stock available. Required: {movementDto.Quantity}, Available: {availableStock}");
            }
        }

        return result;
    }

    public async Task<IEnumerable<StockMovementDto>> GetPendingMovementsAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.TenantId == currentTenantId && !sm.IsDeleted && sm.Status == MovementStatus.Planned)
            .OrderBy(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> ExecutePlannedMovementAsync(Guid movementPlanId, string currentUser, CancellationToken cancellationToken = default)
    {
        var movement = await context.StockMovements.FindAsync(new object[] { movementPlanId }, cancellationToken);
        if (movement is null)
        {
            throw new InvalidOperationException($"Movement plan {movementPlanId} not found");
        }

        if (movement.Status != MovementStatus.Planned)
        {
            throw new InvalidOperationException($"Movement {movementPlanId} is not in planned status");
        }

        movement.Status = MovementStatus.Completed;
        movement.MovementDate = DateTime.UtcNow;
        movement.ModifiedAt = DateTime.UtcNow;
        movement.ModifiedBy = currentUser;

        await UpdateStockLevelsForMovementAsync(movement, cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync("StockMovement", movement.Id, "Status", "Execute",
            "Planned", "Completed", currentUser);

        // Load navigation properties needed for DTO mapping
        await context.Entry(movement).Reference(m => m.Product).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.Lot).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.Serial).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.FromLocation).LoadAsync(cancellationToken);
        await context.Entry(movement).Reference(m => m.ToLocation).LoadAsync(cancellationToken);

        return movement.ToStockMovementDto();
    }

    private async Task UpdateStockLevelsForMovementAsync(StockMovement movement, CancellationToken cancellationToken, bool throwOnMissingSourceStock = true)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        // For outbound or transfer from, reduce stock at source location
        if ((movement.MovementType == StockMovementType.Outbound || movement.MovementType == StockMovementType.Transfer)
            && movement.FromLocationId.HasValue)
        {
            // Check the local change-tracker first so that batch operations within the same
            // SaveChanges cycle see entities that were added or modified but not yet flushed.
            var stock = context.Stocks.Local
                            .FirstOrDefault(s => !s.IsDeleted
                                              && s.TenantId == currentTenantId
                                              && s.ProductId == movement.ProductId
                                              && s.StorageLocationId == movement.FromLocationId.Value
                                              && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                        ?? await context.Stocks
                            .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                   && s.ProductId == movement.ProductId
                                                   && s.StorageLocationId == movement.FromLocationId.Value
                                                   && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                                 cancellationToken);

            if (stock is null)
            {
                logger.LogWarning(
                    "No stock record found for product {ProductId} at location {LocationId} during {MovementType} movement. Stock update skipped.",
                    movement.ProductId, movement.FromLocationId.Value, movement.MovementType);
                if (throwOnMissingSourceStock)
                    throw new InvalidOperationException(
                        $"No stock record found for product {movement.ProductId} at location {movement.FromLocationId.Value}. Cannot process {movement.MovementType} movement.");
                return;
            }

            stock.Quantity -= movement.Quantity;
            stock.LastMovementDate = DateTime.UtcNow;
            stock.ModifiedAt = DateTime.UtcNow;
        }

        // For inbound or transfer to, increase stock at destination location
        if ((movement.MovementType == StockMovementType.Inbound || movement.MovementType == StockMovementType.Transfer)
            && movement.ToLocationId.HasValue)
        {
            var stock = context.Stocks.Local
                            .FirstOrDefault(s => !s.IsDeleted
                                              && s.TenantId == currentTenantId
                                              && s.ProductId == movement.ProductId
                                              && s.StorageLocationId == movement.ToLocationId.Value
                                              && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                        ?? await context.Stocks
                            .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                   && s.ProductId == movement.ProductId
                                                   && s.StorageLocationId == movement.ToLocationId.Value
                                                   && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                                 cancellationToken);

            if (stock is null)
            {
                // Create new stock entry
                stock = new Stock
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId,
                    ProductId = movement.ProductId,
                    StorageLocationId = movement.ToLocationId.Value,
                    LotId = movement.LotId,
                    Quantity = movement.Quantity,
                    ReservedQuantity = 0,
                    LastMovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = movement.UserId ?? "System"
                };
                _ = context.Stocks.Add(stock);
            }
            else
            {
                stock.Quantity += movement.Quantity;
                stock.LastMovementDate = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
            }
        }

        // For adjustment movements
        if (movement.MovementType == StockMovementType.Adjustment)
        {
            var locationId = movement.ToLocationId ?? movement.FromLocationId;
            if (locationId.HasValue)
            {
                var stock = context.Stocks.Local
                                .FirstOrDefault(s => !s.IsDeleted
                                                  && s.TenantId == currentTenantId
                                                  && s.ProductId == movement.ProductId
                                                  && s.StorageLocationId == locationId.Value
                                                  && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                            ?? await context.Stocks
                                .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                       && s.ProductId == movement.ProductId
                                                       && s.StorageLocationId == locationId.Value
                                                       && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                                     cancellationToken);

                if (stock is null)
                {
                    if (movement.ToLocationId.HasValue) // positive adjustment: create new stock entry
                    {
                        stock = new Stock
                        {
                            Id = Guid.NewGuid(),
                            TenantId = currentTenantId,
                            ProductId = movement.ProductId,
                            StorageLocationId = movement.ToLocationId.Value,
                            LotId = movement.LotId,
                            Quantity = movement.Quantity,
                            ReservedQuantity = 0,
                            LastMovementDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = movement.UserId ?? "System"
                        };
                        _ = context.Stocks.Add(stock);
                    }
                }
                else
                {
                    var adjustmentQuantity = movement.ToLocationId.HasValue ? movement.Quantity : -movement.Quantity;
                    stock.Quantity += adjustmentQuantity;
                    stock.LastMovementDate = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;
                }
            }
        }
    }

    #region Export Operations

    public async Task<IEnumerable<Prym.DTOs.Export.InventoryExportDto>> GetInventoryForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for inventory operations.");
        }

        var query = context.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
                .ThenInclude(p => p!.UnitOfMeasure)
            .Include(sm => sm.FromLocation)
                .ThenInclude(loc => loc!.Warehouse)
            .Include(sm => sm.ToLocation)
                .ThenInclude(loc => loc!.Warehouse)
            .Where(sm => !sm.IsDeleted && sm.TenantId == currentTenantId.Value)
            .OrderBy(sm => sm.MovementDate);

        var totalCount = await query.CountAsync(ct);


        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetInventoryInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(MapToInventoryExportDto);
    }

    private async Task<IEnumerable<Prym.DTOs.Export.InventoryExportDto>> GetInventoryInBatchesAsync(
        IQueryable<StockMovement> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.InventoryExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(MapToInventoryExportDto));

            skip += batchSize;

        }

        return results;
    }

    private static Prym.DTOs.Export.InventoryExportDto MapToInventoryExportDto(StockMovement sm)
    {
        var warehouse = sm.ToLocation?.Warehouse ?? sm.FromLocation?.Warehouse;
        var location = sm.ToLocation ?? sm.FromLocation;

        return new Prym.DTOs.Export.InventoryExportDto
        {
            Id = sm.Id,
            MovementDate = sm.MovementDate,
            Product = sm.Product?.Name ?? string.Empty,
            Warehouse = warehouse?.Name ?? string.Empty,
            StorageLocation = location?.Code ?? string.Empty,
            MovementType = sm.MovementType.ToString(),
            Quantity = sm.Quantity,
            UnitOfMeasure = sm.Product?.UnitOfMeasure?.Symbol ?? string.Empty,
            DocumentReference = sm.DocumentHeaderId?.ToString(),
            Notes = sm.Notes,
            CreatedAt = sm.CreatedAt
        };
    }

    #endregion

    #region Row Movement Deletion

    /// <inheritdoc/>
    public async Task DeleteMovementsForRowAsync(Guid rowId, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await context.StockMovements
            .Where(sm => sm.DocumentRowId == rowId && !sm.IsDeleted && sm.TenantId == currentTenantId)
            .ToListAsync(cancellationToken);

        if (movements.Count == 0)
            return;

        foreach (var movement in movements)
        {
            // Revert the stock impact of this movement before soft-deleting it so that
            // the Stock table stays in sync with the actual (deleted) set of movements.
            await ReverseStockLevelsForMovementAsync(movement, cancellationToken);

            movement.IsDeleted = true;
            movement.ModifiedAt = DateTime.UtcNow;
            movement.ModifiedBy = currentUser;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "StockMovement",
            rowId,
            "BulkDeleted",
            "Delete",
            null,
            $"Soft-deleted {movements.Count} stock movement(s) for document row {rowId} (live row change) and reversed their stock impact",
            currentUser);

        logger.LogInformation("Soft-deleted {Count} stock movement(s) for document row {RowId} (live row change) and reversed their stock impact.", movements.Count, rowId);
    }

    /// <summary>
    /// Reverses the stock level changes that were applied when the given movement was processed.
    /// Called before soft-deleting a movement so that the <see cref="Stock"/> table remains
    /// consistent with the active set of movements.
    /// </summary>
    private async Task ReverseStockLevelsForMovementAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        var currentTenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        // Inbound/Transfer-to: was +quantity at ToLocation → reverse is -quantity
        if ((movement.MovementType == StockMovementType.Inbound || movement.MovementType == StockMovementType.Transfer)
            && movement.ToLocationId.HasValue)
        {
            var stock = context.Stocks.Local
                            .FirstOrDefault(s => !s.IsDeleted
                                              && s.TenantId == currentTenantId
                                              && s.ProductId == movement.ProductId
                                              && s.StorageLocationId == movement.ToLocationId.Value
                                              && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                        ?? await context.Stocks
                            .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                   && s.ProductId == movement.ProductId
                                                   && s.StorageLocationId == movement.ToLocationId.Value
                                                   && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                               cancellationToken);

            if (stock is not null)
            {
                stock.Quantity -= movement.Quantity;
                stock.LastMovementDate = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                logger.LogWarning(
                    "ReverseStockLevels: no stock record found for product {ProductId} at to-location {LocationId} while reversing {MovementType} movement {MovementId}. Reversal skipped.",
                    movement.ProductId, movement.ToLocationId.Value, movement.MovementType, movement.Id);
            }
        }

        // Outbound/Transfer-from: was -quantity at FromLocation → reverse is +quantity
        if ((movement.MovementType == StockMovementType.Outbound || movement.MovementType == StockMovementType.Transfer)
            && movement.FromLocationId.HasValue)
        {
            var stock = context.Stocks.Local
                            .FirstOrDefault(s => !s.IsDeleted
                                              && s.TenantId == currentTenantId
                                              && s.ProductId == movement.ProductId
                                              && s.StorageLocationId == movement.FromLocationId.Value
                                              && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                        ?? await context.Stocks
                            .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                   && s.ProductId == movement.ProductId
                                                   && s.StorageLocationId == movement.FromLocationId.Value
                                                   && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                               cancellationToken);

            if (stock is not null)
            {
                stock.Quantity += movement.Quantity;
                stock.LastMovementDate = DateTime.UtcNow;
                stock.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                // Stock record is missing (data inconsistency recovery): the original outbound
                // decremented a stock entry that was subsequently deleted or never existed.
                // Recreate the record so that the reversal and any immediately-following outbound
                // movement can complete without further errors.
                logger.LogWarning(
                    "ReverseStockLevels: no stock record found for product {ProductId} at from-location {LocationId} while reversing {MovementType} movement {MovementId}. Creating recovery stock entry.",
                    movement.ProductId, movement.FromLocationId.Value, movement.MovementType, movement.Id);

                var recoveryStock = new Stock
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId,
                    ProductId = movement.ProductId,
                    StorageLocationId = movement.FromLocationId.Value,
                    LotId = movement.LotId,
                    Quantity = movement.Quantity,
                    ReservedQuantity = 0,
                    LastMovementDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = movement.UserId ?? "System"
                };
                _ = context.Stocks.Add(recoveryStock);
            }
        }

        // Adjustment: was +quantity (ToLocationId set) or -quantity (FromLocationId set) → reverse
        if (movement.MovementType == StockMovementType.Adjustment)
        {
            var locationId = movement.ToLocationId ?? movement.FromLocationId;
            if (locationId.HasValue)
            {
                var stock = context.Stocks.Local
                                .FirstOrDefault(s => !s.IsDeleted
                                                  && s.TenantId == currentTenantId
                                                  && s.ProductId == movement.ProductId
                                                  && s.StorageLocationId == locationId.Value
                                                  && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value))
                            ?? await context.Stocks
                                .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                                       && s.ProductId == movement.ProductId
                                                       && s.StorageLocationId == locationId.Value
                                                       && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                                   cancellationToken);

                if (stock is not null)
                {
                    // ToLocationId set → was a positive adjustment (added stock) → subtract to reverse
                    // FromLocationId set → was a negative adjustment (removed stock) → add to reverse
                    var reversalQuantity = movement.ToLocationId.HasValue ? -movement.Quantity : movement.Quantity;
                    stock.Quantity += reversalQuantity;
                    stock.LastMovementDate = DateTime.UtcNow;
                    stock.ModifiedAt = DateTime.UtcNow;
                }
                else
                {
                    logger.LogWarning(
                        "ReverseStockLevels: no stock record found for product {ProductId} at location {LocationId} while reversing Adjustment movement {MovementId}. Reversal skipped.",
                        movement.ProductId, locationId.Value, movement.Id);
                }
            }
        }
    }

    #endregion

}
