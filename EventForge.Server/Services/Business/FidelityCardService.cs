using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Services.Business;

public class FidelityCardService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    IFidelityTierEvaluationService tierEvaluationService) : IFidelityCardService
{
    public async Task<IEnumerable<FidelityCardDto>> GetAllCardsAsync(CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var cards = await context.FidelityCards
            .AsNoTracking()
            .Include(card => card.Tier)
            .WhereActiveTenant(tenantId)
            .OrderByDescending(card => card.CreatedAt)
            .ToListAsync(ct);

        return cards.Select(MapCard);
    }

    public async Task<IEnumerable<FidelityCardDto>> GetCardsByBusinessPartyAsync(Guid businessPartyId, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var cards = await context.FidelityCards
            .AsNoTracking()
            .Include(card => card.Tier)
            .WhereActiveTenant(tenantId)
            .Where(card => card.BusinessPartyId == businessPartyId)
            .OrderByDescending(card => card.CreatedAt)
            .ToListAsync(ct);

        return cards.Select(MapCard);
    }

    public async Task<FidelityCardDto?> GetCardByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var card = await context.FidelityCards
            .AsNoTracking()
            .Include(card => card.Tier)
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        return card is null ? null : MapCard(card);
    }

    public async Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var card = await context.FidelityCards
            .AsNoTracking()
            .Include(card => card.Tier)
            .WhereActiveTenant(tenantId)
            .Where(c => c.CardNumber == cardNumber)
            .FirstOrDefaultAsync(ct);

        return card is null ? null : MapCard(card);
    }

    public async Task<FidelityCardDto> CreateCardAsync(CreateFidelityCardDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        await EnsureBusinessPartyExistsAsync(dto.BusinessPartyId, tenantId, ct);

        var cardExists = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .AnyAsync(card => card.CardNumber == dto.CardNumber, ct);

        if (cardExists)
        {
            throw new InvalidOperationException("A fidelity card with the same number already exists.");
        }

        var tierId = await ResolveTierIdAsync(dto.TierId, tenantId, ct);

        var card = new FidelityCard
        {
            TenantId = tenantId,
            CardNumber = dto.CardNumber,
            TierId = tierId,
            TierEnteredAt = tierId.HasValue ? DateTime.UtcNow : null,
            Status = EventForge.Server.Data.Entities.Business.FidelityCardStatus.Active,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            DiscountPercentage = dto.DiscountPercentage,
            HasPriorityAccess = dto.HasPriorityAccess,
            HasBirthdayBonus = dto.HasBirthdayBonus,
            Notes = dto.Notes,
            BusinessPartyId = dto.BusinessPartyId,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.FidelityCards.Add(card);
        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(card, "Insert", currentUser, null, ct);

        card.Tier = tierId.HasValue
            ? await context.FidelityTiers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tierId.Value, ct)
            : null;

        return MapCard(card);
    }

    public async Task<FidelityCardDto?> UpdateCardAsync(Guid id, UpdateFidelityCardDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalCard = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        if (originalCard is null)
        {
            return null;
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (card is null)
        {
            return null;
        }

        card.TierId = await ResolveTierIdAsync(dto.TierId, tenantId, ct);
        if (card.TierId.HasValue && card.TierId != originalCard.TierId)
        {
            card.TierEnteredAt = DateTime.UtcNow;
        }
        card.ValidFrom = dto.ValidFrom;
        card.ValidTo = dto.ValidTo;
        card.DiscountPercentage = dto.DiscountPercentage;
        card.HasPriorityAccess = dto.HasPriorityAccess;
        card.HasBirthdayBonus = dto.HasBirthdayBonus;
        card.Notes = dto.Notes;
        card.ModifiedAt = DateTime.UtcNow;
        card.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, ct);

        card.Tier = card.TierId.HasValue
            ? await context.FidelityTiers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == card.TierId.Value, ct)
            : null;

        return MapCard(card);
    }

    public Task<bool> RevokeCardAsync(Guid id, string currentUser, CancellationToken ct = default) =>
        SetStatusAsync(id, EventForge.Server.Data.Entities.Business.FidelityCardStatus.Revoked, currentUser, ct);

    public Task<bool> SuspendCardAsync(Guid id, string currentUser, CancellationToken ct = default) =>
        SetStatusAsync(id, EventForge.Server.Data.Entities.Business.FidelityCardStatus.Suspended, currentUser, ct);

    public Task<bool> ActivateCardAsync(Guid id, string currentUser, CancellationToken ct = default) =>
        SetStatusAsync(id, EventForge.Server.Data.Entities.Business.FidelityCardStatus.Active, currentUser, ct);

    public async Task<FidelityPointsTransactionDto?> AddPointsAsync(Guid id, ModifyFidelityPointsDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalCard = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        if (originalCard is null)
        {
            return null;
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (card is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var transaction = new FidelityPointsTransaction
        {
            TenantId = tenantId,
            FidelityCardId = id,
            TransactionType = EventForge.Server.Data.Entities.Business.FidelityTransactionType.Earned,
            Points = dto.Points,
            Description = dto.Description,
            TransactionDate = now,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        card.CurrentPoints += dto.Points;
        card.TotalPointsEarned += dto.Points;
        card.ModifiedAt = now;
        card.ModifiedBy = currentUser;

        _ = context.FidelityPointsTransactions.Add(transaction);
        _ = await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, ct);
        _ = await auditLogService.TrackEntityChangesAsync(transaction, "Insert", currentUser, null, ct);

        // Automatic tier promotion: after points/spend are persisted, re-evaluate whether the card
        // now qualifies for a higher tier. Runs in the same request/tenant scope.
        _ = await tierEvaluationService.EvaluateUpgradeAsync(id, ct);

        return MapTransaction(transaction);
    }

    public async Task<FidelityPointsTransactionDto?> RedeemPointsAsync(Guid id, ModifyFidelityPointsDto dto, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalCard = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        if (originalCard is null)
        {
            return null;
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (card is null)
        {
            return null;
        }

        if (card.CurrentPoints < dto.Points)
        {
            throw new InvalidOperationException("Insufficient points.");
        }

        var now = DateTime.UtcNow;
        var transaction = new FidelityPointsTransaction
        {
            TenantId = tenantId,
            FidelityCardId = id,
            TransactionType = EventForge.Server.Data.Entities.Business.FidelityTransactionType.Redeemed,
            Points = dto.Points,
            Description = dto.Description,
            TransactionDate = now,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        card.CurrentPoints -= dto.Points;
        card.TotalPointsRedeemed += dto.Points;
        card.ModifiedAt = now;
        card.ModifiedBy = currentUser;

        _ = context.FidelityPointsTransactions.Add(transaction);
        _ = await context.SaveChangesAsync(ct);

        _ = await auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, ct);
        _ = await auditLogService.TrackEntityChangesAsync(transaction, "Insert", currentUser, null, ct);

        return MapTransaction(transaction);
    }

    public async Task<IEnumerable<FidelityPointsTransactionDto>> GetTransactionHistoryAsync(Guid cardId, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var transactions = await context.FidelityPointsTransactions
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .Where(transaction => transaction.FidelityCardId == cardId)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAt)
            .ToListAsync(ct);

        return transactions.Select(MapTransaction);
    }

    public async Task<bool> DeleteCardAsync(Guid id, string currentUser, CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var originalCard = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        if (originalCard is null)
        {
            return false;
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (card is null)
        {
            return false;
        }

        card.IsDeleted = true;
        card.DeletedAt = DateTime.UtcNow;
        card.DeletedBy = currentUser;
        card.ModifiedAt = DateTime.UtcNow;
        card.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(card, "Delete", currentUser, originalCard, ct);

        return true;
    }

    private Guid GetRequiredTenantId()
    {
        var tenantId = tenantContext.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for fidelity card operations.");
        }

        return tenantId.Value;
    }

    private async Task<bool> SetStatusAsync(
        Guid id,
        EventForge.Server.Data.Entities.Business.FidelityCardStatus status,
        string currentUser,
        CancellationToken ct)
    {
        var tenantId = GetRequiredTenantId();

        var originalCard = await context.FidelityCards
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

        if (originalCard is null)
        {
            return false;
        }

        var card = await context.FidelityCards
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(existing => existing.Id == id, ct);

        if (card is null)
        {
            return false;
        }

        card.Status = status;
        card.ModifiedAt = DateTime.UtcNow;
        card.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(ct);
        _ = await auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, ct);

        return true;
    }

    private async Task EnsureBusinessPartyExistsAsync(Guid? businessPartyId, Guid tenantId, CancellationToken ct)
    {
        if (!businessPartyId.HasValue)
        {
            return;
        }

        var exists = await context.BusinessParties
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .AnyAsync(party => party.Id == businessPartyId.Value, ct);

        if (!exists)
        {
            throw new InvalidOperationException("The selected business party does not exist.");
        }
    }

    private static FidelityCardDto MapCard(FidelityCard card) =>
        new()
        {
            Id = card.Id,
            CardNumber = card.CardNumber,
            TierId = card.TierId,
            TierName = card.Tier?.Name,
            TierColor = card.Tier?.Color,
            TierIcon = card.Tier?.Icon,
            Status = (Prym.DTOs.Business.Fidelity.FidelityCardStatus)card.Status,
            ValidFrom = card.ValidFrom,
            ValidTo = card.ValidTo,
            CurrentPoints = card.CurrentPoints,
            TotalPointsEarned = card.TotalPointsEarned,
            TotalPointsRedeemed = card.TotalPointsRedeemed,
            DiscountPercentage = card.DiscountPercentage,
            HasPriorityAccess = card.HasPriorityAccess,
            HasBirthdayBonus = card.HasBirthdayBonus,
            Notes = card.Notes,
            BusinessPartyId = card.BusinessPartyId,
            CreatedAt = card.CreatedAt
        };

    /// <summary>
    /// Resolves the tier to assign. When no tier is requested, defaults to the tenant's base tier
    /// (the active tier with the lowest SortOrder), if any exists.
    /// </summary>
    private async Task<Guid?> ResolveTierIdAsync(Guid? requestedTierId, Guid tenantId, CancellationToken ct)
    {
        if (requestedTierId.HasValue)
        {
            var exists = await context.FidelityTiers
                .AsNoTracking()
                .WhereActiveTenant(tenantId)
                .AnyAsync(tier => tier.Id == requestedTierId.Value, ct);

            if (!exists)
            {
                throw new InvalidOperationException("The selected fidelity tier does not exist.");
            }

            return requestedTierId.Value;
        }

        var baseTier = await context.FidelityTiers
            .AsNoTracking()
            .WhereActiveTenant(tenantId)
            .OrderBy(tier => tier.SortOrder)
            .FirstOrDefaultAsync(ct);

        return baseTier?.Id;
    }

    private static FidelityPointsTransactionDto MapTransaction(FidelityPointsTransaction transaction) =>
        new()
        {
            Id = transaction.Id,
            FidelityCardId = transaction.FidelityCardId,
            TransactionType = (Prym.DTOs.Business.Fidelity.FidelityTransactionType)transaction.TransactionType,
            Points = transaction.Points,
            Description = transaction.Description,
            TransactionDate = transaction.TransactionDate,
            CreatedAt = transaction.CreatedAt
        };
}
