using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

public class TableManagementService(
    EventForgeDbContext context,
    ILogger<TableManagementService> logger,
    ITenantContext tenantContext) : ITableManagementService
{
    private const int MinTableDimension = 40;
    private const int MaxTableDimension = 300;

    private Guid GetTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for table management operations.");
        }
        return tenantId.Value;
    }

    public async Task<PagedResult<TableSessionDto>> GetTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var query = context.Set<TableSession>()
                .AsNoTracking()
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
        catch
        {
            throw;
        }
    }

    public async Task<PagedResult<TableSessionDto>> GetTablesByZoneAsync(string zone, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var query = context.Set<TableSession>()
                .AsNoTracking()
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
        catch
        {
            throw;
        }
    }

    public async Task<PagedResult<TableSessionDto>> GetAvailableTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var query = context.Set<TableSession>()
                .AsNoTracking()
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
        catch
        {
            throw;
        }
    }

    public async Task<TableSessionDto?> GetTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var table = await context.Set<TableSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

            return table is not null ? MapToDto(table) : null;
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<TableSessionDto>> GetAllAvailableTablesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var tables = await context.Set<TableSession>()
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && !t.IsDeleted && t.IsActive && t.Status == TableStatus.Available)
                .OrderBy(t => t.TableNumber)
                .ToListAsync(cancellationToken);

            return tables.Select(MapToDto).ToList();
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableSessionDto> CreateTableAsync(CreateTableSessionDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var exists = await context.Set<TableSession>()
                .AsNoTracking()
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
                Shape = MapShape(dto.Shape),
                Width = NormalizeTableDimension(dto.Width),
                Height = NormalizeTableDimension(dto.Height),
                Area = dto.Area,
                PositionX = dto.PositionX,
                PositionY = dto.PositionY,
                Status = TableStatus.Available,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _ = context.Set<TableSession>().Add(table);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created table {TableId} with number {TableNumber}", table.Id, table.TableNumber);

            return MapToDto(table);
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableSessionDto?> UpdateTableAsync(Guid tableId, UpdateTableSessionDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var table = await context.Set<TableSession>()
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

            if (table is null)
            {
                logger.LogWarning("Table {TableId} not found", tableId);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dto.TableNumber) && dto.TableNumber != table.TableNumber)
            {
                var exists = await context.Set<TableSession>()
                    .AsNoTracking()
                    .AnyAsync(t => t.TenantId == tenantId && t.TableNumber == dto.TableNumber && t.Id != tableId && !t.IsDeleted, cancellationToken);

                if (exists)
                {
                    throw new InvalidOperationException($"Table with number '{dto.TableNumber}' already exists.");
                }

                table.TableNumber = dto.TableNumber;
            }

            if (dto.TableName is not null) table.TableName = dto.TableName;
            if (dto.Capacity.HasValue) table.Capacity = dto.Capacity.Value;
            if (dto.Shape.HasValue) table.Shape = MapShape(dto.Shape.Value);
            if (dto.Width.HasValue) table.Width = NormalizeTableDimension(dto.Width.Value);
            if (dto.Height.HasValue) table.Height = NormalizeTableDimension(dto.Height.Value);
            if (dto.Area is not null) table.Area = dto.Area;
            if (dto.PositionX.HasValue) table.PositionX = dto.PositionX;
            if (dto.PositionY.HasValue) table.PositionY = dto.PositionY;
            if (dto.IsActive.HasValue) table.IsActive = dto.IsActive.Value;

            table.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated table {TableId}", tableId);

            return MapToDto(table);
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableSessionDto?> UpdateTableStatusAsync(Guid tableId, UpdateTableStatusDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var table = await context.Set<TableSession>()
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

            if (table is null)
            {
                logger.LogWarning("Table {TableId} not found", tableId);
                return null;
            }

            if (!Enum.TryParse<TableStatus>(dto.Status, out var newStatus))
            {
                throw new ArgumentException($"Invalid table status: {dto.Status}");
            }

            table.Status = newStatus;
            table.CurrentSaleSessionId = dto.SaleSessionId;
            table.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated table {TableId} status to {Status}", tableId, newStatus);

            return MapToDto(table);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var table = await context.Set<TableSession>()
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

            if (table is null)
            {
                logger.LogWarning("Table {TableId} not found", tableId);
                return false;
            }

            table.IsDeleted = true;
            table.DeletedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Deleted table {TableId}", tableId);

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<TableReservationDto>> GetReservationsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);


            var reservations = await context.Set<TableReservation>()
                .AsNoTracking()
                .Include(r => r.Table)
                .Where(r => r.TenantId == tenantId && !r.IsDeleted &&
                           r.ReservationDateTime >= startDate && r.ReservationDateTime < endDate)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync(cancellationToken);

            return reservations.Select(MapReservationToDto).ToList();
        }
        catch
        {
            throw;
        }
    }

    public async Task<DailyFlowDto> GetDailyFlowAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            var now = DateTime.UtcNow;
            var nextReservationLimit = now.AddMinutes(60);

            var tables = await context.Set<TableSession>()
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .OrderBy(t => t.Area)
                .ThenBy(t => t.TableNumber)
                .ToListAsync(cancellationToken);

            var reservations = await context.Set<TableReservation>()
                .AsNoTracking()
                .Include(r => r.Table)
                .Where(r => r.TenantId == tenantId && !r.IsDeleted &&
                            r.ReservationDateTime >= startDate && r.ReservationDateTime < endDate)
                .OrderBy(r => r.ReservationDateTime)
                .ToListAsync(cancellationToken);

            var tableIds = tables.Select(t => t.Id).ToList();
            var activeSaleSessions = await context.Set<SaleSession>()
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && !s.IsDeleted &&
                            s.TableId.HasValue &&
                            tableIds.Contains(s.TableId.Value) &&
                            s.Status != SaleSessionStatus.Closed &&
                            s.Status != SaleSessionStatus.Cancelled)
                .OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt)
                .ToListAsync(cancellationToken);

            var activeSessionById = activeSaleSessions.ToDictionary(s => s.Id);
            var activeSessionByTableId = activeSaleSessions
                .GroupBy(s => s.TableId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var reservationsByTableId = reservations
                .GroupBy(r => r.TableId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return new DailyFlowDto
            {
                TodayReservations = reservations.Select(MapReservationToDto).ToList(),
                Tables = tables.Select(table =>
                {
                    SaleSession? openSession = null;

                    if (table.CurrentSaleSessionId.HasValue &&
                        activeSessionById.TryGetValue(table.CurrentSaleSessionId.Value, out var currentSession))
                    {
                        openSession = currentSession;
                    }
                    else if (activeSessionByTableId.TryGetValue(table.Id, out var mappedSession))
                    {
                        openSession = mappedSession;
                    }

                    TableReservation? nextReservation = null;
                    if (reservationsByTableId.TryGetValue(table.Id, out var tableReservations))
                    {
                        nextReservation = tableReservations
                            .FirstOrDefault(r => r.Status == ReservationStatus.Confirmed &&
                                                 r.ReservationDateTime >= now &&
                                                 r.ReservationDateTime <= nextReservationLimit);
                    }

                    return new TableDailyStatusDto
                    {
                        TableId = table.Id,
                        TableNumber = table.TableNumber,
                        TableName = table.TableName,
                        Status = table.Status.ToString(),
                        HasOpenBill = openSession is not null,
                        OpenSaleSessionId = openSession?.Id,
                        CurrentPartialAmount = openSession?.FinalTotal,
                        NextReservationId = nextReservation?.Id,
                        NextReservationTime = nextReservation?.ReservationDateTime,
                        NextReservationCustomerName = nextReservation?.CustomerName,
                        MinutesUntilNextReservation = nextReservation is null
                            ? null
                            : Math.Max(0, (int)Math.Ceiling((nextReservation.ReservationDateTime - now).TotalMinutes))
                    };
                }).ToList()
            };
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .AsNoTracking()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            return reservation is not null ? MapReservationToDto(reservation) : null;
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto> CreateReservationAsync(CreateTableReservationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var table = await context.Set<TableSession>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == dto.TableId && t.TenantId == tenantId && !t.IsDeleted, cancellationToken);

            if (table is null)
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

            _ = context.Set<TableReservation>().Add(reservation);
            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created reservation {ReservationId}", reservation.Id);

            reservation = await context.Set<TableReservation>()
                .Include(r => r.Table)
                .FirstAsync(r => r.Id == reservation.Id, cancellationToken);

            return MapReservationToDto(reservation);
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto?> UpdateReservationAsync(Guid reservationId, UpdateTableReservationDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                return null;
            }

            if (dto.CustomerName is not null) reservation.CustomerName = dto.CustomerName;
            if (dto.PhoneNumber is not null) reservation.PhoneNumber = dto.PhoneNumber;
            if (dto.NumberOfGuests.HasValue)
            {
                if (reservation.Table is not null && dto.NumberOfGuests.Value > reservation.Table.Capacity)
                {
                    throw new InvalidOperationException($"Number of guests ({dto.NumberOfGuests.Value}) exceeds table capacity ({reservation.Table.Capacity}).");
                }
                reservation.NumberOfGuests = dto.NumberOfGuests.Value;
            }
            if (dto.ReservationDateTime.HasValue) reservation.ReservationDateTime = dto.ReservationDateTime.Value;
            if (dto.DurationMinutes.HasValue) reservation.DurationMinutes = dto.DurationMinutes;
            if (dto.SpecialRequests is not null) reservation.SpecialRequests = dto.SpecialRequests;

            reservation.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Updated reservation {ReservationId}", reservationId);

            return MapReservationToDto(reservation);
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto?> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                return null;
            }

            reservation.Status = ReservationStatus.Confirmed;
            reservation.ConfirmedAt = DateTime.UtcNow;
            reservation.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Confirmed reservation {ReservationId}", reservationId);

            return MapReservationToDto(reservation);
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto?> MarkArrivedAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                return null;
            }

            reservation.Status = ReservationStatus.Arrived;
            reservation.ArrivedAt = DateTime.UtcNow;
            reservation.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Marked reservation {ReservationId} as arrived", reservationId);

            return MapReservationToDto(reservation);
        }
        catch
        {
            throw;
        }
    }

    public async Task<bool> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                return false;
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Cancelled reservation {ReservationId}", reservationId);

            return true;
        }
        catch
        {
            throw;
        }
    }

    public async Task<TableReservationDto?> MarkNoShowAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = GetTenantId();

            var reservation = await context.Set<TableReservation>()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.TenantId == tenantId && !r.IsDeleted, cancellationToken);

            if (reservation is null)
            {
                logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                return null;
            }

            reservation.Status = ReservationStatus.NoShow;
            reservation.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Marked reservation {ReservationId} as no-show", reservationId);

            return MapReservationToDto(reservation);
        }
        catch
        {
            throw;
        }
    }

    private static TableSessionDto MapToDto(TableSession table)
    {
        return new TableSessionDto
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            TableName = table.TableName,
            Capacity = table.Capacity,
            Shape = MapShape(table.Shape),
            Width = table.Width,
            Height = table.Height,
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

    private static TableShapeDto MapShape(TableShape shape) => shape switch
    {
        TableShape.Rectangle => TableShapeDto.Rectangle,
        TableShape.Circle => TableShapeDto.Circle,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported table shape.")
    };

    private static TableShape MapShape(TableShapeDto shape) => shape switch
    {
        TableShapeDto.Rectangle => TableShape.Rectangle,
        TableShapeDto.Circle => TableShape.Circle,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unsupported table shape.")
    };

    private static int NormalizeTableDimension(int value) => Math.Clamp(value, MinTableDimension, MaxTableDimension);

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
