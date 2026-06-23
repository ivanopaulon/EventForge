using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Services.Business;

public class FidelityCardService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext) : IFidelityCardService
{
    public async Task<IEnumerable<FidelityCardDto>> GetAllCardsAsync(CancellationToken ct = default)
    {
        var tenantId = GetRequiredTenantId();

        var cards = await context.FidelityCards
            .AsNoTracking()
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
            .WhereActiveTenant(tenantId)
            .FirstOrDefaultAsync(card => card.Id == id, ct);

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

        var card = new FidelityCard
        {
            TenantId = tenantId,
            CardNumber = dto.CardNumber,
            Type = (EventForge.Server.Data.Entities.Business.FidelityCardType)dto.Type,
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

        card.Type = (EventForge.Server.Data.Entities.Business.FidelityCardType)dto.Type;
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
            Type = (Prym.DTOs.Business.Fidelity.FidelityCardType)card.Type,
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
