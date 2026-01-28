using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;
using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

public class TableManagementService : ITableManagementService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<TableManagementService> _logger;
    private readonly ITenantContext _tenantContext;

    public TableManagementService(
        EventForgeDbContext context,
        ILogger<TableManagementService> logger,
        ITenantContext tenantContext)
    {
        _context = context;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    private Guid GetTenantId()
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for table management operations.");
        }
        return tenantId.Value;
    }

    public async Task<PagedResult<TableSessionDto>> GetTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting tables with pagination for tenant {TenantId}", tenantId);

        var query = _context.Set<TableSession>()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var tables = await query
            .OrderBy(t => t.Area)
            .ThenBy(t => t.TableNumber)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TableSessionDto>
        {
            Items = tables.Select(MapToDto),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<TableSessionDto>> GetTablesByZoneAsync(string zone, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting tables by zone {Zone} with pagination for tenant {TenantId}", zone, tenantId);

        var query = _context.Set<TableSession>()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.Area == zone);

        var totalCount = await query.CountAsync(cancellationToken);

        var tables = await query
            .OrderBy(t => t.TableNumber)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TableSessionDto>
        {
            Items = tables.Select(MapToDto),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<TableSessionDto>> GetAvailableTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting available tables with pagination for tenant {TenantId}", tenantId);

        var query = _context.Set<TableSession>()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.IsActive && t.Status == TableStatus.Available);

        var totalCount = await query.CountAsync(cancellationToken);

        var tables = await query
            .OrderBy(t => t.Area)
            .ThenBy(t => t.Capacity)
            .ThenBy(t => t.TableNumber)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TableSessionDto>
        {
            Items = tables.Select(MapToDto),
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<List<TableSessionDto>> GetAllTablesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting all tables for tenant {TenantId}", tenantId);

        var tables = await _context.Set<TableSession>()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted)
            .OrderBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);

        return tables.Select(MapToDto).ToList();
    }

    public async Task<TableSessionDto?> GetTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting table {TableId} for tenant {TenantId}", tableId, tenantId);

        var table = await _context.Set<TableSession>()
            .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        return table != null ? MapToDto(table) : null;
    }

    public async Task<List<TableSessionDto>> GetAvailableTablesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting available tables for tenant {TenantId}", tenantId);

        var tables = await _context.Set<TableSession>()
            .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.IsActive && t.Status == TableStatus.Available)
            .OrderBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);

        return tables.Select(MapToDto).ToList();
    }

    public async Task<TableSessionDto> CreateTableAsync(CreateTableSessionDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating table {TableNumber} for tenant {TenantId}", dto.TableNumber, tenantId);

        var exists = await _context.Set<TableSession>()
            .AnyAsync(t => t.TenantId == tenantId && t.TableNumber == dto.TableNumber && !t.IsDeleted, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Table with number '{dto.TableNumber}' already exists.");
        }

        var table = new TableSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TableNumber = dto.TableNumber,
            TableName = dto.TableName,
            Capacity = dto.Capacity,
            Area = dto.Area,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Status = TableStatus.Available,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.Set<TableSession>().Add(table);
        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created table {TableId} with number {TableNumber}", table.Id, table.TableNumber);

        return MapToDto(table);
    }

    public async Task<TableSessionDto?> UpdateTableAsync(Guid tableId, UpdateTableSessionDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating table {TableId} for tenant {TenantId}", tableId, tenantId);

        var table = await _context.Set<TableSession>()
            .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        if (table == null)
        {
            _logger.LogWarning("Table {TableId} not found", tableId);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dto.TableNumber) && dto.TableNumber != table.TableNumber)
        {
            var exists = await _context.Set<TableSession>()
                .AnyAsync(t => t.TenantId == tenantId && t.TableNumber == dto.TableNumber && t.Id != tableId && !t.IsDeleted, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Table with number '{dto.TableNumber}' already exists.");
            }

            table.TableNumber = dto.TableNumber;
        }

        if (dto.TableName != null) table.TableName = dto.TableName;
        if (dto.Capacity.HasValue) table.Capacity = dto.Capacity.Value;
        if (dto.Area != null) table.Area = dto.Area;
        if (dto.PositionX.HasValue) table.PositionX = dto.PositionX;
        if (dto.PositionY.HasValue) table.PositionY = dto.PositionY;
        if (dto.IsActive.HasValue) table.IsActive = dto.IsActive.Value;

        table.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated table {TableId}", tableId);

        return MapToDto(table);
    }

    public async Task<TableSessionDto?> UpdateTableStatusAsync(Guid tableId, UpdateTableStatusDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating status for table {TableId} to {Status}", tableId, dto.Status);

        var table = await _context.Set<TableSession>()
            .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        if (table == null)
        {
            _logger.LogWarning("Table {TableId} not found", tableId);
            return null;
        }

        if (!Enum.TryParse<TableStatus>(dto.Status, out var newStatus))
        {
            throw new ArgumentException($"Invalid table status: {dto.Status}");
        }

        table.Status = newStatus;
        table.CurrentSaleSessionId = dto.SaleSessionId;
        table.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated table {TableId} status to {Status}", tableId, newStatus);

        return MapToDto(table);
    }

    public async Task<bool> DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Deleting table {TableId} for tenant {TenantId}", tableId, tenantId);

        var table = await _context.Set<TableSession>()
            .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        if (table == null)
        {
            _logger.LogWarning("Table {TableId} not found", tableId);
            return false;
        }

        table.IsDeleted = true;
        table.DeletedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted table {TableId}", tableId);

        return true;
    }

    public async Task<List<TableReservationDto>> GetReservationsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);

        _logger.LogInformation("Getting reservations for date {Date} and tenant {TenantId}", date.Date, tenantId);

        var reservations = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .Where(r => r.TenantId == tenantId && !r.IsDeleted &&
                       r.ReservationDateTime >= startDate && r.ReservationDateTime < endDate)
            .OrderBy(r => r.ReservationDateTime)
            .ToListAsync(cancellationToken);

        return reservations.Select(MapReservationToDto).ToList();
    }

    public async Task<TableReservationDto?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Getting reservation {ReservationId} for tenant {TenantId}", reservationId, tenantId);

        var reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        return reservation != null ? MapReservationToDto(reservation) : null;
    }

    public async Task<TableReservationDto> CreateReservationAsync(CreateTableReservationDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating reservation for table {TableId}", dto.TableId);

        var table = await _context.Set<TableSession>()
            .FirstOrDefaultAsync(t => t.Id == dto.TableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

        if (table == null)
        {
            throw new InvalidOperationException("Table not found.");
        }

        if (dto.NumberOfGuests > table.Capacity)
        {
            throw new InvalidOperationException($"Number of guests ({dto.NumberOfGuests}) exceeds table capacity ({table.Capacity}).");
        }

        var reservation = new TableReservation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TableId = dto.TableId,
            CustomerName = dto.CustomerName,
            PhoneNumber = dto.PhoneNumber,
            NumberOfGuests = dto.NumberOfGuests,
            ReservationDateTime = dto.ReservationDateTime,
            DurationMinutes = dto.DurationMinutes,
            SpecialRequests = dto.SpecialRequests,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _ = _context.Set<TableReservation>().Add(reservation);
        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created reservation {ReservationId}", reservation.Id);

        reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstAsync(r => r.Id == reservation.Id, cancellationToken);

        return MapReservationToDto(reservation);
    }

    public async Task<TableReservationDto?> UpdateReservationAsync(Guid reservationId, UpdateTableReservationDto dto, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating reservation {ReservationId}", reservationId);

        var reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return null;
        }

        if (dto.CustomerName != null) reservation.CustomerName = dto.CustomerName;
        if (dto.PhoneNumber != null) reservation.PhoneNumber = dto.PhoneNumber;
        if (dto.NumberOfGuests.HasValue)
        {
            if (reservation.Table != null && dto.NumberOfGuests.Value > reservation.Table.Capacity)
            {
                throw new InvalidOperationException($"Number of guests ({dto.NumberOfGuests.Value}) exceeds table capacity ({reservation.Table.Capacity}).");
            }
            reservation.NumberOfGuests = dto.NumberOfGuests.Value;
        }
        if (dto.ReservationDateTime.HasValue) reservation.ReservationDateTime = dto.ReservationDateTime.Value;
        if (dto.DurationMinutes.HasValue) reservation.DurationMinutes = dto.DurationMinutes;
        if (dto.SpecialRequests != null) reservation.SpecialRequests = dto.SpecialRequests;

        reservation.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated reservation {ReservationId}", reservationId);

        return MapReservationToDto(reservation);
    }

    public async Task<TableReservationDto?> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Confirming reservation {ReservationId}", reservationId);

        var reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return null;
        }

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ConfirmedAt = DateTime.UtcNow;
        reservation.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Confirmed reservation {ReservationId}", reservationId);

        return MapReservationToDto(reservation);
    }

    public async Task<TableReservationDto?> MarkArrivedAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Marking reservation {ReservationId} as arrived", reservationId);

        var reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return null;
        }

        reservation.Status = ReservationStatus.Arrived;
        reservation.ArrivedAt = DateTime.UtcNow;
        reservation.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked reservation {ReservationId} as arrived", reservationId);

        return MapReservationToDto(reservation);
    }

    public async Task<bool> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Cancelling reservation {ReservationId}", reservationId);

        var reservation = await _context.Set<TableReservation>()
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return false;
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled reservation {ReservationId}", reservationId);

        return true;
    }

    public async Task<TableReservationDto?> MarkNoShowAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Marking reservation {ReservationId} as no-show", reservationId);

        var reservation = await _context.Set<TableReservation>()
            .Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
            return null;
        }

        reservation.Status = ReservationStatus.NoShow;
        reservation.ModifiedAt = DateTime.UtcNow;

        _ = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked reservation {ReservationId} as no-show", reservationId);

        return MapReservationToDto(reservation);
    }

    private static TableSessionDto MapToDto(TableSession table)
    {
        return new TableSessionDto
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            TableName = table.TableName,
            Capacity = table.Capacity,
            Status = table.Status.ToString(),
            CurrentSaleSessionId = table.CurrentSaleSessionId,
            Area = table.Area,
            PositionX = table.PositionX,
            PositionY = table.PositionY,
            IsActive = table.IsActive,
            CreatedAt = table.CreatedAt,
            ModifiedAt = table.ModifiedAt
        };
    }

    private static TableReservationDto MapReservationToDto(TableReservation reservation)
    {
        return new TableReservationDto
        {
            Id = reservation.Id,
            TableId = reservation.TableId,
            TableNumber = reservation.Table?.TableNumber ?? "Unknown",
            CustomerName = reservation.CustomerName,
            PhoneNumber = reservation.PhoneNumber,
            NumberOfGuests = reservation.NumberOfGuests,
            ReservationDateTime = reservation.ReservationDateTime,
            DurationMinutes = reservation.DurationMinutes,
            Status = reservation.Status.ToString(),
            SpecialRequests = reservation.SpecialRequests,
            ConfirmedAt = reservation.ConfirmedAt,
            ArrivedAt = reservation.ArrivedAt,
            CreatedAt = reservation.CreatedAt
        };
    }
}
