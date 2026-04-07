using Prym.DTOs.VatRates;
using Prym.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.VatRates;

/// <summary>
/// Service implementation for managing VAT natures.
/// </summary>
public class VatNatureService(
    PrymDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<VatNatureService> logger,
    ICacheService cacheService) : IVatNatureService
{

    private const string CACHE_KEY_ALL = "VatNatures_All";

    public async Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for VAT nature operations.");
            }

            // Cache all VatNatures for 30 minutes
            var allNatures = await cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                currentTenantId.Value,
                async (ct) =>
                {
                    return await context.VatNatures
                        .WhereActiveTenant(currentTenantId.Value)
                        .OrderBy(v => v.Code)
                        .Select(v => MapToVatNatureDto(v))
                        .ToListAsync(ct);
                },
                absoluteExpiration: TimeSpan.FromMinutes(30),
                ct: cancellationToken
            );

            // Paginate in memory (VatNatures are typically few - usually < 50 per tenant)
            // Note: If a tenant has a very large number of VAT natures, consider per-page caching
            var totalCount = allNatures.Count;
            var items = allNatures
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<VatNatureDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving VAT natures.");
            throw;
        }
    }

    public async Task<VatNatureDto?> GetVatNatureByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var vatNature = await context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return vatNature is not null ? MapToVatNatureDto(vatNature) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving VAT nature {VatNatureId}.", id);
            throw;
        }
    }

    public async Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createVatNatureDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createVatNatureDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
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

            _ = context.VatNatures.Add(vatNature);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(vatNature, "Insert", currentUser, null, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            logger.LogInformation("VAT nature {VatNatureId} created by {User}.", vatNature.Id, currentUser);

            return MapToVatNatureDto(vatNature);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating VAT nature.");
            throw;
        }
    }

    public async Task<VatNatureDto?> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateVatNatureDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateVatNatureDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatNature = await context.VatNatures
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatNature is null) return null;

            var vatNature = await context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatNature is null) return null;

            vatNature.Code = updateVatNatureDto.Code;
            vatNature.Name = updateVatNatureDto.Name;
            vatNature.Description = updateVatNatureDto.Description;
            vatNature.ModifiedAt = DateTime.UtcNow;
            vatNature.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating VatNature {VatNatureId}.", id);
                throw new InvalidOperationException("La natura IVA è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(vatNature, "Update", currentUser, originalVatNature, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, originalVatNature.TenantId);

            logger.LogInformation("VAT nature {VatNatureId} updated by {User}.", vatNature.Id, currentUser);

            return MapToVatNatureDto(vatNature);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating VAT nature {VatNatureId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteVatNatureAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalVatNature = await context.VatNatures
                .AsNoTracking()
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalVatNature is null) return false;

            var vatNature = await context.VatNatures
                .Where(v => v.Id == id && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (vatNature is null) return false;

            vatNature.IsDeleted = true;
            vatNature.ModifiedAt = DateTime.UtcNow;
            vatNature.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting VatNature {VatNatureId}.", id);
                throw new InvalidOperationException("La natura IVA è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(vatNature, "Delete", currentUser, originalVatNature, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, originalVatNature.TenantId);

            logger.LogInformation("VAT nature {VatNatureId} deleted by {User}.", vatNature.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting VAT nature {VatNatureId}.", id);
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
