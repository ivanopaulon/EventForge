using EventForge.DTOs.Station;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Station;

/// <summary>
/// Service implementation for managing stations and printers.
/// </summary>
public class StationService : IStationService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StationService> _logger;

    public StationService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<StationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Station Operations

    public async Task<PagedResult<StationDto>> GetStationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve stations without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var query = _context.Stations
                .Where(s => !s.IsDeleted && s.TenantId == tenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var stations = await query
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var stationDtos = new List<StationDto>();
            foreach (var station in stations)
            {
                var printerCount = await _context.Printers
                    .CountAsync(p => p.StationId == station.Id && !p.IsDeleted && p.TenantId == tenantId.Value, cancellationToken);

                stationDtos.Add(MapToStationDto(station, printerCount));
            }

            return new PagedResult<StationDto>
            {
                Items = stationDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stations");
            throw;
        }
    }

    public async Task<StationDto?> GetStationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve station without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
            {
                _logger.LogWarning("Station with ID {StationId} not found.", id);
                return null;
            }

            var printerCount = await _context.Printers
                .CountAsync(p => p.StationId == id && !p.IsDeleted && p.TenantId == tenantId.Value, cancellationToken);

            return MapToStationDto(station, printerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving station with ID {StationId}", id);
            throw;
        }
    }

    public async Task<StationDto> CreateStationAsync(CreateStationDto createStationDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot create station without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var station = new EventForge.Server.Data.Entities.StationMonitor.Station
            {
                TenantId = tenantId.Value,
                Name = createStationDto.Name,
                Description = createStationDto.Description,
                Location = createStationDto.Location,
                SortOrder = createStationDto.SortOrder,
                Notes = createStationDto.Notes,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _context.Stations.Add(station);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(station, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Station {StationName} created with ID {StationId} by {User}",
                station.Name, station.Id, currentUser);

            return MapToStationDto(station, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating station");
            throw;
        }
    }

    public async Task<StationDto?> UpdateStationAsync(Guid id, UpdateStationDto updateStationDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot update station without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalStation = await _context.Stations
                .AsNoTracking()
                .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStation == null)
            {
                _logger.LogWarning("Station with ID {StationId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
            {
                _logger.LogWarning("Station with ID {StationId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            station.Name = updateStationDto.Name;
            station.Description = updateStationDto.Description;
            station.Location = updateStationDto.Location;
            station.SortOrder = updateStationDto.SortOrder;
            station.Notes = updateStationDto.Notes;
            station.ModifiedAt = DateTime.UtcNow;
            station.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(station, "Update", currentUser, originalStation, cancellationToken);

            _logger.LogInformation("Station {StationId} updated by {User}", id, currentUser);

            var printerCount = await _context.Printers
                .CountAsync(p => p.StationId == id && !p.IsDeleted && p.TenantId == tenantId.Value, cancellationToken);

            return MapToStationDto(station, printerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating station with ID {StationId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteStationAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot delete station without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalStation = await _context.Stations
                .AsNoTracking()
                .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStation == null)
            {
                _logger.LogWarning("Station with ID {StationId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
            {
                _logger.LogWarning("Station with ID {StationId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            station.IsDeleted = true;
            station.DeletedAt = DateTime.UtcNow;
            station.DeletedBy = currentUser;
            station.ModifiedAt = DateTime.UtcNow;
            station.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(station, "Delete", currentUser, originalStation, cancellationToken);

            _logger.LogInformation("Station {StationId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting station with ID {StationId}", id);
            throw;
        }
    }

    #endregion

    #region Printer Operations

    public async Task<PagedResult<PrinterDto>> GetPrintersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve printers without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var query = _context.Printers
                .Include(p => p.Station)
                .Where(p => !p.IsDeleted && p.TenantId == tenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var printers = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var printerDtos = printers.Select(MapToPrinterDto);

            return new PagedResult<PrinterDto>
            {
                Items = printerDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving printers");
            throw;
        }
    }

    public async Task<PrinterDto?> GetPrinterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve printer without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var printer = await _context.Printers
                .Include(p => p.Station)
                .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (printer == null)
            {
                _logger.LogWarning("Printer with ID {PrinterId} not found.", id);
                return null;
            }

            return MapToPrinterDto(printer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving printer with ID {PrinterId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<PrinterDto>> GetPrintersByStationAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve printers without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var printers = await _context.Printers
                .Include(p => p.Station)
                .Where(p => p.StationId == stationId && !p.IsDeleted && p.TenantId == tenantId.Value)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return printers.Select(MapToPrinterDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving printers for station {StationId}", stationId);
            throw;
        }
    }

    public async Task<PrinterDto> CreatePrinterAsync(CreatePrinterDto createPrinterDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot create printer without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var printer = new Printer
            {
                TenantId = tenantId.Value,
                Name = createPrinterDto.Name,
                Type = createPrinterDto.Type,
                Model = createPrinterDto.Model,
                Location = createPrinterDto.Location,
                Address = createPrinterDto.Address,
                StationId = createPrinterDto.StationId,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _context.Printers.Add(printer);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(printer, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Printer {PrinterName} created with ID {PrinterId} by {User}",
                printer.Name, printer.Id, currentUser);

            // Reload with includes
            var createdPrinter = await _context.Printers
                .Include(p => p.Station)
                .FirstAsync(p => p.Id == printer.Id, cancellationToken);

            return MapToPrinterDto(createdPrinter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating printer");
            throw;
        }
    }

    public async Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updatePrinterDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot update printer without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalPrinter = await _context.Printers
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPrinter == null)
            {
                _logger.LogWarning("Printer with ID {PrinterId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var printer = await _context.Printers
                .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (printer == null)
            {
                _logger.LogWarning("Printer with ID {PrinterId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            printer.Name = updatePrinterDto.Name;
            printer.Type = updatePrinterDto.Type;
            printer.Model = updatePrinterDto.Model;
            printer.Location = updatePrinterDto.Location;
            printer.Address = updatePrinterDto.Address;
            printer.StationId = updatePrinterDto.StationId;
            printer.ModifiedAt = DateTime.UtcNow;
            printer.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(printer, "Update", currentUser, originalPrinter, cancellationToken);

            _logger.LogInformation("Printer {PrinterId} updated by {User}", id, currentUser);

            // Reload with includes
            var updatedPrinter = await _context.Printers
                .Include(p => p.Station)
                .FirstAsync(p => p.Id == id, cancellationToken);

            return MapToPrinterDto(updatedPrinter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating printer with ID {PrinterId}", id);
            throw;
        }
    }

    public async Task<bool> DeletePrinterAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot delete printer without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalPrinter = await _context.Printers
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPrinter == null)
            {
                _logger.LogWarning("Printer with ID {PrinterId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var printer = await _context.Printers
                .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (printer == null)
            {
                _logger.LogWarning("Printer with ID {PrinterId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            printer.IsDeleted = true;
            printer.DeletedAt = DateTime.UtcNow;
            printer.DeletedBy = currentUser;
            printer.ModifiedAt = DateTime.UtcNow;
            printer.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(printer, "Delete", currentUser, originalPrinter, cancellationToken);

            _logger.LogInformation("Printer {PrinterId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting printer with ID {PrinterId}", id);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    public async Task<bool> StationExistsAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await _context.Stations
            .AnyAsync(s => s.Id == stationId && !s.IsDeleted && s.TenantId == tenantId.Value, cancellationToken);
    }

    private static StationDto MapToStationDto(EventForge.Server.Data.Entities.StationMonitor.Station station, int printerCount)
    {
        return new StationDto
        {
            Id = station.Id,
            Name = station.Name,
            Description = station.Description,
            Location = station.Location,
            SortOrder = station.SortOrder,
            Notes = station.Notes,
            PrinterCount = printerCount,
            CreatedAt = station.CreatedAt,
            CreatedBy = station.CreatedBy,
            ModifiedAt = station.ModifiedAt,
            ModifiedBy = station.ModifiedBy
        };
    }

    private static PrinterDto MapToPrinterDto(Printer printer)
    {
        return new PrinterDto
        {
            Id = printer.Id,
            Name = printer.Name,
            Type = printer.Type,
            Model = printer.Model,
            Location = printer.Location,
            Address = printer.Address,
            StationId = printer.StationId,
            StationName = printer.Station?.Name,
            CreatedAt = printer.CreatedAt,
            CreatedBy = printer.CreatedBy,
            ModifiedAt = printer.ModifiedAt,
            ModifiedBy = printer.ModifiedBy
        };
    }

    #endregion
}