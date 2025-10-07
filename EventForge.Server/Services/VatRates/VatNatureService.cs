using EventForge.DTOs.VatRates;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.VatRates;

/// <summary>
/// Service implementation for managing VAT natures.
/// </summary>
public class VatNatureService : IVatNatureService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<VatNatureService> _logger;

    public VatNatureService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<VatNatureService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT nature operations.");
            }

            var query = _context.VatNatures
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var vatNatures = await query
                .OrderBy(v => v.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var vatNatureDtos = vatNatures.Select(MapToVatNatureDto);

            return new PagedResult<VatNatureDto>
            {
                Items = vatNatureDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VAT natures.");
            throw;
        }
    }

    public async Task<VatNatureDto?> GetVatNatureByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vatNature = await _context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return vatNature != null ? MapToVatNatureDto(vatNature) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VAT nature {VatNatureId}.", id);
            throw;
        }
    }

    public async Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createVatNatureDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createVatNatureDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT nature operations.");
            }

            var vatNature = new VatNature
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Code = createVatNatureDto.Code,
                Name = createVatNatureDto.Name,
                Description = createVatNatureDto.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = _context.VatNatures.Add(vatNature);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatNature, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("VAT nature {VatNatureId} created by {User}.", vatNature.Id, currentUser);

            return MapToVatNatureDto(vatNature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VAT nature.");
            throw;
        }
    }

    public async Task<VatNatureDto?> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateVatNatureDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateVatNatureDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatNature = await _context.VatNatures
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatNature == null) return null;

            var vatNature = await _context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatNature == null) return null;

            vatNature.Code = updateVatNatureDto.Code;
            vatNature.Name = updateVatNatureDto.Name;
            vatNature.Description = updateVatNatureDto.Description;
            vatNature.ModifiedAt = DateTime.UtcNow;
            vatNature.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatNature, "Update", currentUser, originalVatNature, cancellationToken);

            _logger.LogInformation("VAT nature {VatNatureId} updated by {User}.", vatNature.Id, currentUser);

            return MapToVatNatureDto(vatNature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VAT nature {VatNatureId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteVatNatureAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatNature = await _context.VatNatures
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatNature == null) return false;

            var vatNature = await _context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatNature == null) return false;

            vatNature.IsDeleted = true;
            vatNature.ModifiedAt = DateTime.UtcNow;
            vatNature.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatNature, "Delete", currentUser, originalVatNature, cancellationToken);

            _logger.LogInformation("VAT nature {VatNatureId} deleted by {User}.", vatNature.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VAT nature {VatNatureId}.", id);
            throw;
        }
    }

    private static VatNatureDto MapToVatNatureDto(VatNature vatNature)
    {
        return new VatNatureDto
        {
            Id = vatNature.Id,
            Code = vatNature.Code,
            Name = vatNature.Name,
            Description = vatNature.Description,
            CreatedAt = vatNature.CreatedAt,
            CreatedBy = vatNature.CreatedBy,
            ModifiedAt = vatNature.ModifiedAt,
            ModifiedBy = vatNature.ModifiedBy
        };
    }
}
