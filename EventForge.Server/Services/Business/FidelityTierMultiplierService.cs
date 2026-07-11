using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

public class FidelityTierMultiplierService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : IFidelityTierMultiplierService
{
    public async Task<IEnumerable<FidelityTierMultiplier>> GetByCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted && multiplier.CampaignId == campaignId)
            .OrderBy(multiplier => multiplier.CardType)
            .ToListAsync(ct);
    }

    public async Task<FidelityTierMultiplier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted)
            .FirstOrDefaultAsync(multiplier => multiplier.Id == id, ct);
    }

    public async Task<FidelityTierMultiplier> CreateAsync(FidelityTierMultiplier tierMultiplier, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        // One multiplier per card type per campaign — each campaign owns its own set of tier
        // multipliers, so uniqueness is now scoped to CampaignId instead of the whole tenant.
        var alreadyExists = await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted && multiplier.CampaignId == tierMultiplier.CampaignId)
            .AnyAsync(multiplier => multiplier.CardType == tierMultiplier.CardType, ct);

        if (alreadyExists)
        {
            throw new InvalidOperationException($"Esiste già un moltiplicatore per il livello {tierMultiplier.CardType} in questa campagna.");
        }

        var entity = new FidelityTierMultiplier
        {
            TenantId = tenantId,
            CampaignId = tierMultiplier.CampaignId,
            CardType = tierMultiplier.CardType,
            Multiplier = tierMultiplier.Multiplier,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.FidelityTierMultipliers.Add(entity);
        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Create", currentUser, null, ct);
        return entity;
    }

    public async Task<FidelityTierMultiplier?> UpdateAsync(Guid id, FidelityTierMultiplier tierMultiplier, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalMultiplier = await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted)
            .FirstOrDefaultAsync(multiplier => multiplier.Id == id, ct);

        if (originalMultiplier is null)
        {
            return null;
        }

        var duplicateExists = await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted && multiplier.CampaignId == originalMultiplier.CampaignId)
            .AnyAsync(multiplier => multiplier.Id != id && multiplier.CardType == tierMultiplier.CardType, ct);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Esiste già un moltiplicatore per il livello {tierMultiplier.CardType} in questa campagna.");
        }

        var existingMultiplier = await context.FidelityTierMultipliers
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted)
            .FirstOrDefaultAsync(multiplier => multiplier.Id == id, ct);

        if (existingMultiplier is null)
        {
            return null;
        }

        existingMultiplier.CardType = tierMultiplier.CardType;
        existingMultiplier.Multiplier = tierMultiplier.Multiplier;
        existingMultiplier.ModifiedAt = DateTime.UtcNow;
        existingMultiplier.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(existingMultiplier, "Update", currentUser, originalMultiplier, ct);
        return existingMultiplier;
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalMultiplier = await context.FidelityTierMultipliers
            .AsNoTracking()
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted)
            .FirstOrDefaultAsync(multiplier => multiplier.Id == id, ct);

        if (originalMultiplier is null)
        {
            return false;
        }

        var existingMultiplier = await context.FidelityTierMultipliers
            .Where(multiplier => multiplier.TenantId == tenantId && !multiplier.IsDeleted)
            .FirstOrDefaultAsync(multiplier => multiplier.Id == id, ct);

        if (existingMultiplier is null)
        {
            return false;
        }

        existingMultiplier.IsDeleted = true;
        existingMultiplier.DeletedAt = DateTime.UtcNow;
        existingMultiplier.DeletedBy = currentUser;
        existingMultiplier.ModifiedAt = DateTime.UtcNow;
        existingMultiplier.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(existingMultiplier, "Delete", currentUser, originalMultiplier, ct);
        return true;
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity tier multiplier operations.");
        }

        return tenantId.Value;
    }
}
