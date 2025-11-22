using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing transfer orders.
/// </summary>
public class TransferOrderService : ITransferOrderService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TransferOrderService> _logger;

    public TransferOrderService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<TransferOrderService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<TransferOrderDto>> GetTransferOrdersAsync(
        int page = 1,
        int pageSize = 20,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        string? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.TransferOrders
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.Rows)
                    .ThenInclude(r => r.Product)
                .Where(t => t.TenantId == currentTenantId.Value && !t.IsDeleted);

            // Apply filters
            if (sourceWarehouseId.HasValue)
            {
                query = query.Where(t => t.SourceWarehouseId == sourceWarehouseId.Value);
            }

            if (destinationWarehouseId.HasValue)
            {
                query = query.Where(t => t.DestinationWarehouseId == destinationWarehouseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransferOrderStatus>(status, true, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.Number.Contains(searchTerm) || 
                                        (t.ShippingReference != null && t.ShippingReference.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var transferOrders = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TransferOrderDto>
            {
                Items = transferOrders.Select(t => t.ToTransferOrderDto()),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer orders.");
            throw;
        }
    }

    public async Task<TransferOrderDto?> GetTransferOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var transferOrder = await _context.TransferOrders
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.Rows)
                    .ThenInclude(r => r.Product)
                .Include(t => t.Rows)
                    .ThenInclude(r => r.SourceLocation)
                .Include(t => t.Rows)
                    .ThenInclude(r => r.DestinationLocation)
                .Include(t => t.Rows)
                    .ThenInclude(r => r.Lot)
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == currentTenantId.Value && !t.IsDeleted, cancellationToken);

            return transferOrder?.ToTransferOrderDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer order {TransferOrderId}.", id);
            throw;
        }
    }

    public async Task<TransferOrderDto> CreateTransferOrderAsync(CreateTransferOrderDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Validate warehouses exist and are different
            if (createDto.SourceWarehouseId == createDto.DestinationWarehouseId)
            {
                throw new InvalidOperationException("Source and destination warehouses must be different.");
            }

            var sourceWarehouse = await _context.StorageFacilities
                .FirstOrDefaultAsync(w => w.Id == createDto.SourceWarehouseId && w.TenantId == currentTenantId.Value && !w.IsDeleted, cancellationToken);
            
            if (sourceWarehouse == null)
            {
                throw new InvalidOperationException("Source warehouse not found.");
            }

            var destinationWarehouse = await _context.StorageFacilities
                .FirstOrDefaultAsync(w => w.Id == createDto.DestinationWarehouseId && w.TenantId == currentTenantId.Value && !w.IsDeleted, cancellationToken);
            
            if (destinationWarehouse == null)
            {
                throw new InvalidOperationException("Destination warehouse not found.");
            }

            // Validate rows
            if (createDto.Rows == null || !createDto.Rows.Any())
            {
                throw new InvalidOperationException("Transfer order must have at least one row.");
            }

            // Generate transfer order number if not provided
            var number = createDto.Number;
            if (string.IsNullOrWhiteSpace(number))
            {
                number = await GenerateTransferOrderNumberAsync(createDto.Series, currentTenantId.Value, cancellationToken);
            }

            // Create transfer order
            var transferOrder = createDto.ToEntity(currentTenantId.Value, currentUser, number);
            _context.TransferOrders.Add(transferOrder);

            // Create transfer order rows
            foreach (var rowDto in createDto.Rows)
            {
                // Validate product exists
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == rowDto.ProductId && p.TenantId == currentTenantId.Value && !p.IsDeleted, cancellationToken);
                
                if (product == null)
                {
                    throw new InvalidOperationException($"Product with ID {rowDto.ProductId} not found.");
                }

                // Validate source location exists and belongs to source warehouse
                var sourceLocation = await _context.StorageLocations
                    .FirstOrDefaultAsync(l => l.Id == rowDto.SourceLocationId && l.WarehouseId == createDto.SourceWarehouseId && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);
                
                if (sourceLocation == null)
                {
                    throw new InvalidOperationException($"Source location with ID {rowDto.SourceLocationId} not found in source warehouse.");
                }

                // Create row
                var row = rowDto.ToEntity(currentTenantId.Value, transferOrder.Id, currentUser);
                _context.TransferOrderRows.Add(row);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                "TransferOrder", 
                transferOrder.Id, 
                "Status", 
                "Create", 
                null, 
                "Pending", 
                currentUser, 
                $"Transfer order {number} created from {sourceWarehouse.Name} to {destinationWarehouse.Name}",
                cancellationToken);

            _logger.LogInformation("Transfer order {TransferOrderNumber} created successfully by {User}.", number, currentUser);

            return await GetTransferOrderByIdAsync(transferOrder.Id, cancellationToken) 
                ?? throw new InvalidOperationException("Failed to retrieve created transfer order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer order.");
            throw;
        }
    }

    public async Task<TransferOrderDto> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto shipDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var transferOrder = await _context.TransferOrders
                .Include(t => t.Rows)
                    .ThenInclude(r => r.Product)
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == currentTenantId.Value && !t.IsDeleted, cancellationToken);

            if (transferOrder == null)
            {
                throw new InvalidOperationException("Transfer order not found.");
            }

            if (transferOrder.Status != TransferOrderStatus.Pending)
            {
                throw new InvalidOperationException($"Transfer order cannot be shipped. Current status: {transferOrder.Status}.");
            }

            // Validate stock availability and create stock movements OUT
            foreach (var row in transferOrder.Rows)
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductId == row.ProductId && 
                                            s.StorageLocationId == row.SourceLocationId && 
                                            s.LotId == row.LotId &&
                                            s.TenantId == currentTenantId.Value, cancellationToken);

                if (stock == null || stock.AvailableQuantity < row.QuantityOrdered)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {row.Product?.Name ?? row.ProductId.ToString()} in source location.");
                }

                // Update stock quantity
                stock.Quantity -= row.QuantityOrdered;
                stock.LastMovementDate = DateTime.UtcNow;
                stock.ModifiedBy = currentUser;
                stock.ModifiedAt = DateTime.UtcNow;

                // Create stock movement OUT
                var stockMovement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId.Value,
                    MovementType = StockMovementType.Transfer,
                    ProductId = row.ProductId,
                    LotId = row.LotId,
                    FromLocationId = row.SourceLocationId,
                    ToLocationId = null, // In transit
                    Quantity = -row.QuantityOrdered, // Negative for outbound
                    UnitCost = stock.UnitCost,
                    MovementDate = shipDto.ShipmentDate,
                    Reason = StockMovementReason.Transfer,
                    Reference = transferOrder.Number,
                    Notes = $"Transfer order {transferOrder.Number} shipped to warehouse",
                    UserId = currentUser,
                    Status = MovementStatus.Completed,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.StockMovements.Add(stockMovement);

                // Update row quantities
                row.QuantityShipped = row.QuantityOrdered;
                row.ModifiedBy = currentUser;
                row.ModifiedAt = DateTime.UtcNow;
            }

            // Update transfer order
            transferOrder.Status = TransferOrderStatus.Shipped;
            transferOrder.ShipmentDate = shipDto.ShipmentDate;
            transferOrder.ExpectedArrivalDate = shipDto.ExpectedArrivalDate;
            transferOrder.ShippingReference = shipDto.ShippingReference;
            transferOrder.ModifiedBy = currentUser;
            transferOrder.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                "TransferOrder", 
                transferOrder.Id, 
                "Status", 
                "Update", 
                "Pending", 
                "Shipped", 
                currentUser, 
                $"Transfer order {transferOrder.Number} shipped",
                cancellationToken);

            _logger.LogInformation("Transfer order {TransferOrderNumber} shipped successfully by {User}.", transferOrder.Number, currentUser);

            return await GetTransferOrderByIdAsync(transferOrder.Id, cancellationToken) 
                ?? throw new InvalidOperationException("Failed to retrieve shipped transfer order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping transfer order {TransferOrderId}.", id);
            throw;
        }
    }

    public async Task<TransferOrderDto> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto receiveDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var transferOrder = await _context.TransferOrders
                .Include(t => t.Rows)
                    .ThenInclude(r => r.Product)
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == currentTenantId.Value && !t.IsDeleted, cancellationToken);

            if (transferOrder == null)
            {
                throw new InvalidOperationException("Transfer order not found.");
            }

            if (transferOrder.Status != TransferOrderStatus.Shipped && transferOrder.Status != TransferOrderStatus.InTransit)
            {
                throw new InvalidOperationException($"Transfer order cannot be received. Current status: {transferOrder.Status}.");
            }

            // Capture original status for audit log
            var originalStatus = transferOrder.Status.ToString();

            // Process each received row
            foreach (var receiveRow in receiveDto.Rows)
            {
                var row = transferOrder.Rows.FirstOrDefault(r => r.Id == receiveRow.RowId);
                if (row == null)
                {
                    throw new InvalidOperationException($"Transfer order row {receiveRow.RowId} not found.");
                }

                // Validate destination location belongs to destination warehouse
                var destinationLocation = await _context.StorageLocations
                    .FirstOrDefaultAsync(l => l.Id == receiveRow.DestinationLocationId && 
                                            l.WarehouseId == transferOrder.DestinationWarehouseId && 
                                            l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);
                
                if (destinationLocation == null)
                {
                    throw new InvalidOperationException($"Destination location {receiveRow.DestinationLocationId} not found in destination warehouse.");
                }

                // Find or create stock entry at destination
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.ProductId == row.ProductId && 
                                            s.StorageLocationId == receiveRow.DestinationLocationId && 
                                            s.LotId == row.LotId &&
                                            s.TenantId == currentTenantId.Value, cancellationToken);

                if (stock == null)
                {
                    // Create new stock entry
                    stock = new Stock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = currentTenantId.Value,
                        ProductId = row.ProductId,
                        StorageLocationId = receiveRow.DestinationLocationId,
                        LotId = row.LotId,
                        Quantity = receiveRow.QuantityReceived,
                        ReservedQuantity = 0,
                        LastMovementDate = DateTime.UtcNow,
                        CreatedBy = currentUser,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Stocks.Add(stock);
                }
                else
                {
                    // Update existing stock
                    stock.Quantity += receiveRow.QuantityReceived;
                    stock.LastMovementDate = DateTime.UtcNow;
                    stock.ModifiedBy = currentUser;
                    stock.ModifiedAt = DateTime.UtcNow;
                }

                // Create stock movement IN
                var stockMovement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    TenantId = currentTenantId.Value,
                    MovementType = StockMovementType.Transfer,
                    ProductId = row.ProductId,
                    LotId = row.LotId,
                    FromLocationId = null, // From transit
                    ToLocationId = receiveRow.DestinationLocationId,
                    Quantity = receiveRow.QuantityReceived, // Positive for inbound
                    UnitCost = stock.UnitCost,
                    MovementDate = receiveDto.ActualArrivalDate,
                    Reason = StockMovementReason.Transfer,
                    Reference = transferOrder.Number,
                    Notes = $"Transfer order {transferOrder.Number} received from warehouse",
                    UserId = currentUser,
                    Status = MovementStatus.Completed,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.StockMovements.Add(stockMovement);

                // Update row
                row.DestinationLocationId = receiveRow.DestinationLocationId;
                row.QuantityReceived = receiveRow.QuantityReceived;
                row.ModifiedBy = currentUser;
                row.ModifiedAt = DateTime.UtcNow;
            }

            // Update transfer order
            transferOrder.Status = TransferOrderStatus.Completed;
            transferOrder.ActualArrivalDate = receiveDto.ActualArrivalDate;
            transferOrder.ModifiedBy = currentUser;
            transferOrder.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                "TransferOrder", 
                transferOrder.Id, 
                "Status", 
                "Update", 
                originalStatus, 
                "Completed", 
                currentUser, 
                $"Transfer order {transferOrder.Number} received",
                cancellationToken);

            _logger.LogInformation("Transfer order {TransferOrderNumber} received successfully by {User}.", transferOrder.Number, currentUser);

            return await GetTransferOrderByIdAsync(transferOrder.Id, cancellationToken) 
                ?? throw new InvalidOperationException("Failed to retrieve received transfer order.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving transfer order {TransferOrderId}.", id);
            throw;
        }
    }

    public async Task<bool> CancelTransferOrderAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var transferOrder = await _context.TransferOrders
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == currentTenantId.Value && !t.IsDeleted, cancellationToken);

            if (transferOrder == null)
            {
                return false;
            }

            if (transferOrder.Status == TransferOrderStatus.Shipped || 
                transferOrder.Status == TransferOrderStatus.InTransit || 
                transferOrder.Status == TransferOrderStatus.Completed)
            {
                throw new InvalidOperationException($"Cannot cancel transfer order in status {transferOrder.Status}.");
            }

            // Capture original status for audit log
            var originalStatus = transferOrder.Status.ToString();

            transferOrder.Status = TransferOrderStatus.Cancelled;
            transferOrder.ModifiedBy = currentUser;
            transferOrder.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogEntityChangeAsync(
                "TransferOrder", 
                transferOrder.Id, 
                "Status", 
                "Update", 
                originalStatus, 
                "Cancelled", 
                currentUser, 
                $"Transfer order {transferOrder.Number} cancelled",
                cancellationToken);

            _logger.LogInformation("Transfer order {TransferOrderNumber} cancelled by {User}.", transferOrder.Number, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer order {TransferOrderId}.", id);
            throw;
        }
    }

    private async Task<string> GenerateTransferOrderNumberAsync(string? series, Guid tenantId, CancellationToken cancellationToken)
    {
        var prefix = string.IsNullOrWhiteSpace(series) ? "TO" : series;
        var today = DateTime.UtcNow;
        var datePrefix = today.ToString("yyyyMMdd");

        // Get the last transfer order number for today
        var lastNumber = await _context.TransferOrders
            .Where(t => t.TenantId == tenantId && t.Number.StartsWith($"{prefix}-{datePrefix}"))
            .OrderByDescending(t => t.Number)
            .Select(t => t.Number)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var parts = lastNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        return $"{prefix}-{datePrefix}-{sequence:D4}";
    }
}
