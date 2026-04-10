using EventForge.DTOs.VatRates;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.VatRates;

/// <summary>
/// Service implementation for managing VAT rates.
/// </summary>
public class VatRateService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<VatRateService> logger) : IVatRateService
{

    public async Task<PagedResult<VatRateDto>> GetVatRatesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT rate operations.");
            }

            var query = context.VatRates
                .AsNoTracking()
                .Include(v => v.VatNature)
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var vatRates = await query
                .OrderBy(v => v.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var vatRateDtos = vatRates.Select(MapToVatRateDto);

            return new PagedResult<VatRateDto>
            {
                Items = vatRateDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving VAT rates.");
            throw;
        }
    }

    public async Task<VatRateDto?> GetVatRateByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vatRate = await context.VatRates
                .AsNoTracking()
                .Include(v => v.VatNature)
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return vatRate is not null ? MapToVatRateDto(vatRate) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createVatRateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createVatRateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
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
                FiscalCode = createVatRateDto.FiscalCode,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.VatRates.Add(vatRate);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(vatRate, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("VAT rate {VatRateId} created by {User}.", vatRate.Id, currentUser);

            return MapToVatRateDto(vatRate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating VAT rate.");
            throw;
        }
    }

    public async Task<VatRateDto?> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateVatRateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateVatRateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var vatRate = await context.VatRates
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatRate is null) return null;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(vatRate).CurrentValues.Clone();
            var originalVatRate = (VatRate)originalValues.ToObject();

            vatRate.Name = updateVatRateDto.Name;
            vatRate.Percentage = updateVatRateDto.Percentage;
            vatRate.ValidFrom = updateVatRateDto.ValidFrom;
            vatRate.ValidTo = updateVatRateDto.ValidTo;
            vatRate.Notes = updateVatRateDto.Notes;
            vatRate.VatNatureId = updateVatRateDto.VatNatureId;
            vatRate.FiscalCode = updateVatRateDto.FiscalCode;
            vatRate.ModifiedAt = DateTime.UtcNow;
            vatRate.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating VatRate {VatRateId}.", id);
                throw new InvalidOperationException("L'aliquota IVA è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(vatRate, "Update", currentUser, originalVatRate, cancellationToken);

            logger.LogInformation("VAT rate {VatRateId} updated by {User}.", vatRate.Id, currentUser);

            return MapToVatRateDto(vatRate);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteVatRateAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var vatRate = await context.VatRates
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatRate is null) return false;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(vatRate).CurrentValues.Clone();
            var originalVatRate = (VatRate)originalValues.ToObject();

            vatRate.IsDeleted = true;
            vatRate.ModifiedAt = DateTime.UtcNow;
            vatRate.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting VatRate {VatRateId}.", id);
                throw new InvalidOperationException("L'aliquota IVA è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(vatRate, "Delete", currentUser, originalVatRate, cancellationToken);

            logger.LogInformation("VAT rate {VatRateId} deleted by {User}.", vatRate.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting VAT rate {VatRateId}.", id);
            throw;
        }
    }

    public async Task<bool> VatRateExistsAsync(Guid vatRateId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.VatRates
                .AsNoTracking()
                .AnyAsync(v => v.Id == vatRateId && !v.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if VAT rate {VatRateId} exists.", vatRateId);
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
            FiscalCode = vatRate.FiscalCode,
            IsActive = vatRate.IsActive,
            CreatedAt = vatRate.CreatedAt,
            CreatedBy = vatRate.CreatedBy,
            ModifiedAt = vatRate.ModifiedAt,
            ModifiedBy = vatRate.ModifiedBy
        };
    }

}
