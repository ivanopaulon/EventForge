using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing storage locations within warehouses.
/// </summary>
public class StorageLocationService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<StorageLocationService> logger) : IStorageLocationService
{

    public async Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(PaginationParameters pagination, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting storage locations: page={Page}, pageSize={PageSize}, warehouseId={WarehouseId}", pagination.Page, pagination.PageSize, warehouseId);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var query = context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => !sl.IsDeleted && sl.TenantId == currentTenantId.Value)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(sl => sl.WarehouseId == warehouseId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(sl => sl.Warehouse != null ? sl.Warehouse.Name : string.Empty)
            .ThenBy(sl => sl.Zone)
            .ThenBy(sl => sl.Code)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StorageLocationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting storage location by ID: {Id}", id);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var location = await context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.Id == id && sl.TenantId == currentTenantId.Value)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return location;
    }

    public async Task<IEnumerable<StorageLocationDto>> GetLocationsByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting storage locations for warehouse: {WarehouseId}", warehouseId);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var locations = await context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.WarehouseId == warehouseId && sl.TenantId == currentTenantId.Value)
            .OrderBy(sl => sl.Zone)
            .ThenBy(sl => sl.Row)
            .ThenBy(sl => sl.Column)
            .ThenBy(sl => sl.Level)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return locations;
    }

    public async Task<IEnumerable<StorageLocationDto>> GetAvailableLocationsAsync(Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting available storage locations for warehouse: {WarehouseId}", warehouseId);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var query = context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.TenantId == currentTenantId.Value && sl.IsActive &&
                        (sl.Capacity == null || sl.Occupancy == null || sl.Occupancy < sl.Capacity));

        if (warehouseId.HasValue)
        {
            query = query.Where(sl => sl.WarehouseId == warehouseId.Value);
        }

        var locations = await query
            .OrderBy(sl => sl.Zone)
            .ThenBy(sl => sl.Row)
            .ThenBy(sl => sl.Column)
            .ThenBy(sl => sl.Level)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return locations;
    }

    public async Task<StorageLocationDto> CreateStorageLocationAsync(CreateStorageLocationDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating storage location: {Code}", createDto.Code);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var warehouseExists = await context.StorageFacilities
            .AsNoTracking()
            .AnyAsync(sf => sf.Id == createDto.WarehouseId && sf.TenantId == currentTenantId.Value, cancellationToken);

        if (!warehouseExists)
        {
            logger.LogWarning("Warehouse with ID {WarehouseId} not found.", createDto.WarehouseId);
            throw new ArgumentException($"Warehouse with ID {createDto.WarehouseId} not found.");
        }

        var codeExists = await context.StorageLocations
            .AsNoTracking()
            .AnyAsync(sl => sl.Code == createDto.Code && sl.WarehouseId == createDto.WarehouseId && sl.TenantId == currentTenantId.Value, cancellationToken);

        if (codeExists)
        {
            logger.LogWarning("Storage location with code '{Code}' already exists in warehouse {WarehouseId}.", createDto.Code, createDto.WarehouseId);
            throw new ArgumentException($"Storage location with code '{createDto.Code}' already exists in this warehouse.");
        }

        if (createDto.Capacity.HasValue && createDto.Occupancy.HasValue && createDto.Occupancy > createDto.Capacity)
        {
            logger.LogWarning("Occupancy {Occupancy} cannot exceed capacity {Capacity}.", createDto.Occupancy, createDto.Capacity);
            throw new ArgumentException("Occupancy cannot exceed capacity.");
        }

        var location = new StorageLocation
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenantId.Value,
            Code = createDto.Code,
            Description = createDto.Description,
            WarehouseId = createDto.WarehouseId,
            Capacity = createDto.Capacity,
            Occupancy = createDto.Occupancy ?? 0,
            IsRefrigerated = createDto.IsRefrigerated,
            Notes = createDto.Notes,
            Zone = createDto.Zone,
            Floor = createDto.Floor,
            Row = createDto.Row,
            Column = createDto.Column,
            Level = createDto.Level,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser
        };

        _ = context.StorageLocations.Add(location);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(location, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Created storage location: {Id} - {Code}", location.Id, location.Code);

        return await GetStorageLocationByIdAsync(location.Id, cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve created storage location.");
    }

    public async Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating storage location: {Id}", id);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for this operation.");

        var originalLocation = await context.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);

        var location = await context.StorageLocations
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);
        if (location is null)
        {
            logger.LogWarning("Storage location {Id} not found for update.", id);
            return null;
        }

        if (updateDto.WarehouseId.HasValue && updateDto.WarehouseId != location.WarehouseId)
        {
            var warehouseExists = await context.StorageFacilities
                .AsNoTracking()
                .AnyAsync(sf => sf.Id == updateDto.WarehouseId.Value, cancellationToken);

            if (!warehouseExists)
            {
                logger.LogWarning("Warehouse with ID {WarehouseId} not found.", updateDto.WarehouseId);
                throw new ArgumentException($"Warehouse with ID {updateDto.WarehouseId} not found.");
            }
        }

        if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != location.Code)
        {
            var warehouseIdToCheck = updateDto.WarehouseId ?? location.WarehouseId;
            var codeExists = await context.StorageLocations
                .AsNoTracking()
                .AnyAsync(sl => sl.Code == updateDto.Code && sl.WarehouseId == warehouseIdToCheck && sl.Id != id, cancellationToken);

            if (codeExists)
            {
                logger.LogWarning("Storage location with code '{Code}' already exists in warehouse {WarehouseId}.", updateDto.Code, warehouseIdToCheck);
                throw new ArgumentException($"Storage location with code '{updateDto.Code}' already exists in this warehouse.");
            }
        }

        var newCapacity = updateDto.Capacity ?? location.Capacity;
        var newOccupancy = updateDto.Occupancy ?? location.Occupancy;

        if (newCapacity.HasValue && newOccupancy.HasValue && newOccupancy > newCapacity)
        {
            logger.LogWarning("Occupancy {Occupancy} cannot exceed capacity {Capacity}.", newOccupancy, newCapacity);
            throw new ArgumentException("Occupancy cannot exceed capacity.");
        }

        if (!string.IsNullOrEmpty(updateDto.Code))
            location.Code = updateDto.Code;
        if (updateDto.Description is not null)
            location.Description = updateDto.Description;
        if (updateDto.WarehouseId.HasValue)
            location.WarehouseId = updateDto.WarehouseId.Value;
        if (updateDto.Capacity.HasValue)
            location.Capacity = updateDto.Capacity;
        if (updateDto.Occupancy.HasValue)
            location.Occupancy = updateDto.Occupancy;
        if (updateDto.IsRefrigerated.HasValue)
            location.IsRefrigerated = updateDto.IsRefrigerated.Value;
        if (updateDto.Notes is not null)
            location.Notes = updateDto.Notes;
        if (updateDto.Zone is not null)
            location.Zone = updateDto.Zone;
        if (updateDto.Floor is not null)
            location.Floor = updateDto.Floor;
        if (updateDto.Row is not null)
            location.Row = updateDto.Row;
        if (updateDto.Column is not null)
            location.Column = updateDto.Column;
        if (updateDto.Level is not null)
            location.Level = updateDto.Level;
        if (updateDto.IsActive.HasValue)
            location.IsActive = updateDto.IsActive.Value;

        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating StorageLocation {StorageLocationId}.", id);
            throw new InvalidOperationException("La posizione di magazzino è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(location, "Update", currentUser, originalLocation, cancellationToken);

        logger.LogInformation("Updated storage location: {Id} - {Code}", location.Id, location.Code);

        return await GetStorageLocationByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteStorageLocationAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting storage location: {Id}", id);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for this operation.");

        var originalLocation = await context.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);

        var location = await context.StorageLocations
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);
        if (location is null)
        {
            logger.LogWarning("Storage location {Id} not found for delete.", id);
            return false;
        }

        if (location.Occupancy > 0)
        {
            logger.LogWarning("Cannot delete storage location {Id} because it contains inventory.", id);
            throw new InvalidOperationException("Cannot delete storage location that contains inventory. Move inventory first.");
        }

        // RIMOSSO controllo su IHasRowVersion e rowVersion

        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.DeletedBy = currentUser;
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting StorageLocation {StorageLocationId}.", id);
            throw new InvalidOperationException("La posizione di magazzino è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(location, "Delete", currentUser, originalLocation, cancellationToken);

        logger.LogInformation("Deleted storage location: {Id}", id);
        return true;
    }

    public async Task<StorageLocationDto?> UpdateOccupancyAsync(Guid id, int newOccupancy, string currentUser, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating occupancy for storage location: {Id} to {Occupancy}", id, newOccupancy);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for this operation.");

        var originalLocation = await context.StorageLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);

        var location = await context.StorageLocations
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == currentTenantId, cancellationToken);
        if (location is null)
        {
            logger.LogWarning("Storage location {Id} not found for occupancy update.", id);
            return null;
        }

        if (newOccupancy < 0)
        {
            logger.LogWarning("Occupancy cannot be negative for storage location {Id}.", id);
            throw new ArgumentException("Occupancy cannot be negative.");
        }

        if (location.Capacity.HasValue && newOccupancy > location.Capacity)
        {
            logger.LogWarning("Occupancy {Occupancy} cannot exceed capacity {Capacity} for storage location {Id}.", newOccupancy, location.Capacity, id);
            throw new ArgumentException($"Occupancy ({newOccupancy}) cannot exceed capacity ({location.Capacity}).");
        }

        location.Occupancy = newOccupancy;
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(location, "UpdateOccupancy", currentUser, originalLocation, cancellationToken);

        logger.LogInformation("Updated occupancy for storage location: {Id} to {Occupancy}", id, newOccupancy);

        return await GetStorageLocationByIdAsync(id, cancellationToken);
    }

    public async Task<PagedResult<StorageLocationDto>> GetLocationsByWarehouseAsync(
        Guid warehouseId,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting storage locations for warehouse {WarehouseId} with pagination: page={Page}, pageSize={PageSize}",
            warehouseId, pagination.Page, pagination.PageSize);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var query = context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => !sl.IsDeleted && sl.WarehouseId == warehouseId && sl.TenantId == currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(sl => sl.Zone)
            .ThenBy(sl => sl.Code)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StorageLocationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<StorageLocationDto>> GetLocationsByZoneAsync(
        string zone,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting storage locations for zone {Zone} with pagination: page={Page}, pageSize={PageSize}",
            zone, pagination.Page, pagination.PageSize);

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage location operations.");
        }

        var query = context.StorageLocations
            .AsNoTracking()
            .Include(sl => sl.Warehouse)
            .Where(sl => !sl.IsDeleted && sl.Zone == zone && sl.TenantId == currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(sl => sl.Warehouse != null ? sl.Warehouse.Name : string.Empty)
            .ThenBy(sl => sl.Code)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(sl => new StorageLocationDto
            {
                Id = sl.Id,
                Code = sl.Code,
                Description = sl.Description,
                WarehouseId = sl.WarehouseId,
                WarehouseName = sl.Warehouse != null ? sl.Warehouse.Name : null,
                Capacity = sl.Capacity,
                Occupancy = sl.Occupancy,
                LastInventoryDate = sl.LastInventoryDate,
                IsRefrigerated = sl.IsRefrigerated,
                Notes = sl.Notes,
                Zone = sl.Zone,
                Floor = sl.Floor,
                Row = sl.Row,
                Column = sl.Column,
                Level = sl.Level,
                IsActive = sl.IsActive,
                CreatedAt = sl.CreatedAt,
                CreatedBy = sl.CreatedBy,
                ModifiedAt = sl.ModifiedAt,
                ModifiedBy = sl.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StorageLocationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

}
