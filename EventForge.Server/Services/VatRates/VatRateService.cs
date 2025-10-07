using EventForge.DTOs.VatRates;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.VatRates;

/// <summary>
/// Service implementation for managing VAT rates.
/// </summary>
public class VatRateService : IVatRateService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<VatRateService> _logger;

    public VatRateService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<VatRateService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in VAT rate queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT rate operations.");
            }

            var query = _context.VatRates
                .Include(v => v.VatNature)
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var vatRates = await query
                .OrderBy(v => v.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var vatRateDtos = vatRates.Select(MapToVatRateDto);

            return new PagedResult<VatRateDto>
            {
                Items = vatRateDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VAT rates.");
            throw;
        }
    }

    public async Task<VatRateDto?> GetVatRateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vatRate = await _context.VatRates
                .Include(v => v.VatNature)
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return vatRate != null ? MapToVatRateDto(vatRate) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createVatRateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createVatRateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT rate operations.");
            }

            var vatRate = new VatRate
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                Name = createVatRateDto.Name,
                Percentage = createVatRateDto.Percentage,
                ValidFrom = createVatRateDto.ValidFrom,
                ValidTo = createVatRateDto.ValidTo,
                Notes = createVatRateDto.Notes,
                VatNatureId = createVatRateDto.VatNatureId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = _context.VatRates.Add(vatRate);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatRate, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("VAT rate {VatRateId} created by {User}.", vatRate.Id, currentUser);

            return MapToVatRateDto(vatRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VAT rate.");
            throw;
        }
    }

    public async Task<VatRateDto?> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateVatRateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateVatRateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatRate = await _context.VatRates
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatRate == null) return null;

            var vatRate = await _context.VatRates
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatRate == null) return null;

            vatRate.Name = updateVatRateDto.Name;
            vatRate.Percentage = updateVatRateDto.Percentage;
            vatRate.ValidFrom = updateVatRateDto.ValidFrom;
            vatRate.ValidTo = updateVatRateDto.ValidTo;
            vatRate.Notes = updateVatRateDto.Notes;
            vatRate.VatNatureId = updateVatRateDto.VatNatureId;
            vatRate.ModifiedAt = DateTime.UtcNow;
            vatRate.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatRate, "Update", currentUser, originalVatRate, cancellationToken);

            _logger.LogInformation("VAT rate {VatRateId} updated by {User}.", vatRate.Id, currentUser);

            return MapToVatRateDto(vatRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteVatRateAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatRate = await _context.VatRates
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatRate == null) return false;

            var vatRate = await _context.VatRates
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatRate == null) return false;

            vatRate.IsDeleted = true;
            vatRate.ModifiedAt = DateTime.UtcNow;
            vatRate.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(vatRate, "Delete", currentUser, originalVatRate, cancellationToken);

            _logger.LogInformation("VAT rate {VatRateId} deleted by {User}.", vatRate.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<bool> VatRateExistsAsync(Guid vatRateId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.VatRates
                .AnyAsync(v => v.Id == vatRateId && !v.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if VAT rate {VatRateId} exists.", vatRateId);
            throw;
        }
    }

    private static VatRateDto MapToVatRateDto(VatRate vatRate)
    {
        return new VatRateDto
        {
            Id = vatRate.Id,
            Name = vatRate.Name,
            Percentage = vatRate.Percentage,
            Status = (EventForge.DTOs.Common.VatRateStatus)vatRate.Status,
            ValidFrom = vatRate.ValidFrom,
            ValidTo = vatRate.ValidTo,
            Notes = vatRate.Notes,
            VatNatureId = vatRate.VatNatureId,
            VatNatureCode = vatRate.VatNature?.Code,
            VatNatureName = vatRate.VatNature?.Name,
            IsActive = vatRate.IsActive,
            CreatedAt = vatRate.CreatedAt,
            CreatedBy = vatRate.CreatedBy,
            ModifiedAt = vatRate.ModifiedAt,
            ModifiedBy = vatRate.ModifiedBy
        };
    }
}