using EventForge.Server.Data.Entities.Business;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

public class FidelityTierService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : IFidelityTierService
{
    public async Task<IEnumerable<FidelityTier>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityTiers
            .AsNoTracking()
            .Include(tier => tier.Rule)
            .WhereActiveTenant(tenantId)
            .OrderBy(tier => tier.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<FidelityTier?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityTiers
            .AsNoTracking()
            .Include(tier => tier.Rule)
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(tier => tier.Id == id, ct);
    }

    public async Task<FidelityTier> CreateAsync(
        FidelityTier tier,
        decimal? minimumSpendThreshold,
        int evaluationPeriodMonths,
        string currentUser,
        CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var entity = new FidelityTier
        {
            TenantId = tenantId,
            Name = tier.Name,
            SortOrder = tier.SortOrder,
            Color = tier.Color,
            Icon = tier.Icon,
            IsActive = tier.IsActive,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.FidelityTiers.Add(entity);

        var rule = new FidelityTierRule
        {
            TenantId = tenantId,
            TierId = entity.Id,
            MinimumSpendThreshold = minimumSpendThreshold,
            EvaluationPeriodMonths = evaluationPeriodMonths,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };
        _ = context.FidelityTierRules.Add(rule);

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Create", currentUser, null, ct);

        entity.Rule = rule;
        return entity;
    }

    public async Task<FidelityTier?> UpdateAsync(
        Guid id,
        FidelityTier tier,
        decimal? minimumSpendThreshold,
        int evaluationPeriodMonths,
        string currentUser,
        CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalTier = await context.FidelityTiers
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (originalTier is null)
        {
            return null;
        }

        var entity = await context.FidelityTiers
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        entity.Name = tier.Name;
        entity.SortOrder = tier.SortOrder;
        entity.Color = tier.Color;
        entity.Icon = tier.Icon;
        entity.IsActive = tier.IsActive;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser;

        var rule = await context.FidelityTierRules
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(r => r.TierId == id, ct);

        if (rule is null)
        {
            rule = new FidelityTierRule
            {
                TenantId = tenantId,
                TierId = id,
                MinimumSpendThreshold = minimumSpendThreshold,
                EvaluationPeriodMonths = evaluationPeriodMonths,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };
            _ = context.FidelityTierRules.Add(rule);
        }
        else
        {
            rule.MinimumSpendThreshold = minimumSpendThreshold;
            rule.EvaluationPeriodMonths = evaluationPeriodMonths;
            rule.ModifiedAt = DateTime.UtcNow;
            rule.ModifiedBy = currentUser;
        }

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalTier, ct);

        entity.Rule = rule;
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalTier = await context.FidelityTiers
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (originalTier is null)
        {
            return false;
        }

        var entity = await context.FidelityTiers
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (entity is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.DeletedBy = currentUser;
        entity.ModifiedAt = now;
        entity.ModifiedBy = currentUser;

        var rule = await context.FidelityTierRules
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(r => r.TierId == id, ct);

        if (rule is not null)
        {
            rule.IsDeleted = true;
            rule.DeletedAt = now;
            rule.DeletedBy = currentUser;
            rule.ModifiedAt = now;
            rule.ModifiedBy = currentUser;
        }

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalTier, ct);

        return true;
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity tier operations.");
        }

        return tenantId.Value;
    }
}
