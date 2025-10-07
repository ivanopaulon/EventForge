using EventForge.DTOs.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.UnitOfMeasures;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService : IUMService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UMService> _logger;

    public UMService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<UMService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in UM queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var query = _context.UMs
                .WhereActiveTenant(currentTenantId.Value)
                .Where(u => !u.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var ums = await query
                .OrderBy(u => u.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var umDtos = ums.Select(MapToUMDto);

            return new PagedResult<UMDto>
            {
                Items = umDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving units of measure.");
            throw;
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var um = await _context.UMs
                .Where(u => u.Id == id && u.TenantId == currentTenantId.Value && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return um != null ? MapToUMDto(um) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createUMDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for unit of measure operations.");
            }

            var um = new UM
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createUMDto.Name,
                Symbol = createUMDto.Symbol,
                Description = createUMDto.Description,
                IsDefault = createUMDto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = _context.UMs.Add(um);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(um, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Unit of measure {UMId} created by {User}.", um.Id, currentUser);

            return MapToUMDto(um);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating unit of measure.");
            throw;
        }
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateUMDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalUM = await _context.UMs
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalUM == null) return null;

            var um = await _context.UMs
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (um == null) return null;

            um.Name = updateUMDto.Name;
            // Note: Symbol is intentionally not updatable - it's used in calculations
            um.Description = updateUMDto.Description;
            um.IsDefault = updateUMDto.IsDefault;
            um.ModifiedAt = DateTime.UtcNow;
            um.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(um, "Update", currentUser, originalUM, cancellationToken);

            _logger.LogInformation("Unit of measure {UMId} updated by {User}.", um.Id, currentUser);

            return MapToUMDto(um);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteUMAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalUM = await _context.UMs
                .AsNoTracking()
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalUM == null) return false;

            var um = await _context.UMs
                .Where(u => u.Id == id && !u.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (um == null) return false;

            um.IsDeleted = true;
            um.ModifiedAt = DateTime.UtcNow;
            um.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(um, "Delete", currentUser, originalUM, cancellationToken);

            _logger.LogInformation("Unit of measure {UMId} deleted by {User}.", um.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting unit of measure {UMId}.", id);
            throw;
        }
    }

    public async Task<bool> UMExistsAsync(Guid umId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UMs
                .AnyAsync(u => u.Id == umId && !u.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if unit of measure {UMId} exists.", umId);
            throw;
        }
    }

    private static UMDto MapToUMDto(UM um)
    {
        return new UMDto
        {
            Id = um.Id,
            Name = um.Name,
            Symbol = um.Symbol,
            Description = um.Description,
            IsDefault = um.IsDefault,
            IsActive = um.IsActive,
            CreatedAt = um.CreatedAt,
            CreatedBy = um.CreatedBy,
            ModifiedAt = um.ModifiedAt,
            ModifiedBy = um.ModifiedBy
        };
    }
}