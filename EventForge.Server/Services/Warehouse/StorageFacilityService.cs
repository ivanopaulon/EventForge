using EventForge.DTOs.Warehouse;
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

    public StorageFacilityService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<StorageFacilityService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<StorageFacilityDto>> GetStorageFacilitiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in storage facility queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for storage facility operations.");
            }

            var query = _context.StorageFacilities
                .WhereActiveTenant(currentTenantId.Value)
                .Include(sf => sf.Locations.Where(l => !l.IsDeleted && l.TenantId == currentTenantId.Value));

            var totalCount = await query.CountAsync(cancellationToken);
            var facilities = await query
                .OrderBy(sf => sf.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var facilityDtos = facilities.Select(MapToStorageFacilityDto);

            return new PagedResult<StorageFacilityDto>
            {
                Items = facilityDtos,
                Page = page,
                PageSize = pageSize,
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

            _context.StorageFacilities.Add(facility);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(facility, "Create", currentUser, null, cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(facility, "Update", currentUser, originalFacility, cancellationToken);

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

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(facility, "Delete", currentUser, originalFacility, cancellationToken);

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
}