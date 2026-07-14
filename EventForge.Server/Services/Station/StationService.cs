using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Station;
using StationQueueEntityStatus = EventForge.Server.Data.Entities.StationMonitor.StationOrderQueueStatus;

namespace EventForge.Server.Services.Station;

/// <summary>
/// Service implementation for managing stations and printers.
/// </summary>
public class StationService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    IHubContext<StationMonitorHub> hubContext,
    ILogger<StationService> logger) : IStationService
{

    #region Station Operations

    public async Task<PagedResult<StationDto>> GetStationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot retrieve stations without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var query = context.Stations
            .AsNoTracking()
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
            var printerCount = await context.Printers
                .AsNoTracking()
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

    public async Task<StationDto?> GetStationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot retrieve station without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var station = await context.Stations
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
        {
            logger.LogWarning("Station with ID {StationId} not found.", id);
            return null;
        }

        var printerCount = await context.Printers
            .AsNoTracking()
            .CountAsync(p => p.StationId == id && !p.IsDeleted && p.TenantId == tenantId.Value, cancellationToken);

        return MapToStationDto(station, printerCount);
    }

    public async Task<StationDto> CreateStationAsync(CreateStationDto createStationDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot create station without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var station = new EventForge.Server.Data.Entities.StationMonitor.Station
        {
            TenantId = tenantId.Value,
            Name = createStationDto.Name,
            Description = createStationDto.Description,
            Status = createStationDto.Status,
            Location = createStationDto.Location,
            SortOrder = createStationDto.SortOrder,
            Notes = createStationDto.Notes,
            StationType = createStationDto.StationType,
            AssignedPrinterId = createStationDto.AssignedPrinterId,
            PrintsReceiptCopy = createStationDto.PrintsReceiptCopy,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.Stations.Add(station);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(station, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Station {StationId} created by {User}.",
            station.Id, currentUser);

        return MapToStationDto(station, 0);
    }

    public async Task<StationDto?> UpdateStationAsync(Guid id, UpdateStationDto updateStationDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot update station without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var originalStation = await context.Stations
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStation is null)
        {
            logger.LogWarning("Station with ID {StationId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        var station = await context.Stations
            .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
        {
            logger.LogWarning("Station with ID {StationId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        station.Name = updateStationDto.Name;
        station.Description = updateStationDto.Description;
        station.Status = updateStationDto.Status;
        station.Location = updateStationDto.Location;
        station.SortOrder = updateStationDto.SortOrder;
        station.Notes = updateStationDto.Notes;
        station.StationType = updateStationDto.StationType;
        station.AssignedPrinterId = updateStationDto.AssignedPrinterId;
        station.PrintsReceiptCopy = updateStationDto.PrintsReceiptCopy;
        station.ModifiedAt = DateTime.UtcNow;
        station.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating Station {StationId}.", id);
            throw new InvalidOperationException("La stazione è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(station, "Update", currentUser, originalStation, cancellationToken);

        logger.LogInformation("Station {StationId} updated by {User}", id, currentUser);

        var printerCount = await context.Printers
            .AsNoTracking()
            .CountAsync(p => p.StationId == id && !p.IsDeleted && p.TenantId == tenantId.Value, cancellationToken);

        return MapToStationDto(station, printerCount);
    }

    public async Task<bool> DeleteStationAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot delete station without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var originalStation = await context.Stations
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalStation is null)
        {
            logger.LogWarning("Station with ID {StationId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        var station = await context.Stations
            .Where(s => s.Id == id && !s.IsDeleted && s.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
        {
            logger.LogWarning("Station with ID {StationId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        station.IsDeleted = true;
        station.DeletedAt = DateTime.UtcNow;
        station.DeletedBy = currentUser;
        station.ModifiedAt = DateTime.UtcNow;
        station.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting Station {StationId}.", id);
            throw new InvalidOperationException("La stazione è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(station, "Delete", currentUser, originalStation, cancellationToken);

        logger.LogInformation("Station {StationId} deleted by {User}", id, currentUser);

        return true;
    }

    #endregion

    #region Printer Operations

    public async Task<PagedResult<PrinterDto>> GetPrintersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot retrieve printers without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var query = context.Printers
            .AsNoTracking()
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

    public async Task<PrinterDto?> GetPrinterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot retrieve printer without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var printer = await context.Printers
            .AsNoTracking()
            .Include(p => p.Station)
            .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (printer is null)
        {
            logger.LogWarning("Printer with ID {PrinterId} not found.", id);
            return null;
        }

        return MapToPrinterDto(printer);
    }

    public async Task<IEnumerable<PrinterDto>> GetPrintersByStationAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot retrieve printers without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var printers = await context.Printers
            .AsNoTracking()
            .Include(p => p.Station)
            .Where(p => p.StationId == stationId && !p.IsDeleted && p.TenantId == tenantId.Value)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return printers.Select(MapToPrinterDto);
    }

    public async Task<PrinterDto> CreatePrinterAsync(CreatePrinterDto createPrinterDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot create printer without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        if (createPrinterDto.ConnectionType == PrinterConnectionType.UsbViaAgent
            && string.IsNullOrWhiteSpace(createPrinterDto.UsbDeviceId))
        {
            throw new ArgumentException(
                "UsbDeviceId is required when ConnectionType is UsbViaAgent.", nameof(createPrinterDto));
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
            IsFiscalPrinter = createPrinterDto.IsFiscalPrinter,
            ProtocolType = createPrinterDto.ProtocolType,
            Port = createPrinterDto.Port,
            BaudRate = createPrinterDto.BaudRate,
            SerialPortName = createPrinterDto.SerialPortName,
            ConnectionType = createPrinterDto.ConnectionType,
            AgentId = createPrinterDto.AgentId,
            UsbDeviceId = createPrinterDto.UsbDeviceId,
            Category = createPrinterDto.Category,
            IsThermal = createPrinterDto.IsThermal,
            PrinterWidth = createPrinterDto.PrinterWidth,
            PaperWidth = createPrinterDto.PaperWidth,
            PrintLanguage = createPrinterDto.PrintLanguage,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.Printers.Add(printer);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(printer, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Printer {PrinterId} created by {User}.",
            printer.Id, currentUser);

        // Reload with includes
        var createdPrinter = await context.Printers
            .Include(p => p.Station)
            .FirstAsync(p => p.Id == printer.Id, cancellationToken);

        return MapToPrinterDto(createdPrinter);
    }

    public async Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updatePrinterDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot update printer without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        if (updatePrinterDto.ConnectionType == PrinterConnectionType.UsbViaAgent
            && string.IsNullOrWhiteSpace(updatePrinterDto.UsbDeviceId))
        {
            throw new ArgumentException(
                "UsbDeviceId is required when ConnectionType is UsbViaAgent.", nameof(updatePrinterDto));
        }

        var originalPrinter = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalPrinter is null)
        {
            logger.LogWarning("Printer with ID {PrinterId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        var printer = await context.Printers
            .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (printer is null)
        {
            logger.LogWarning("Printer with ID {PrinterId} not found for update by user {User}.", id, currentUser);
            return null;
        }

        printer.Name = updatePrinterDto.Name;
        printer.Type = updatePrinterDto.Type;
        printer.Model = updatePrinterDto.Model;
        printer.Location = updatePrinterDto.Location;
        printer.Address = updatePrinterDto.Address;
        printer.StationId = updatePrinterDto.StationId;
        printer.IsFiscalPrinter = updatePrinterDto.IsFiscalPrinter;
        printer.ProtocolType = updatePrinterDto.ProtocolType;
        printer.Port = updatePrinterDto.Port;
        printer.BaudRate = updatePrinterDto.BaudRate;
        printer.SerialPortName = updatePrinterDto.SerialPortName;
        printer.ConnectionType = updatePrinterDto.ConnectionType;
        printer.AgentId = updatePrinterDto.AgentId;
        printer.UsbDeviceId = updatePrinterDto.UsbDeviceId;
        printer.Category = updatePrinterDto.Category;
        printer.IsThermal = updatePrinterDto.IsThermal;
        printer.PrinterWidth = updatePrinterDto.PrinterWidth;
        printer.PaperWidth = updatePrinterDto.PaperWidth;
        printer.PrintLanguage = updatePrinterDto.PrintLanguage;
        printer.ModifiedAt = DateTime.UtcNow;
        printer.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(printer, "Update", currentUser, originalPrinter, cancellationToken);

        logger.LogInformation("Printer {PrinterId} updated by {User}", id, currentUser);

        // Reload with includes
        var updatedPrinter = await context.Printers
            .Include(p => p.Station)
            .FirstAsync(p => p.Id == id, cancellationToken);

        return MapToPrinterDto(updatedPrinter);
    }

    public async Task<bool> DeletePrinterAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot delete printer without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        var originalPrinter = await context.Printers
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalPrinter is null)
        {
            logger.LogWarning("Printer with ID {PrinterId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        var printer = await context.Printers
            .Where(p => p.Id == id && !p.IsDeleted && p.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (printer is null)
        {
            logger.LogWarning("Printer with ID {PrinterId} not found for deletion by user {User}.", id, currentUser);
            return false;
        }

        printer.IsDeleted = true;
        printer.DeletedAt = DateTime.UtcNow;
        printer.DeletedBy = currentUser;
        printer.ModifiedAt = DateTime.UtcNow;
        printer.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(printer, "Delete", currentUser, originalPrinter, cancellationToken);

        logger.LogInformation("Printer {PrinterId} deleted by {User}", id, currentUser);

        return true;
    }

    #endregion

    #region Order Queue Operations

    public async Task<IEnumerable<StationOrderQueueItemDto>> GetQueueItemsByStationAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        var items = await context.StationOrderQueueItems
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(q => q.StationId == stationId)
            .Include(q => q.Station)
            .Include(q => q.DocumentHeader)
            .Include(q => q.TeamMember)
            .Include(q => q.Product)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapToQueueItemDto).ToList();
    }

    public async Task<IEnumerable<StationOrderQueueItemDto>> GetActiveQueueItemsAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();
        var activeStatuses = new[]
        {
            StationQueueEntityStatus.Waiting,
            StationQueueEntityStatus.Accepted,
            StationQueueEntityStatus.InPreparation
        };

        var items = await context.StationOrderQueueItems
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(q => q.StationId == stationId && activeStatuses.Contains(q.Status))
            .Include(q => q.Station)
            .Include(q => q.DocumentHeader)
            .Include(q => q.TeamMember)
            .Include(q => q.Product)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(MapToQueueItemDto).ToList();
    }

    public async Task<StationOrderQueueItemDto> CreateQueueItemAsync(CreateStationOrderQueueItemDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        var sortOrder = dto.SortOrder;
        if (sortOrder <= 0)
        {
            sortOrder = (await context.StationOrderQueueItems
                .AsNoTracking()
                .WhereActiveTenant(tenantId)
                .Where(q => q.StationId == dto.StationId)
                .Select(q => (int?)q.SortOrder)
                .MaxAsync(cancellationToken) ?? -1) + 1;
        }

        var queueItem = new StationOrderQueueItem
        {
            TenantId = tenantId,
            StationId = dto.StationId,
            DocumentHeaderId = dto.DocumentHeaderId,
            DocumentRowId = dto.DocumentRowId,
            TeamMemberId = dto.TeamMemberId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Status = StationQueueEntityStatus.Waiting,
            SortOrder = sortOrder,
            AssignedAt = DateTime.UtcNow,
            Notes = dto.Notes,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.StationOrderQueueItems.Add(queueItem);
        _ = await context.SaveChangesAsync(cancellationToken);
        _ = await auditLogService.TrackEntityChangesAsync(queueItem, "Insert", currentUser, null, cancellationToken);

        var createdQueueItem = await GetQueueItemWithRelationsAsync(queueItem.Id, tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Unable to reload the created station queue item.");

        var itemDto = MapToQueueItemDto(createdQueueItem);
        await hubContext.Clients.Group(GetStationGroupName(queueItem.StationId))
            .SendAsync("QueueItemAdded", itemDto, cancellationToken);

        logger.LogInformation("Station queue item {QueueItemId} created for station {StationId} by {User}.",
            queueItem.Id, queueItem.StationId, currentUser);

        return itemDto;
    }

    public async Task<StationOrderQueueItemDto?> UpdateQueueItemStatusAsync(Guid id, Prym.DTOs.Station.StationOrderQueueStatus status, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalQueueItem = await context.StationOrderQueueItems
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (originalQueueItem is null)
        {
            logger.LogWarning("Queue item {QueueItemId} not found for update by {User}.", id, currentUser);
            return null;
        }

        var queueItem = await context.StationOrderQueueItems
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (queueItem is null)
        {
            logger.LogWarning("Queue item {QueueItemId} not found for update by {User}.", id, currentUser);
            return null;
        }

        ApplyQueueStatus(queueItem, status, currentUser);

        _ = await context.SaveChangesAsync(cancellationToken);
        _ = await auditLogService.TrackEntityChangesAsync(queueItem, "Update", currentUser, originalQueueItem, cancellationToken);

        var updatedQueueItem = await GetQueueItemWithRelationsAsync(queueItem.Id, tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Unable to reload the updated station queue item.");

        var itemDto = MapToQueueItemDto(updatedQueueItem);
        await hubContext.Clients.Group(GetStationGroupName(queueItem.StationId))
            .SendAsync("QueueItemStatusChanged", itemDto, cancellationToken);

        logger.LogInformation("Station queue item {QueueItemId} status updated to {Status} by {User}.",
            queueItem.Id, status, currentUser);

        return itemDto;
    }

    public async Task<bool> DeleteQueueItemAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalQueueItem = await context.StationOrderQueueItems
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (originalQueueItem is null)
        {
            logger.LogWarning("Queue item {QueueItemId} not found for deletion by {User}.", id, currentUser);
            return false;
        }

        var queueItem = await context.StationOrderQueueItems
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (queueItem is null)
        {
            logger.LogWarning("Queue item {QueueItemId} not found for deletion by {User}.", id, currentUser);
            return false;
        }

        queueItem.IsDeleted = true;
        queueItem.DeletedAt = DateTime.UtcNow;
        queueItem.DeletedBy = currentUser;
        queueItem.ModifiedAt = DateTime.UtcNow;
        queueItem.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);
        _ = await auditLogService.TrackEntityChangesAsync(queueItem, "Delete", currentUser, originalQueueItem, cancellationToken);

        await hubContext.Clients.Group(GetStationGroupName(queueItem.StationId))
            .SendAsync("QueueItemRemoved", queueItem.Id, cancellationToken);

        logger.LogInformation("Station queue item {QueueItemId} deleted by {User}.", queueItem.Id, currentUser);

        return true;
    }

    #endregion

    #region Helper Methods

    public async Task<bool> StationExistsAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            return false;
        }

        return await context.Stations
            .AsNoTracking()
            .AnyAsync(s => s.Id == stationId && !s.IsDeleted && s.TenantId == tenantId.Value, cancellationToken);
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            logger.LogWarning("Cannot execute station queue operation without a tenant context.");
            throw new InvalidOperationException("Tenant context is required.");
        }

        return tenantId.Value;
    }

    private async Task<StationOrderQueueItem?> GetQueueItemWithRelationsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken)
    {
        return await context.StationOrderQueueItems
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(q => q.Id == id)
            .Include(q => q.Station)
            .Include(q => q.DocumentHeader)
            .Include(q => q.TeamMember)
            .Include(q => q.Product)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ApplyQueueStatus(StationOrderQueueItem queueItem, Prym.DTOs.Station.StationOrderQueueStatus status, string currentUser)
    {
        var now = DateTime.UtcNow;
        queueItem.Status = MapToEntityStatus(status);
        queueItem.ModifiedAt = now;
        queueItem.ModifiedBy = currentUser;

        switch (status)
        {
            case Prym.DTOs.Station.StationOrderQueueStatus.Waiting:
                queueItem.AssignedAt ??= now;
                queueItem.StartedAt = null;
                queueItem.CompletedAt = null;
                break;

            case Prym.DTOs.Station.StationOrderQueueStatus.InProgress:
                queueItem.AssignedAt ??= now;
                queueItem.StartedAt ??= now;
                queueItem.CompletedAt = null;
                break;

            case Prym.DTOs.Station.StationOrderQueueStatus.Completed:
                queueItem.AssignedAt ??= now;
                queueItem.StartedAt ??= now;
                queueItem.CompletedAt = now;
                break;

            case Prym.DTOs.Station.StationOrderQueueStatus.Cancelled:
                queueItem.CompletedAt ??= now;
                break;
        }
    }

    private static StationOrderQueueItemDto MapToQueueItemDto(StationOrderQueueItem queueItem)
    {
        return new StationOrderQueueItemDto
        {
            Id = queueItem.Id,
            StationId = queueItem.StationId,
            StationName = queueItem.Station?.Name ?? string.Empty,
            DocumentHeaderId = queueItem.DocumentHeaderId,
            DocumentNumber = queueItem.DocumentHeader?.Number,
            DocumentRowId = queueItem.DocumentRowId,
            TeamMemberId = queueItem.TeamMemberId,
            TeamMemberName = queueItem.TeamMember is null
                ? null
                : $"{queueItem.TeamMember.FirstName} {queueItem.TeamMember.LastName}".Trim(),
            ProductId = queueItem.ProductId,
            ProductName = queueItem.Product?.Name ?? string.Empty,
            Quantity = queueItem.Quantity,
            Status = MapToDtoStatus(queueItem.Status),
            SortOrder = queueItem.SortOrder,
            AssignedAt = queueItem.AssignedAt,
            StartedAt = queueItem.StartedAt,
            CompletedAt = queueItem.CompletedAt,
            Notes = queueItem.Notes,
            CreatedAt = queueItem.CreatedAt
        };
    }

    private static StationQueueEntityStatus MapToEntityStatus(Prym.DTOs.Station.StationOrderQueueStatus status) =>
        status switch
        {
            Prym.DTOs.Station.StationOrderQueueStatus.Waiting => StationQueueEntityStatus.Waiting,
            Prym.DTOs.Station.StationOrderQueueStatus.InProgress => StationQueueEntityStatus.InPreparation,
            Prym.DTOs.Station.StationOrderQueueStatus.Completed => StationQueueEntityStatus.Ready,
            Prym.DTOs.Station.StationOrderQueueStatus.Cancelled => StationQueueEntityStatus.Cancelled,
            _ => StationQueueEntityStatus.Waiting
        };

    private static Prym.DTOs.Station.StationOrderQueueStatus MapToDtoStatus(StationQueueEntityStatus status) =>
        status switch
        {
            StationQueueEntityStatus.Waiting => Prym.DTOs.Station.StationOrderQueueStatus.Waiting,
            StationQueueEntityStatus.Accepted => Prym.DTOs.Station.StationOrderQueueStatus.InProgress,
            StationQueueEntityStatus.InPreparation => Prym.DTOs.Station.StationOrderQueueStatus.InProgress,
            StationQueueEntityStatus.Ready => Prym.DTOs.Station.StationOrderQueueStatus.Completed,
            StationQueueEntityStatus.Delivered => Prym.DTOs.Station.StationOrderQueueStatus.Completed,
            StationQueueEntityStatus.Cancelled => Prym.DTOs.Station.StationOrderQueueStatus.Cancelled,
            _ => Prym.DTOs.Station.StationOrderQueueStatus.Waiting
        };

    private static string GetStationGroupName(Guid stationId) => $"station-{stationId}";

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
            StationType = station.StationType,
            AssignedPrinterId = station.AssignedPrinterId,
            PrintsReceiptCopy = station.PrintsReceiptCopy,
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
            ModifiedBy = printer.ModifiedBy,
            IsFiscalPrinter = printer.IsFiscalPrinter,
            ProtocolType = printer.ProtocolType,
            ConnectionString = printer.ConnectionString,
            Port = printer.Port,
            BaudRate = printer.BaudRate,
            SerialPortName = printer.SerialPortName,
            Status = printer.Status,
            ConnectionType = printer.ConnectionType,
            AgentId = printer.AgentId,
            UsbDeviceId = printer.UsbDeviceId,
            Category = printer.Category,
            IsThermal = printer.IsThermal,
            PrinterWidth = printer.PrinterWidth,
            PaperWidth = printer.PaperWidth,
            PrintLanguage = printer.PrintLanguage
        };
    }

    #endregion

}
