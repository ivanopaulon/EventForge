using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

public class FidelityPointsCampaignService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : IFidelityPointsCampaignService
{
    public async Task<IEnumerable<FidelityPointsCampaign>> GetAllAsync(CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityPointsCampaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && !campaign.IsDeleted)
            .OrderByDescending(campaign => campaign.StartDate)
            .ThenByDescending(campaign => campaign.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<FidelityPointsCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        return await context.FidelityPointsCampaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && !campaign.IsDeleted)
            .FirstOrDefaultAsync(campaign => campaign.Id == id, ct);
    }

    public async Task<FidelityPointsCampaign> CreateAsync(FidelityPointsCampaign campaign, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();
        ValidateDateRange(campaign.StartDate, campaign.EndDate);

        if (campaign.IsActive)
        {
            var activeCampaigns = await context.FidelityPointsCampaigns
                .AsNoTracking()
                .Where(existing => existing.TenantId == tenantId && !existing.IsDeleted && existing.IsActive)
                .ToListAsync(ct);

            FidelityPointsOverlapValidation.EnsureNoOverlap(
                activeCampaigns,
                campaign.StartDate,
                campaign.EndDate,
                existing => existing.StartDate,
                existing => existing.EndDate,
                existing => $"La campagna si sovrappone a \"{existing.Name}\" ({existing.StartDate:d} - {existing.EndDate:d}). Concludi o modifica quella esistente prima di crearne una nuova nello stesso periodo.");
        }

        var entity = new FidelityPointsCampaign
        {
            TenantId = tenantId,
            Name = campaign.Name,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Multiplier = campaign.Multiplier,
            RoundingMode = campaign.RoundingMode,
            IgnoreTierMultiplier = campaign.IgnoreTierMultiplier,
            IsActive = campaign.IsActive,
            ProductIdsJSON = campaign.ProductIdsJSON,
            CategoryIdsJSON = campaign.CategoryIdsJSON,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.FidelityPointsCampaigns.Add(entity);
        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(entity, "Create", currentUser, null, ct);
        return entity;
    }

    public async Task<FidelityPointsCampaign?> UpdateAsync(Guid id, FidelityPointsCampaign campaign, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();
        ValidateDateRange(campaign.StartDate, campaign.EndDate);

        var originalCampaign = await context.FidelityPointsCampaigns
            .AsNoTracking()
            .Where(existing => existing.TenantId == tenantId && !existing.IsDeleted)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (originalCampaign is null)
        {
            return null;
        }

        if (campaign.IsActive)
        {
            var activeCampaigns = await context.FidelityPointsCampaigns
                .AsNoTracking()
                .Where(existing => existing.TenantId == tenantId && !existing.IsDeleted && existing.IsActive)
                .Where(existing => existing.Id != id)
                .ToListAsync(ct);

            FidelityPointsOverlapValidation.EnsureNoOverlap(
                activeCampaigns,
                campaign.StartDate,
                campaign.EndDate,
                existing => existing.StartDate,
                existing => existing.EndDate,
                existing => $"La campagna si sovrappone a \"{existing.Name}\" ({existing.StartDate:d} - {existing.EndDate:d}). Concludi o modifica quella esistente prima di crearne una nuova nello stesso periodo.");
        }

        var existingCampaign = await context.FidelityPointsCampaigns
            .Where(existing => existing.TenantId == tenantId && !existing.IsDeleted)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (existingCampaign is null)
        {
            return null;
        }

        existingCampaign.Name = campaign.Name;
        existingCampaign.StartDate = campaign.StartDate;
        existingCampaign.EndDate = campaign.EndDate;
        existingCampaign.Multiplier = campaign.Multiplier;
        existingCampaign.RoundingMode = campaign.RoundingMode;
        existingCampaign.IgnoreTierMultiplier = campaign.IgnoreTierMultiplier;
        existingCampaign.IsActive = campaign.IsActive;
        existingCampaign.ProductIdsJSON = campaign.ProductIdsJSON;
        existingCampaign.CategoryIdsJSON = campaign.CategoryIdsJSON;
        existingCampaign.ModifiedAt = DateTime.UtcNow;
        existingCampaign.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(existingCampaign, "Update", currentUser, originalCampaign, ct);
        return existingCampaign;
    }

    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalCampaign = await context.FidelityPointsCampaigns
            .AsNoTracking()
            .Where(campaign => campaign.TenantId == tenantId && !campaign.IsDeleted)
            .FirstOrDefaultAsync(campaign => campaign.Id == id, ct);

        if (originalCampaign is null)
        {
            return false;
        }

        var existingCampaign = await context.FidelityPointsCampaigns
            .Where(campaign => campaign.TenantId == tenantId && !campaign.IsDeleted)
            .FirstOrDefaultAsync(campaign => campaign.Id == id, ct);

        if (existingCampaign is null)
        {
            return false;
        }

        existingCampaign.IsDeleted = true;
        existingCampaign.DeletedAt = DateTime.UtcNow;
        existingCampaign.DeletedBy = currentUser;
        existingCampaign.ModifiedAt = DateTime.UtcNow;
        existingCampaign.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(existingCampaign, "Delete", currentUser, originalCampaign, ct);
        return true;
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity campaign operations.");
        }

        return tenantId.Value;
    }

    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate < startDate)
        {
            throw new InvalidOperationException("La data di fine non può essere precedente alla data di inizio.");
        }
    }
}
