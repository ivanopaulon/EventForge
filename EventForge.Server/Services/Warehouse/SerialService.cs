using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing individual serial numbers/matricole.
/// </summary>
public class SerialService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<SerialService> logger) : ISerialService
{
    private static readonly IReadOnlyDictionary<string, SerialStatus> StatusAliases =
        new Dictionary<string, SerialStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["Available"] = SerialStatus.Available,
            ["Sold"] = SerialStatus.Sold,
            ["InUse"] = SerialStatus.InUse,
            ["InMaintenance"] = SerialStatus.Maintenance,
            ["Maintenance"] = SerialStatus.Maintenance,
            ["Defective"] = SerialStatus.Defective,
            ["Retired"] = SerialStatus.Scrapped,
            ["Scrapped"] = SerialStatus.Scrapped,
            ["Lost"] = SerialStatus.Recalled,
            ["Recalled"] = SerialStatus.Recalled
        };

    private static readonly SerialStatus[] OperationalStatuses =
    [
        SerialStatus.Available,
        SerialStatus.InUse,
        SerialStatus.Maintenance
    ];

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
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = context.Serials
                .AsNoTracking()
                .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted);

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

            if (!string.IsNullOrEmpty(status) && TryParseSerialStatus(status, out var serialStatus))
            {
                query = query.Where(s => s.Status == serialStatus);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.SerialNumber.Contains(searchTerm) ||
                                        (s.Barcode != null && s.Barcode.Contains(searchTerm)) ||
                                        (s.RfidTag != null && s.RfidTag.Contains(searchTerm)) ||
                                        (s.Product != null && (s.Product.Name.Contains(searchTerm) || s.Product.Code.Contains(searchTerm))) ||
                                        (s.Lot != null && s.Lot.Code.Contains(searchTerm)) ||
                                        (s.CurrentLocation != null && s.CurrentLocation.Code.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var serialDtos = await query
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SerialDto
                {
                    Id = s.Id,
                    TenantId = s.TenantId,
                    SerialNumber = s.SerialNumber,
                    ProductId = s.ProductId,
                    ProductName = s.Product != null ? s.Product.Name : null,
                    ProductCode = s.Product != null ? s.Product.Code : null,
                    LotId = s.LotId,
                    LotCode = s.Lot != null ? s.Lot.Code : null,
                    CurrentLocationId = s.CurrentLocationId,
                    CurrentLocationCode = s.CurrentLocation != null ? s.CurrentLocation.Code : null,
                    WarehouseName = s.CurrentLocation != null && s.CurrentLocation.Warehouse != null ? s.CurrentLocation.Warehouse.Name : null,
                    Status = s.Status.ToString(),
                    ManufacturingDate = s.ManufacturingDate,
                    WarrantyExpiry = s.WarrantyExpiry,
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner != null ? s.Owner.Name : null,
                    SaleDate = s.SaleDate,
                    Notes = s.Notes,
                    Barcode = s.Barcode,
                    RfidTag = s.RfidTag,
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy,
                    ModifiedAt = s.ModifiedAt,
                    ModifiedBy = s.ModifiedBy,
                    IsActive = s.IsActive
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SerialDto>
            {
                Items = serialDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            return await context.Serials
                .AsNoTracking()
                .Where(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .Select(s => new SerialDto
                {
                    Id = s.Id,
                    TenantId = s.TenantId,
                    SerialNumber = s.SerialNumber,
                    ProductId = s.ProductId,
                    ProductName = s.Product != null ? s.Product.Name : null,
                    ProductCode = s.Product != null ? s.Product.Code : null,
                    LotId = s.LotId,
                    LotCode = s.Lot != null ? s.Lot.Code : null,
                    CurrentLocationId = s.CurrentLocationId,
                    CurrentLocationCode = s.CurrentLocation != null ? s.CurrentLocation.Code : null,
                    WarehouseName = s.CurrentLocation != null && s.CurrentLocation.Warehouse != null ? s.CurrentLocation.Warehouse.Name : null,
                    Status = s.Status.ToString(),
                    ManufacturingDate = s.ManufacturingDate,
                    WarrantyExpiry = s.WarrantyExpiry,
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner != null ? s.Owner.Name : null,
                    SaleDate = s.SaleDate,
                    Notes = s.Notes,
                    Barcode = s.Barcode,
                    RfidTag = s.RfidTag,
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy,
                    ModifiedAt = s.ModifiedAt,
                    ModifiedBy = s.ModifiedBy,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<SerialDto?> GetSerialByNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            return await context.Serials
                .AsNoTracking()
                .Where(s => s.SerialNumber == serialNumber && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .Select(s => new SerialDto
                {
                    Id = s.Id,
                    TenantId = s.TenantId,
                    SerialNumber = s.SerialNumber,
                    ProductId = s.ProductId,
                    ProductName = s.Product != null ? s.Product.Name : null,
                    ProductCode = s.Product != null ? s.Product.Code : null,
                    LotId = s.LotId,
                    LotCode = s.Lot != null ? s.Lot.Code : null,
                    CurrentLocationId = s.CurrentLocationId,
                    CurrentLocationCode = s.CurrentLocation != null ? s.CurrentLocation.Code : null,
                    WarehouseName = s.CurrentLocation != null && s.CurrentLocation.Warehouse != null ? s.CurrentLocation.Warehouse.Name : null,
                    Status = s.Status.ToString(),
                    ManufacturingDate = s.ManufacturingDate,
                    WarrantyExpiry = s.WarrantyExpiry,
                    OwnerId = s.OwnerId,
                    OwnerName = s.Owner != null ? s.Owner.Name : null,
                    SaleDate = s.SaleDate,
                    Notes = s.Notes,
                    Barcode = s.Barcode,
                    RfidTag = s.RfidTag,
                    CreatedAt = s.CreatedAt,
                    CreatedBy = s.CreatedBy,
                    ModifiedAt = s.ModifiedAt,
                    ModifiedBy = s.ModifiedBy,
                    IsActive = s.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.ProductId == productId && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .OrderBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.LotId == lotId && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .OrderBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.CurrentLocationId == locationId && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serials = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.OwnerId == ownerId && s.TenantId == currentTenantId.Value && !s.IsDeleted)
                .OrderBy(s => s.Product!.Name)
                .ThenBy(s => s.SerialNumber)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<SerialDto>> GetSerialsWithExpiringWarrantyAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var thresholdDate = DateTime.UtcNow.AddDays(daysAhead);

            var serials = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .Where(s => s.TenantId == currentTenantId.Value &&
                           !s.IsDeleted &&
                           OperationalStatuses.Contains(s.Status) &&
                           s.WarrantyExpiry.HasValue &&
                           s.WarrantyExpiry.Value <= thresholdDate &&
                           s.WarrantyExpiry.Value > DateTime.UtcNow)
                .OrderBy(s => s.WarrantyExpiry)
                .ToListAsync(cancellationToken);

            return serials.Select(s => s.ToSerialDto());
        }
        catch
        {
            throw;
        }
    }

    public async Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            createDto.SerialNumber = createDto.SerialNumber.Trim();

            // Check if serial number is unique (excluding soft-deleted)
            var existingSerial = await context.Serials
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SerialNumber == createDto.SerialNumber && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (existingSerial is not null)
            {
                throw new InvalidOperationException($"Serial number '{createDto.SerialNumber}' already exists.");
            }

            var newSerial = createDto.ToEntity(currentTenantId.Value, currentUser);
            _ = context.Serials.Add(newSerial);

            _ = await auditLogService.LogEntityChangeAsync("Serial", newSerial.Id, "Created", "Create", null,
                $"Created serial number {createDto.SerialNumber} for product {createDto.ProductId}", currentUser);

            _ = await context.SaveChangesAsync(cancellationToken);

            // Reload with includes for DTO mapping
            var serialForDto = await context.Serials
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstAsync(s => s.Id == newSerial.Id, cancellationToken);

            return serialForDto.ToSerialDto();
        }
        catch
        {
            throw;
        }
    }

    public async Task<SerialDto?> UpdateSerialAsync(Guid id, UpdateSerialDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await context.Serials
                .Include(s => s.Product)
                .Include(s => s.Lot)
                .Include(s => s.CurrentLocation)
                    .ThenInclude(cl => cl!.Warehouse)
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(updateDto.SerialNumber))
            {
                throw new ArgumentException("Serial number is required.");
            }

            var normalizedSerialNumber = updateDto.SerialNumber.Trim();

            // Check serial number uniqueness if it's being changed
            if (normalizedSerialNumber != serial.SerialNumber)
            {
                var existingSerial = await context.Serials
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SerialNumber == normalizedSerialNumber &&
                                            s.TenantId == currentTenantId.Value &&
                                            s.Id != id &&
                                            !s.IsDeleted, cancellationToken);

                if (existingSerial is not null)
                {
                    throw new InvalidOperationException($"Serial number '{normalizedSerialNumber}' already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Status))
            {
                if (!TryParseSerialStatus(updateDto.Status, out var requestedStatus))
                {
                    throw new ArgumentException($"Invalid serial status: {updateDto.Status}");
                }

                if (requestedStatus == SerialStatus.Sold)
                {
                    throw new InvalidOperationException("Use the dedicated sell workflow to mark a serial as sold.");
                }

                if (serial.Status == SerialStatus.Sold && requestedStatus != SerialStatus.Sold)
                {
                    throw new InvalidOperationException("Use the dedicated return workflow to restore a sold serial.");
                }
            }

            updateDto.SerialNumber = normalizedSerialNumber;
            serial.UpdateFromDto(updateDto, currentUser);

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Updated", "Update", null, "Updated serial information", currentUser);
            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Serial {SerialId}.", id);
                throw new InvalidOperationException("Il seriale è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            return serial.ToSerialDto();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            if (!TryParseSerialStatus(status, out var serialStatus))
            {
                throw new ArgumentException($"Invalid serial status: {status}");
            }

            var serial = await context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null)
            {
                return false;
            }

            if (serialStatus == SerialStatus.Sold)
            {
                throw new InvalidOperationException("Use the dedicated sell workflow to mark a serial as sold.");
            }

            if (serial.Status == SerialStatus.Sold && serialStatus != SerialStatus.Sold)
            {
                throw new InvalidOperationException("Use the dedicated return workflow to restore a sold serial.");
            }

            if (serial.Status == SerialStatus.Scrapped && serialStatus != SerialStatus.Scrapped)
            {
                throw new InvalidOperationException("A scrapped serial cannot change status.");
            }

            var oldStatus = serial.Status.ToString();
            serial.Status = serialStatus;
            if (!string.IsNullOrEmpty(notes))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? notes : $"{serial.Notes}\n{notes}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Status", "StatusUpdate", oldStatus,
                status, currentUser);
            // Add notes separately if provided
            if (!string.IsNullOrEmpty(notes))
            {
                _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Notes", "Update", null,
                    $"Status change notes: {notes}", currentUser);
            }
            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Serial status {SerialId}.", id);
                throw new InvalidOperationException("Il seriale è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> MoveSerialAsync(Guid id, Guid newLocationId, string currentUser, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await context.Serials
                .Include(s => s.CurrentLocation)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null)
            {
                return false;
            }

            if (serial.Status is SerialStatus.Sold or SerialStatus.Scrapped)
            {
                throw new InvalidOperationException("The serial cannot be moved in its current status.");
            }

            var targetLocationExists = await context.StorageLocations
                .AsNoTracking()
                .AnyAsync(l => l.Id == newLocationId && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

            if (!targetLocationExists)
            {
                throw new InvalidOperationException("Target location not found.");
            }

            var oldLocationId = serial.CurrentLocationId;
            serial.CurrentLocationId = newLocationId;
            if (!string.IsNullOrEmpty(notes))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? notes : $"{serial.Notes}\n{notes}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Location", "Move", oldLocationId?.ToString(),
                newLocationId.ToString(), currentUser);
            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> SellSerialAsync(Guid id, Guid customerId, DateTime saleDate, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null || serial.Status != SerialStatus.Available)
            {
                return false;
            }

            var customer = await context.BusinessParties
                .AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == customerId &&
                                           bp.TenantId == currentTenantId.Value &&
                                           !bp.IsDeleted, cancellationToken);

            if (customer is null ||
                (customer.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente &&
                 customer.PartyType != EventForge.Server.Data.Entities.Business.BusinessPartyType.ClienteFornitore))
            {
                throw new InvalidOperationException("Customer not found.");
            }

            serial.Status = SerialStatus.Sold;
            serial.OwnerId = customerId;
            serial.SaleDate = saleDate;
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;
            serial.CurrentLocationId = null;

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Owner", "Sell", null,
                customerId.ToString(), currentUser);
            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> ReturnSerialAsync(Guid id, Guid? newLocationId, string currentUser, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null || serial.Status != SerialStatus.Sold)
            {
                return false;
            }

            if (newLocationId.HasValue)
            {
                var locationExists = await context.StorageLocations
                    .AsNoTracking()
                    .AnyAsync(l => l.Id == newLocationId.Value && l.TenantId == currentTenantId.Value && !l.IsDeleted, cancellationToken);

                if (!locationExists)
                {
                    throw new InvalidOperationException("Target location not found.");
                }
            }

            var previousOwnerId = serial.OwnerId;
            serial.Status = SerialStatus.Available;
            serial.OwnerId = null;
            serial.CurrentLocationId = newLocationId;
            serial.SaleDate = null;
            if (!string.IsNullOrEmpty(reason))
            {
                serial.Notes = string.IsNullOrEmpty(serial.Notes) ? $"Return reason: {reason}" : $"{serial.Notes}\nReturn reason: {reason}";
            }
            serial.ModifiedBy = currentUser;
            serial.ModifiedAt = DateTime.UtcNow;

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Owner", "Return", previousOwnerId?.ToString(),
                null, currentUser);
            _ = await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> IsSerialNumberUniqueAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var query = context.Serials
                .AsNoTracking()
                .Where(s => s.SerialNumber == serialNumber && s.TenantId == currentTenantId.Value && !s.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteSerialAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var serial = await context.Serials
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == currentTenantId.Value && !s.IsDeleted, cancellationToken);

            if (serial is null)
            {
                return false;
            }

            // Soft delete — keeps the record for audit trail and preserves StockMovement references
            serial.IsDeleted = true;
            serial.DeletedAt = DateTime.UtcNow;
            serial.DeletedBy = currentUser;

            _ = await auditLogService.LogEntityChangeAsync("Serial", serial.Id, "Deleted", "Delete", null,
                $"Deleted serial number {serial.SerialNumber}", currentUser);
            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Serial {SerialId}.", id);
                throw new InvalidOperationException("Il seriale è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            logger.LogInformation("Soft-deleted serial {SerialId} ({SerialNumber}) for tenant {TenantId}", serial.Id, serial.SerialNumber, currentTenantId.Value);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch
        {
            throw;
        }
    }

    public async Task<IEnumerable<StockMovementDto>> GetSerialHistoryAsync(Guid serialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Current tenant ID is not available.");
            }

            var movements = await context.StockMovements
                .AsNoTracking()
                .Include(sm => sm.Product)
                .Include(sm => sm.FromLocation)
                .Include(sm => sm.ToLocation)
                .Include(sm => sm.DocumentHeader)
                .Where(sm => sm.SerialId == serialId && sm.TenantId == currentTenantId.Value)
                .OrderByDescending(sm => sm.MovementDate)
                .ToListAsync(cancellationToken);

            return movements.Select(sm => sm.ToStockMovementDto());
        }
        catch
        {
            throw;
        }
    }

    private static bool TryParseSerialStatus(string status, out SerialStatus serialStatus)
    {
        if (StatusAliases.TryGetValue(status, out serialStatus))
        {
            return true;
        }

        return Enum.TryParse(status, ignoreCase: true, out serialStatus);
    }
}
