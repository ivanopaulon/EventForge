using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing stock movements and transaction history.
/// </summary>
public class StockMovementService : IStockMovementService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<StockMovementService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Lot)
                .Include(sm => sm.Serial)
                .Include(sm => sm.FromLocation)
                .Include(sm => sm.ToLocation)
                .Where(sm => sm.TenantId == currentTenantId.Value);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movements with filters");
            throw;
        }
    }

    public async Task<StockMovementDto?> GetMovementByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var movement = await _context.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Lot)
                .Include(sm => sm.Serial)
                .Include(sm => sm.FromLocation)
                .Include(sm => sm.ToLocation)
                .FirstOrDefaultAsync(sm => sm.Id == id && sm.TenantId == currentTenantId.Value, cancellationToken);

            return movement?.ToStockMovementDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock movement by ID: {MovementId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.ProductId == productId && sm.TenantId == currentTenantId)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.LotId == lotId && sm.TenantId == currentTenantId)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsBySerialIdAsync(Guid serialId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Serial)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.SerialId == serialId && sm.TenantId == currentTenantId)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => (sm.FromLocationId == locationId || sm.ToLocationId == locationId) && sm.TenantId == currentTenantId)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<IEnumerable<StockMovementDto>> GetMovementsByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.DocumentHeaderId == documentId && sm.TenantId == currentTenantId)
            .OrderByDescending(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> CreateMovementAsync(CreateStockMovementDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId,
            MovementType = Enum.Parse<StockMovementType>(createDto.MovementType),
            ProductId = createDto.ProductId,
            LotId = createDto.LotId,
            SerialId = createDto.SerialId,
            FromLocationId = createDto.FromLocationId,
            ToLocationId = createDto.ToLocationId,
            Quantity = createDto.Quantity,
            UnitCost = createDto.UnitCost,
            MovementDate = createDto.MovementDate,
            Reason = !string.IsNullOrEmpty(createDto.Reason) && Enum.TryParse<StockMovementReason>(createDto.Reason, out var reasonEnum)
                ? reasonEnum
                : StockMovementReason.Other,
            Notes = createDto.Notes,
            UserId = currentUser,
            Status = MovementStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        _ = _context.StockMovements.Add(movement);

        // Update stock levels
        await UpdateStockLevelsForMovementAsync(movement, cancellationToken);

        _ = await _context.SaveChangesAsync(cancellationToken);

        _ = await _auditLogService.LogEntityChangeAsync("StockMovement", movement.Id, "Created", "Create",
            null, $"Created stock movement: {movement.MovementType}", currentUser);

        return (await GetMovementByIdAsync(movement.Id, cancellationToken))!;
    }

    public async Task<IEnumerable<StockMovementDto>> CreateMovementsBatchAsync(
        IEnumerable<CreateStockMovementDto> createDtos,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var movements = new List<StockMovement>();
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        foreach (var createDto in createDtos)
        {
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                MovementType = Enum.Parse<StockMovementType>(createDto.MovementType),
                ProductId = createDto.ProductId,
                LotId = createDto.LotId,
                SerialId = createDto.SerialId,
                FromLocationId = createDto.FromLocationId,
                ToLocationId = createDto.ToLocationId,
                Quantity = createDto.Quantity,
                UnitCost = createDto.UnitCost,
                MovementDate = createDto.MovementDate,
                Reason = !string.IsNullOrEmpty(createDto.Reason) && Enum.TryParse<StockMovementReason>(createDto.Reason, out var reasonEnum)
                    ? reasonEnum
                    : StockMovementReason.Other,
                Notes = createDto.Notes,
                UserId = currentUser,
                Status = MovementStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            movements.Add(movement);
            await UpdateStockLevelsForMovementAsync(movement, cancellationToken);
        }

        _context.StockMovements.AddRange(movements);
        _ = await _context.SaveChangesAsync(cancellationToken);

        _ = await _auditLogService.LogEntityChangeAsync("StockMovement", Guid.Empty, "BatchCreated", "BatchCreate",
            null, $"Created {movements.Count} stock movements", currentUser);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> ProcessInboundMovementAsync(
        Guid productId,
        Guid toLocationId,
        decimal quantity,
        decimal? unitCost = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        string? notes = null,
        string? currentUser = null,
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
            Notes = notes,
            Reason = "Purchase"
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ProcessOutboundMovementAsync(
        Guid productId,
        Guid fromLocationId,
        decimal quantity,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        string? notes = null,
        string? currentUser = null,
        CancellationToken cancellationToken = default)
    {
        var createDto = new CreateStockMovementDto
        {
            MovementType = StockMovementType.Outbound.ToString(),
            ProductId = productId,
            FromLocationId = fromLocationId,
            Quantity = quantity,
            LotId = lotId,
            SerialId = serialId,
            DocumentHeaderId = documentHeaderId,
            Notes = notes,
            Reason = "Sale"
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
            Reason = "Transfer"
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
            MovementDate = movementDate ?? DateTime.UtcNow
        };

        return await CreateMovementAsync(createDto, currentUser ?? "System", cancellationToken);
    }

    public async Task<StockMovementDto> ReverseMovementAsync(Guid movementId, string reason, string currentUser, CancellationToken cancellationToken = default)
    {
        var originalMovement = await _context.StockMovements.FindAsync(new object[] { movementId }, cancellationToken);
        if (originalMovement == null)
        {
            throw new InvalidOperationException($"Stock movement {movementId} not found");
        }

        var reverseDto = new CreateStockMovementDto
        {
            MovementType = originalMovement.MovementType.ToString(),
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
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var query = _context.StockMovements
            .Where(sm => sm.TenantId == currentTenantId
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
        var product = await _context.Products.FindAsync(new object[] { movementDto.ProductId }, cancellationToken);
        if (product == null)
        {
            result.IsValid = false;
            result.Errors.Add($"Product {movementDto.ProductId} not found");
            return result;
        }

        // Validate locations exist
        if (movementDto.FromLocationId.HasValue)
        {
            var fromLocation = await _context.StorageLocations.FindAsync(new object[] { movementDto.FromLocationId.Value }, cancellationToken);
            if (fromLocation == null)
            {
                result.IsValid = false;
                result.Errors.Add($"From location {movementDto.FromLocationId} not found");
            }
        }

        if (movementDto.ToLocationId.HasValue)
        {
            var toLocation = await _context.StorageLocations.FindAsync(new object[] { movementDto.ToLocationId.Value }, cancellationToken);
            if (toLocation == null)
            {
                result.IsValid = false;
                result.Errors.Add($"To location {movementDto.ToLocationId} not found");
            }
        }

        // For outbound movements, check if sufficient stock is available
        if (movementDto.MovementType == StockMovementType.Outbound.ToString() && movementDto.FromLocationId.HasValue)
        {
            var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

            var availableStock = await _context.Stocks
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
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        var movements = await _context.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Lot)
            .Include(sm => sm.FromLocation)
            .Include(sm => sm.ToLocation)
            .Where(sm => sm.TenantId == currentTenantId && sm.Status == MovementStatus.Planned)
            .OrderBy(sm => sm.MovementDate)
            .ToListAsync(cancellationToken);

        return movements.Select(m => m.ToStockMovementDto());
    }

    public async Task<StockMovementDto> ExecutePlannedMovementAsync(Guid movementPlanId, string currentUser, CancellationToken cancellationToken = default)
    {
        var movement = await _context.StockMovements.FindAsync(new object[] { movementPlanId }, cancellationToken);
        if (movement == null)
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
        _ = await _context.SaveChangesAsync(cancellationToken);

        _ = await _auditLogService.LogEntityChangeAsync("StockMovement", movement.Id, "Status", "Execute",
            "Planned", "Completed", currentUser);

        return (await GetMovementByIdAsync(movement.Id, cancellationToken))!;
    }

    private async Task UpdateStockLevelsForMovementAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Current tenant ID is not available.");

        // For outbound or transfer from, reduce stock at source location
        if ((movement.MovementType == StockMovementType.Outbound || movement.MovementType == StockMovementType.Transfer)
            && movement.FromLocationId.HasValue)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                       && s.ProductId == movement.ProductId
                                       && s.StorageLocationId == movement.FromLocationId.Value
                                       && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                     cancellationToken);

            if (stock != null)
            {
                stock.Quantity -= movement.Quantity;
                stock.ModifiedAt = DateTime.UtcNow;
            }
        }

        // For inbound or transfer to, increase stock at destination location
        if ((movement.MovementType == StockMovementType.Inbound || movement.MovementType == StockMovementType.Transfer)
            && movement.ToLocationId.HasValue)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                       && s.ProductId == movement.ProductId
                                       && s.StorageLocationId == movement.ToLocationId.Value
                                       && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                     cancellationToken);

            if (stock == null)
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
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = movement.UserId ?? "System"
                };
                _ = _context.Stocks.Add(stock);
            }
            else
            {
                stock.Quantity += movement.Quantity;
                stock.ModifiedAt = DateTime.UtcNow;
            }
        }

        // For adjustment movements
        if (movement.MovementType == StockMovementType.Adjustment)
        {
            var locationId = movement.ToLocationId ?? movement.FromLocationId;
            if (locationId.HasValue)
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.TenantId == currentTenantId
                                           && s.ProductId == movement.ProductId
                                           && s.StorageLocationId == locationId.Value
                                           && (!movement.LotId.HasValue || s.LotId == movement.LotId.Value),
                                         cancellationToken);

                if (stock != null)
                {
                    var adjustmentQuantity = movement.ToLocationId.HasValue ? movement.Quantity : -movement.Quantity;
                    stock.Quantity += adjustmentQuantity;
                    stock.ModifiedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
