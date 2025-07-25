using EventForge.Server.DTOs.Station;
using EventForge.Server.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Station;

/// <summary>
/// Service implementation for managing stations and printers.
/// </summary>
public class StationService : IStationService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StationService> _logger;

    public StationService(EventForgeDbContext context, IAuditLogService auditLogService, ILogger<StationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Station Operations

    public async Task<PagedResult<StationDto>> GetStationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Stations
                .Where(s => !s.IsDeleted);

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
                    .CountAsync(p => p.StationId == station.Id && !p.IsDeleted, cancellationToken);

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
            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
                return null;

            var printerCount = await _context.Printers
                .CountAsync(p => p.StationId == id && !p.IsDeleted, cancellationToken);

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
            var station = new EventForge.Server.Data.Entities.StationMonitor.Station
            {
                Name = createStationDto.Name,
                Description = createStationDto.Description,
                Status = createStationDto.Status,
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
            var originalStation = await _context.Stations
                .AsNoTracking()
                .Where(s => s.Id == id && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStation == null)
                return null;

            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
                return null;

            station.Name = updateStationDto.Name;
            station.Description = updateStationDto.Description;
            station.Status = updateStationDto.Status;
            station.Location = updateStationDto.Location;
            station.SortOrder = updateStationDto.SortOrder;
            station.Notes = updateStationDto.Notes;
            station.ModifiedAt = DateTime.UtcNow;
            station.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(station, "Update", currentUser, originalStation, cancellationToken);

            _logger.LogInformation("Station {StationId} updated by {User}", id, currentUser);

            var printerCount = await _context.Printers
                .CountAsync(p => p.StationId == id && !p.IsDeleted, cancellationToken);

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
            var originalStation = await _context.Stations
                .AsNoTracking()
                .Where(s => s.Id == id && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalStation == null)
                return false;

            var station = await _context.Stations
                .Where(s => s.Id == id && !s.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (station == null)
                return false;

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
            var query = _context.Printers
                .Include(p => p.Station)
                .Where(p => !p.IsDeleted);

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
            var printer = await _context.Printers
                .Include(p => p.Station)
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return printer != null ? MapToPrinterDto(printer) : null;
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
            var printers = await _context.Printers
                .Include(p => p.Station)
                .Where(p => p.StationId == stationId && !p.IsDeleted)
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
            var printer = new Printer
            {
                Name = createPrinterDto.Name,
                Type = createPrinterDto.Type,
                Model = createPrinterDto.Model,
                Location = createPrinterDto.Location,
                Address = createPrinterDto.Address,
                Status = createPrinterDto.Status,
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
            var originalPrinter = await _context.Printers
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPrinter == null)
                return null;

            var printer = await _context.Printers
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (printer == null)
                return null;

            printer.Name = updatePrinterDto.Name;
            printer.Type = updatePrinterDto.Type;
            printer.Model = updatePrinterDto.Model;
            printer.Location = updatePrinterDto.Location;
            printer.Address = updatePrinterDto.Address;
            printer.Status = updatePrinterDto.Status;
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
            var originalPrinter = await _context.Printers
                .AsNoTracking()
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPrinter == null)
                return false;

            var printer = await _context.Printers
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (printer == null)
                return false;

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
        return await _context.Stations
            .AnyAsync(s => s.Id == stationId && !s.IsDeleted, cancellationToken);
    }

    private static StationDto MapToStationDto(EventForge.Server.Data.Entities.StationMonitor.Station station, int printerCount)
    {
        return new StationDto
        {
            Id = station.Id,
            Name = station.Name,
            Description = station.Description,
            Status = station.Status,
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
            Status = printer.Status,
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