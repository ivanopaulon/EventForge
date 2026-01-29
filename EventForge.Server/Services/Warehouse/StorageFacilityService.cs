using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using EventForge.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing storage facilities.
/// </summary>
public class StorageFacilityService : IStorageFacilityService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StorageFacilityService> _logger;
    private readonly ICacheService _cacheService;
    
    private const string CACHE_KEY_ALL = "StorageFacilities_All";

    public StorageFacilityService(
        EventForgeDbContext context, 
        IAuditLogService auditLogService, 
        ITenantContext tenantContext, 
        ILogger<StorageFacilityService> logger,
        ICacheService cacheService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for storage facility operations.");
            }

            // Cache all StorageFacilities for 5 minutes
            var allFacilities = await _cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                currentTenantId.Value,
                async (ct) =>
                {
                    var facilities = await _context.StorageFacilities
                        .AsNoTracking()
                        .WhereActiveTenant(currentTenantId.Value)
                        .Include(sf => sf.Locations.Where(l => !l.IsDeleted && l.TenantId == currentTenantId.Value))
                        .OrderBy(sf => sf.Name)
                        .ToListAsync(ct);

                    return facilities.Select(MapToStorageFacilityDto).ToList();
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                ct: cancellationToken
            );

            // Paginate in memory (StorageFacilities are typically few - usually < 50 per tenant)
            // Note: If a tenant has a very large number of facilities, consider per-page caching
            var totalCount = allFacilities.Count;
            var items = allFacilities
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToList();

            return new PagedResult<StorageFacilityDto>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facilities.");
            throw;
        }
    }

    public async Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var facility = await _context.StorageFacilities
                .Include(sf => sf.Locations)
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return facility == null ? null : MapToStorageFacilityDto(facility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for storage facility operations.");
            }

            var facility = new StorageFacility
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createDto.Name,
                Code = createDto.Code,
                Address = createDto.Address,
                Phone = createDto.Phone,
                Email = createDto.Email,
                Manager = createDto.Manager,
                IsFiscal = createDto.IsFiscal,
                Notes = createDto.Notes,
                AreaSquareMeters = createDto.AreaSquareMeters,
                Capacity = createDto.Capacity,
                IsRefrigerated = createDto.IsRefrigerated,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _ = _context.StorageFacilities.Add(facility);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(facility, "Create", currentUser, null, cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            _logger.LogInformation("Storage facility {FacilityId} created by {User}.", facility.Id, currentUser);

            return MapToStorageFacilityDto(facility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storage facility.");
            throw;
        }
    }

    public async Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalFacility = await _context.StorageFacilities
                .AsNoTracking()
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalFacility == null) return null;

            var facility = await _context.StorageFacilities
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (facility == null) return null;

            facility.Name = updateDto.Name;
            facility.Address = updateDto.Address;
            facility.Phone = updateDto.Phone;
            facility.Email = updateDto.Email;
            facility.Manager = updateDto.Manager;
            facility.IsFiscal = updateDto.IsFiscal;
            facility.Notes = updateDto.Notes;
            facility.AreaSquareMeters = updateDto.AreaSquareMeters;
            facility.Capacity = updateDto.Capacity;
            facility.IsRefrigerated = updateDto.IsRefrigerated;
            facility.ModifiedAt = DateTime.UtcNow;
            facility.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(facility, "Update", currentUser, originalFacility, cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, originalFacility.TenantId);

            _logger.LogInformation("Storage facility {FacilityId} updated by {User}.", facility.Id, currentUser);

            return MapToStorageFacilityDto(facility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteStorageFacilityAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalFacility = await _context.StorageFacilities
                .AsNoTracking()
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalFacility == null) return false;

            var facility = await _context.StorageFacilities
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (facility == null) return false;

            facility.IsDeleted = true;
            facility.DeletedAt = DateTime.UtcNow;
            facility.DeletedBy = currentUser;
            facility.ModifiedAt = DateTime.UtcNow;
            facility.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(facility, "Delete", currentUser, originalFacility, cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, originalFacility.TenantId);

            _logger.LogInformation("Storage facility {FacilityId} deleted by {User}.", facility.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<bool> StorageFacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.StorageFacilities
                .AnyAsync(sf => sf.Id == facilityId && !sf.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if storage facility {FacilityId} exists.", facilityId);
            throw;
        }
    }

    private static StorageFacilityDto MapToStorageFacilityDto(StorageFacility facility)
    {
        return new StorageFacilityDto
        {
            Id = facility.Id,
            Name = facility.Name,
            Code = facility.Code,
            Address = facility.Address,
            Phone = facility.Phone,
            Email = facility.Email,
            Manager = facility.Manager,
            IsFiscal = facility.IsFiscal,
            Notes = facility.Notes,
            AreaSquareMeters = facility.AreaSquareMeters,
            Capacity = facility.Capacity,
            IsRefrigerated = facility.IsRefrigerated,
            TotalLocations = facility.TotalLocations,
            ActiveLocations = facility.ActiveLocations,
            IsActive = facility.IsActive,
            CreatedAt = facility.CreatedAt,
            CreatedBy = facility.CreatedBy,
            ModifiedAt = facility.ModifiedAt,
            ModifiedBy = facility.ModifiedBy
        };
    }

    #region Export Operations

    public async Task<IEnumerable<EventForge.DTOs.Export.WarehouseExportDto>> GetWarehousesForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = _tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage facility operations.");
        }

        var query = _context.StorageFacilities
            .Where(sf => !sf.IsDeleted && sf.TenantId == currentTenantId.Value)
            .OrderBy(sf => sf.Name);
        
        var totalCount = await query.CountAsync(ct);
        
        _logger.LogInformation("Export requested for {Count} storage facilities", totalCount);
        
        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            _logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetWarehousesInBatchesAsync(query, ct);
        }
        
        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);
        
        return items.Select(sf => new EventForge.DTOs.Export.WarehouseExportDto
        {
            Id = sf.Id,
            Code = sf.Code,
            Name = sf.Name,
            Type = sf.IsFiscal ? "Fiscal" : "Standard",
            Address = sf.Address,
            City = ExtractCityFromAddress(sf.Address),
            IsActive = sf.IsActive,
            TotalStorageLocations = sf.TotalLocations,
            CreatedAt = sf.CreatedAt
        });
    }

    private async Task<IEnumerable<EventForge.DTOs.Export.WarehouseExportDto>> GetWarehousesInBatchesAsync(
        IQueryable<StorageFacility> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<EventForge.DTOs.Export.WarehouseExportDto>();
        var skip = 0;
        
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            
            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);
            
            if (batch.Count == 0) break;
            
            results.AddRange(batch.Select(sf => new EventForge.DTOs.Export.WarehouseExportDto
            {
                Id = sf.Id,
                Code = sf.Code,
                Name = sf.Name,
                Type = sf.IsFiscal ? "Fiscal" : "Standard",
                Address = sf.Address,
                City = ExtractCityFromAddress(sf.Address),
                IsActive = sf.IsActive,
                TotalStorageLocations = sf.TotalLocations,
                CreatedAt = sf.CreatedAt
            }));
            
            skip += batchSize;
            
            _logger.LogInformation("Batch export progress: {Processed}/{Total}", 
                Math.Min(skip, results.Count), results.Count);
        }
        
        return results;
    }

    private static string? ExtractCityFromAddress(string? address)
    {
        if (string.IsNullOrEmpty(address)) return null;
        
        // Try to extract city from address (simple heuristic - take last part before postal code if exists)
        var parts = address.Split(',');
        if (parts.Length > 1)
        {
            return parts[^2].Trim(); // Second to last part often contains city
        }
        
        return null;
    }

    #endregion
}