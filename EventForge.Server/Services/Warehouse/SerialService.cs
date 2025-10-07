using EventForge.DTOs.Warehouse;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing individual serial numbers/matricole.
/// </summary>
public class SerialService : ISerialService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SerialService> _logger;

    public SerialService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<SerialService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<SerialDto>> GetSerialsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? lotId = null,
        Guid? locationId = null,
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

            var query = _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.TenantId == currentTenantId.Value);

            // Apply filters
            if (productId.HasValue)
            {
                query = query.Where(s => s.ProductId == productId.Value);
            }

            if (lotId.HasValue)
            {
                query = query.Where(s => s.LotId == lotId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(s => s.CurrentLocationId == locationId.Value);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<SerialStatus>(status, out var serialStatus))
            {
                query = query.Where(s => s.Status == serialStatus);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.SerialNumber.Contains(searchTerm) ||
                                        (s.Barcode != null && s.Barcode.Contains(searchTerm)) ||
                                        (s.RfidTag != null && s.RfidTag.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var serials = await query
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var serialDtos = serials.Select(s => s.ToSerialDto()).ToList();

            return new PagedResult<SerialDto>
            {
                Items = serialDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials with filters - ProductId: {ProductId}, LotId: {LotId}, LocationId: {LocationId}, Status: {Status}",
                productId, lotId, locationId, status);
            throw;
        }
    }

    public async Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            return serial?.ToSerialDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serial by ID: {SerialId}", id);
            throw;
        }
    }

    public async Task<SerialDto?> GetSerialByNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.SerialNumber == serialNumber && s.TenantId == currentTenantId.Value, cancellationToken);

            return serial?.ToSerialDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serial by number: {SerialNumber}", serialNumber);
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials by product ID: {ProductId}", productId);
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.LotId == lotId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials by lot ID: {LotId}", lotId);
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.CurrentLocationId == locationId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials by location ID: {LocationId}", locationId);
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.OwnerId == ownerId && s.TenantId == currentTenantId.Value)
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials by owner ID: {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsWithExpiringWarrantyAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var thresholdDate = DateTime.UtcNow.AddDays(daysAhead);

            var serials = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.TenantId == currentTenantId.Value &&
                           s.WarrantyExpiry.HasValue &&
                           s.WarrantyExpiry.Value <= thresholdDate &&
                           s.WarrantyExpiry.Value > DateTime.UtcNow)
                .OrderBy(s => s.WarrantyExpiry)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serials with expiring warranty within {DaysAhead} days", daysAhead);
            throw;
        }
    }

    public async Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            // Check if serial number is unique
            var existingSerial = await _context.Serials
                .FirstOrDefaultAsync(s => s.SerialNumber == createDto.SerialNumber && s.TenantId == currentTenantId.Value, cancellationToken);

            if (existingSerial != null)
            {
                throw new InvalidOperationException($"Serial number '{createDto.SerialNumber}' already exists.");
            }

            var newSerial = createDto.ToEntity(currentTenantId.Value, currentUser);
            _ = _context.Serials.Add(newSerial);

            _ = await _auditLogService.LogEntityChangeAsync("Serial", newSerial.Id, "Created", "Create", null,
                $"Created serial number {createDto.SerialNumber} for product {createDto.ProductId}", currentUser);

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Reload with includes for DTO mapping
            var serialForDto = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstAsync(s => s.Id == newSerial.Id, cancellationToken);

            return serialForDto.ToSerialDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating serial: {SerialNumber}", createDto.SerialNumber);
            throw;
        }
    }

    public async Task<SerialDto?> UpdateSerialAsync(Guid id, UpdateSerialDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null)
            {
                return null;
            }

            // Check serial number uniqueness if it's being changed
            if (!string.IsNullOrEmpty(updateDto.SerialNumber) && updateDto.SerialNumber != serial.SerialNumber)
            {
                var existingSerial = await _context.Serials
                    .FirstOrDefaultAsync(s => s.SerialNumber == updateDto.SerialNumber &&
                                            s.TenantId == currentTenantId.Value &&
                                            s.Id != id, cancellationToken);

                if (existingSerial != null)
                {
                    throw new InvalidOperationException($"Serial number '{updateDto.SerialNumber}' already exists.");
                }
            }

            serial.UpdateFromDto(updateDto, currentUser);

            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Updated", "Update", null, "Updated serial information", currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return serial.ToSerialDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating serial: {SerialId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            if (!Enum.TryParse<SerialStatus>(status, out var serialStatus))
            {
                throw new ArgumentException($"Invalid serial status: {status}");
            }

            var serial = await _context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null)
            {
                return false;
            }

            var oldStatus = serial.Status.ToString();
            serial.Status = serialStatus;
            if (!string.IsNullOrEmpty(notes))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? notes : $"{serial.Notes}\n{notes}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Status", "StatusUpdate", oldStatus,
                status, currentUser);
            // Add notes separately if provided
            if (!string.IsNullOrEmpty(notes))
            {
                _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Notes", "Update", null,
                    $"Status change notes: {notes}", currentUser);
            }
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating serial status - ID: {SerialId}, Status: {Status}", id, status);
            throw;
        }
    }

    public async Task<bool> MoveSerialAsync(Guid id, Guid newLocationId, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .Include(s => s.CurrentLocation)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null)
            {
                return false;
            }

            var oldLocationId = serial.CurrentLocationId;
            serial.CurrentLocationId = newLocationId;
            if (!string.IsNullOrEmpty(notes))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? notes : $"{serial.Notes}\n{notes}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Location", "Move", oldLocationId?.ToString(),
                newLocationId.ToString(), currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving serial - ID: {SerialId}, NewLocation: {LocationId}", id, newLocationId);
            throw;
        }
    }

    public async Task<bool> SellSerialAsync(Guid id, Guid customerId, DateTime saleDate, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null || serial.Status != SerialStatus.Available)
            {
                return false;
            }

            serial.Status = SerialStatus.Sold;
            serial.OwnerId = customerId;
            serial.SaleDate = saleDate;
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Owner", "Sell", null,
                customerId.ToString(), currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selling serial - ID: {SerialId}, Customer: {CustomerId}", id, customerId);
            throw;
        }
    }

    public async Task<bool> ReturnSerialAsync(Guid id, Guid? newLocationId, string currentUser, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null || serial.Status != SerialStatus.Sold)
            {
                return false;
            }

            serial.Status = SerialStatus.Available;
            serial.OwnerId = null;
            serial.CurrentLocationId = newLocationId;
            if (!string.IsNullOrEmpty(reason))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? $"Return reason: {reason}" : $"{serial.Notes}\nReturn reason: {reason}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Owner", "Return", serial.OwnerId?.ToString(),
                null, currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning serial - ID: {SerialId}, Reason: {Reason}", id, reason);
            throw;
        }
    }

    public async Task<bool> IsSerialNumberUniqueAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = _context.Serials
                .Where(s => s.SerialNumber == serialNumber && s.TenantId == currentTenantId.Value);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking serial number uniqueness: {SerialNumber}", serialNumber);
            throw;
        }
    }

    public async Task<bool> DeleteSerialAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await _context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value, cancellationToken);

            if (serial == null)
            {
                return false;
            }

            _ = _context.Serials.Remove(serial);
            _ = await _auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Deleted", "Delete", null,
                $"Deleted serial number {serial.SerialNumber}", currentUser);
            _ = await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting serial: {SerialId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<StockMovementDto>> GetSerialHistoryAsync(Guid serialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var movements = await _context.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.FromLocation)
                .Include(sm => sm.ToLocation)
                .Include(sm => sm.DocumentHeader)
                .Where(sm => sm.SerialId == serialId && sm.TenantId == currentTenantId.Value)
                .OrderByDescending(sm => sm.MovementDate)
                .ToListAsync(cancellationToken);

            return movements.Select(sm => sm.ToStockMovementDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting serial history: {SerialId}", serialId);
            throw;
        }
    }
}