using EventForge.Server.DTOs.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing storage locations within warehouses.
/// </summary>
public class StorageLocationService : IStorageLocationService
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<StorageLocationService> _logger;

    public StorageLocationService(EventForgeDbContext context, ILogger<StorageLocationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageLocationDto>> GetStorageLocationsAsync(int page = 1, int pageSize = 20, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting storage locations: page={Page}, pageSize={PageSize}, warehouseId={WarehouseId}", page, pageSize, warehouseId);

        var query = _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            query = query.Where(sl => sl.WarehouseId == warehouseId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var items = await query
            .Skip(skip)
            .Take(pageSize)
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
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StorageLocationDto?> GetStorageLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting storage location by ID: {Id}", id);

        var location = await _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.Id == id)
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
        _logger.LogDebug("Getting storage locations for warehouse: {WarehouseId}", warehouseId);

        var locations = await _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.WarehouseId == warehouseId)
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
        _logger.LogDebug("Getting available storage locations for warehouse: {WarehouseId}", warehouseId);

        var query = _context.StorageLocations
            .Include(sl => sl.Warehouse)
            .Where(sl => sl.IsActive &&
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
        _logger.LogDebug("Creating storage location: {Code}", createDto.Code);

        // Validate warehouse exists
        var warehouseExists = await _context.StorageFacilities
            .AnyAsync(sf => sf.Id == createDto.WarehouseId, cancellationToken);

        if (!warehouseExists)
        {
            throw new ArgumentException($"Warehouse with ID {createDto.WarehouseId} not found.");
        }

        // Check for duplicate code within the same warehouse
        var codeExists = await _context.StorageLocations
            .AnyAsync(sl => sl.Code == createDto.Code && sl.WarehouseId == createDto.WarehouseId, cancellationToken);

        if (codeExists)
        {
            throw new ArgumentException($"Storage location with code '{createDto.Code}' already exists in this warehouse.");
        }

        // Validate occupancy doesn't exceed capacity
        if (createDto.Capacity.HasValue && createDto.Occupancy.HasValue && createDto.Occupancy > createDto.Capacity)
        {
            throw new ArgumentException("Occupancy cannot exceed capacity.");
        }

        var location = new StorageLocation
        {
            Id = Guid.NewGuid(),
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

        _context.StorageLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created storage location: {Id} - {Code}", location.Id, location.Code);

        // Return the created location with warehouse info
        return await GetStorageLocationByIdAsync(location.Id, cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve created storage location.");
    }

    public async Task<StorageLocationDto?> UpdateStorageLocationAsync(Guid id, UpdateStorageLocationDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating storage location: {Id}", id);

        var location = await _context.StorageLocations.FindAsync(new object[] { id }, cancellationToken);
        if (location == null)
        {
            return null;
        }

        // Validate warehouse exists if changed
        if (updateDto.WarehouseId.HasValue && updateDto.WarehouseId != location.WarehouseId)
        {
            var warehouseExists = await _context.StorageFacilities
                .AnyAsync(sf => sf.Id == updateDto.WarehouseId.Value, cancellationToken);

            if (!warehouseExists)
            {
                throw new ArgumentException($"Warehouse with ID {updateDto.WarehouseId} not found.");
            }
        }

        // Check for duplicate code if changed
        if (!string.IsNullOrEmpty(updateDto.Code) && updateDto.Code != location.Code)
        {
            var warehouseIdToCheck = updateDto.WarehouseId ?? location.WarehouseId;
            var codeExists = await _context.StorageLocations
                .AnyAsync(sl => sl.Code == updateDto.Code && sl.WarehouseId == warehouseIdToCheck && sl.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new ArgumentException($"Storage location with code '{updateDto.Code}' already exists in this warehouse.");
            }
        }

        // Validate occupancy doesn't exceed capacity
        var newCapacity = updateDto.Capacity ?? location.Capacity;
        var newOccupancy = updateDto.Occupancy ?? location.Occupancy;

        if (newCapacity.HasValue && newOccupancy.HasValue && newOccupancy > newCapacity)
        {
            throw new ArgumentException("Occupancy cannot exceed capacity.");
        }

        // Update properties
        if (!string.IsNullOrEmpty(updateDto.Code))
            location.Code = updateDto.Code;
        if (updateDto.Description != null)
            location.Description = updateDto.Description;
        if (updateDto.WarehouseId.HasValue)
            location.WarehouseId = updateDto.WarehouseId.Value;
        if (updateDto.Capacity.HasValue)
            location.Capacity = updateDto.Capacity;
        if (updateDto.Occupancy.HasValue)
            location.Occupancy = updateDto.Occupancy;
        if (updateDto.IsRefrigerated.HasValue)
            location.IsRefrigerated = updateDto.IsRefrigerated.Value;
        if (updateDto.Notes != null)
            location.Notes = updateDto.Notes;
        if (updateDto.Zone != null)
            location.Zone = updateDto.Zone;
        if (updateDto.Floor != null)
            location.Floor = updateDto.Floor;
        if (updateDto.Row != null)
            location.Row = updateDto.Row;
        if (updateDto.Column != null)
            location.Column = updateDto.Column;
        if (updateDto.Level != null)
            location.Level = updateDto.Level;
        if (updateDto.IsActive.HasValue)
            location.IsActive = updateDto.IsActive.Value;

        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated storage location: {Id} - {Code}", location.Id, location.Code);

        return await GetStorageLocationByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteStorageLocationAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting storage location: {Id}", id);

        var location = await _context.StorageLocations.FindAsync(new object[] { id }, cancellationToken);
        if (location == null)
        {
            return false;
        }

        // Check if location has inventory (occupancy > 0)
        if (location.Occupancy > 0)
        {
            throw new InvalidOperationException("Cannot delete storage location that contains inventory. Move inventory first.");
        }

        // Soft delete
        location.IsDeleted = true;
        location.DeletedAt = DateTime.UtcNow;
        location.DeletedBy = currentUser;
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted storage location: {Id}", id);
        return true;
    }

    public async Task<StorageLocationDto?> UpdateOccupancyAsync(Guid id, int newOccupancy, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating occupancy for storage location: {Id} to {Occupancy}", id, newOccupancy);

        var location = await _context.StorageLocations.FindAsync(new object[] { id }, cancellationToken);
        if (location == null)
        {
            return null;
        }

        if (newOccupancy < 0)
        {
            throw new ArgumentException("Occupancy cannot be negative.");
        }

        if (location.Capacity.HasValue && newOccupancy > location.Capacity)
        {
            throw new ArgumentException($"Occupancy ({newOccupancy}) cannot exceed capacity ({location.Capacity}).");
        }

        location.Occupancy = newOccupancy;
        location.ModifiedAt = DateTime.UtcNow;
        location.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated occupancy for storage location: {Id} to {Occupancy}", id, newOccupancy);

        return await GetStorageLocationByIdAsync(id, cancellationToken);
    }
}