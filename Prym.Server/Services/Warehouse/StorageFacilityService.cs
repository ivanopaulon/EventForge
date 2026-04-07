using Prym.DTOs.Warehouse;
using Prym.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.Warehouse;

/// <summary>
/// Service implementation for managing storage facilities.
/// </summary>
public class StorageFacilityService(
    PrymDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<StorageFacilityService> logger,
    ICacheService cacheService) : IStorageFacilityService
{

    private const string CACHE_KEY_ALL = "StorageFacilities_All";

    public async Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for storage facility operations.");
            }

            // Cache all StorageFacilities for 5 minutes
            var allFacilities = await cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                currentTenantId.Value,
                async (ct) =>
                {
                    var facilities = await context.StorageFacilities
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
            logger.LogError(ex, "Error retrieving storage facilities.");
            throw;
        }
    }

    public async Task<StorageFacilityDto?> GetStorageFacilityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var facility = await context.StorageFacilities
                .Include(sf => sf.Locations)
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return facility is null ? null : MapToStorageFacilityDto(facility);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<StorageFacilityDto> CreateStorageFacilityAsync(CreateStorageFacilityDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
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

            _ = context.StorageFacilities.Add(facility);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(facility, "Create", currentUser, null, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            logger.LogInformation("Storage facility {FacilityId} created by {User}.", facility.Id, currentUser);

            return MapToStorageFacilityDto(facility);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating storage facility.");
            throw;
        }
    }

    public async Task<StorageFacilityDto?> UpdateStorageFacilityAsync(Guid id, UpdateStorageFacilityDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalFacility = await context.StorageFacilities
                .AsNoTracking()
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalFacility is null) return null;

            var facility = await context.StorageFacilities
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (facility is null) return null;

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

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating StorageFacility {StorageFacilityId}.", id);
                throw new InvalidOperationException("Il magazzino è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(facility, "Update", currentUser, originalFacility, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, originalFacility.TenantId);

            logger.LogInformation("Storage facility {FacilityId} updated by {User}.", facility.Id, currentUser);

            return MapToStorageFacilityDto(facility);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteStorageFacilityAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalFacility = await context.StorageFacilities
                .AsNoTracking()
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalFacility is null) return false;

            var facility = await context.StorageFacilities
                .Where(sf => sf.Id == id && !sf.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (facility is null) return false;

            facility.IsDeleted = true;
            facility.DeletedAt = DateTime.UtcNow;
            facility.DeletedBy = currentUser;
            facility.ModifiedAt = DateTime.UtcNow;
            facility.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting StorageFacility {StorageFacilityId}.", id);
                throw new InvalidOperationException("Il magazzino è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(facility, "Delete", currentUser, originalFacility, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, originalFacility.TenantId);

            logger.LogInformation("Storage facility {FacilityId} deleted by {User}.", facility.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting storage facility {FacilityId}.", id);
            throw;
        }
    }

    public async Task<bool> StorageFacilityExistsAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.StorageFacilities
                .AnyAsync(sf => sf.Id == facilityId && !sf.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if storage facility {FacilityId} exists.", facilityId);
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

    public async Task<IEnumerable<Prym.DTOs.Export.WarehouseExportDto>> GetWarehousesForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for storage facility operations.");
        }

        var query = context.StorageFacilities
            .Where(sf => !sf.IsDeleted && sf.TenantId == currentTenantId.Value)
            .OrderBy(sf => sf.Name);

        var totalCount = await query.CountAsync(ct);

        logger.LogInformation("Export requested for {Count} storage facilities", totalCount);

        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetWarehousesInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(sf => new Prym.DTOs.Export.WarehouseExportDto
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

    private async Task<IEnumerable<Prym.DTOs.Export.WarehouseExportDto>> GetWarehousesInBatchesAsync(
        IQueryable<StorageFacility> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.WarehouseExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(sf => new Prym.DTOs.Export.WarehouseExportDto
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

            logger.LogInformation("Batch export progress: {Processed}/{Total}",
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
