using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

public class FidelityPointsBaseRateService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : IFidelityPointsBaseRateService
{
    public async Task<IEnumerable<FidelityPointsBaseRate>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .ThenByDescending(rate => rate.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<FidelityPointsBaseRate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .FirstOrDefaultAsync(rate => rate.Id == id, ct);
    }

    public async Task<FidelityPointsBaseRate> CreateAsync(FidelityPointsBaseRate baseRate, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();
        ValidateDateRange(baseRate.EffectiveFrom, baseRate.EffectiveTo);

        FidelityPointsBaseRate? existingCurrentRate = null;
        FidelityPointsBaseRate? originalCurrentRate = null;

        if (baseRate.EffectiveTo is null)
        {
            existingCurrentRate = await context.FidelityPointsBaseRates
                .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
                .FirstOrDefaultAsync(rate => rate.EffectiveTo == null, ct);

            if (existingCurrentRate is not null)
            {
                originalCurrentRate = await context.FidelityPointsBaseRates
                    .AsNoTracking()
                    .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
                    .FirstOrDefaultAsync(rate => rate.Id == existingCurrentRate.Id, ct);

                existingCurrentRate.EffectiveTo = baseRate.EffectiveFrom.AddDays(-1);
                existingCurrentRate.ModifiedAt = DateTime.UtcNow;
                existingCurrentRate.ModifiedBy = currentUser;
            }
        }

        var otherRates = await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .Where(rate => existingCurrentRate == null || rate.Id != existingCurrentRate.Id)
            .ToListAsync(ct);

        FidelityPointsOverlapValidation.EnsureNoOverlap(
            otherRates,
            baseRate.EffectiveFrom,
            baseRate.EffectiveTo ?? DateTime.MaxValue,
            rate => rate.EffectiveFrom,
            rate => rate.EffectiveTo,
            rate => $"La tariffa base si sovrappone a una tariffa esistente ({rate.EffectiveFrom:d} - {(rate.EffectiveTo?.ToString("d") ?? "corrente")}).");

        var entity = new FidelityPointsBaseRate
        {
            TenantId = tenantId,
            Rate = baseRate.Rate,
            RoundingMode = baseRate.RoundingMode,
            EffectiveFrom = baseRate.EffectiveFrom,
            EffectiveTo = baseRate.EffectiveTo,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.FidelityPointsBaseRates.Add(entity);
        _ = await context.SaveChangesAsync(ct);

        if (existingCurrentRate is not null && originalCurrentRate is not null)
        {
            _ = await auditLogService.TrackEntityChangesAsync(existingCurrentRate, "Update", currentUser, originalCurrentRate, ct);
        }

        _ = await auditLogService.TrackEntityChangesAsync(entity, "Create", currentUser, null, ct);
        return entity;
    }

    public async Task<FidelityPointsBaseRate?> UpdateAsync(Guid id, FidelityPointsBaseRate baseRate, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();
        ValidateDateRange(baseRate.EffectiveFrom, baseRate.EffectiveTo);

        var originalRate = await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .FirstOrDefaultAsync(rate => rate.Id == id, ct);

        if (originalRate is null)
        {
            return null;
        }

        var existingRate = await context.FidelityPointsBaseRates
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .FirstOrDefaultAsync(rate => rate.Id == id, ct);

        if (existingRate is null)
        {
            return null;
        }

        FidelityPointsBaseRate? otherCurrentRate = null;
        FidelityPointsBaseRate? originalOtherCurrentRate = null;

        if (baseRate.EffectiveTo is null)
        {
            otherCurrentRate = await context.FidelityPointsBaseRates
                .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
                .FirstOrDefaultAsync(rate => rate.Id != id && rate.EffectiveTo == null, ct);

            if (otherCurrentRate is not null)
            {
                originalOtherCurrentRate = await context.FidelityPointsBaseRates
                    .AsNoTracking()
                    .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
                    .FirstOrDefaultAsync(rate => rate.Id == otherCurrentRate.Id, ct);

                otherCurrentRate.EffectiveTo = baseRate.EffectiveFrom.AddDays(-1);
                otherCurrentRate.ModifiedAt = DateTime.UtcNow;
                otherCurrentRate.ModifiedBy = currentUser;
            }
        }

        var otherRates = await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .Where(rate => rate.Id != id && (otherCurrentRate == null || rate.Id != otherCurrentRate.Id))
            .ToListAsync(ct);

        FidelityPointsOverlapValidation.EnsureNoOverlap(
            otherRates,
            baseRate.EffectiveFrom,
            baseRate.EffectiveTo ?? DateTime.MaxValue,
            rate => rate.EffectiveFrom,
            rate => rate.EffectiveTo,
            rate => $"La tariffa base si sovrappone a una tariffa esistente ({rate.EffectiveFrom:d} - {(rate.EffectiveTo?.ToString("d") ?? "corrente")}).");

        existingRate.Rate = baseRate.Rate;
        existingRate.RoundingMode = baseRate.RoundingMode;
        existingRate.EffectiveFrom = baseRate.EffectiveFrom;
        existingRate.EffectiveTo = baseRate.EffectiveTo;
        existingRate.ModifiedAt = DateTime.UtcNow;
        existingRate.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);

        if (otherCurrentRate is not null && originalOtherCurrentRate is not null)
        {
            _ = await auditLogService.TrackEntityChangesAsync(otherCurrentRate, "Update", currentUser, originalOtherCurrentRate, ct);
        }

        _ = await auditLogService.TrackEntityChangesAsync(existingRate, "Update", currentUser, originalRate, ct);
        return existingRate;
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalRate = await context.FidelityPointsBaseRates
            .AsNoTracking()
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .FirstOrDefaultAsync(rate => rate.Id == id, ct);

        if (originalRate is null)
        {
            return false;
        }

        var existingRate = await context.FidelityPointsBaseRates
            .Where(rate => rate.TenantId == tenantId && !rate.IsDeleted)
            .FirstOrDefaultAsync(rate => rate.Id == id, ct);

        if (existingRate is null)
        {
            return false;
        }

        existingRate.IsDeleted = true;
        existingRate.DeletedAt = DateTime.UtcNow;
        existingRate.DeletedBy = currentUser;
        existingRate.ModifiedAt = DateTime.UtcNow;
        existingRate.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(existingRate, "Delete", currentUser, originalRate, ct);
        return true;
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity base rate operations.");
        }

        return tenantId.Value;
    }

    private static void ValidateDateRange(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new InvalidOperationException("La data di fine non può essere precedente alla data di inizio.");
        }
    }
}
