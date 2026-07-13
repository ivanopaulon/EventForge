using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<IEnumerable<MembershipCardDto>> GetMembershipCardsByMemberAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var cards = await context.MembershipCards
            .AsNoTracking()
            .Where(mc => mc.TeamMemberId == teamMemberId && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .Include(mc => mc.DocumentReference)
            .OrderBy(mc => mc.ValidFrom)
            .ToListAsync(cancellationToken);

        return cards.Select(MapToMembershipCardDto);
    }

    public async Task<MembershipCardDto?> GetMembershipCardByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var card = await context.MembershipCards
            .AsNoTracking()
            .Where(mc => mc.Id == id && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .Include(mc => mc.DocumentReference)
            .Include(mc => mc.TeamMember)
            .FirstOrDefaultAsync(cancellationToken);

        return card is not null ? MapToMembershipCardDto(card) : null;
    }

    public async Task<MembershipCardDto> CreateMembershipCardAsync(CreateMembershipCardDto createMembershipCardDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createMembershipCardDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var card = new MembershipCard
        {
            TeamMemberId = createMembershipCardDto.TeamMemberId,
            CardNumber = createMembershipCardDto.CardNumber,
            Federation = createMembershipCardDto.Federation,
            ValidFrom = createMembershipCardDto.ValidFrom,
            ValidTo = createMembershipCardDto.ValidTo,
            DocumentReferenceId = createMembershipCardDto.DocumentReferenceId,
            Category = createMembershipCardDto.Category,
            Notes = createMembershipCardDto.Notes,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required")
        };

        _ = context.MembershipCards.Add(card);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(card, "Insert", currentUser, null, cancellationToken);

        var createdCard = await context.MembershipCards
            .Include(mc => mc.DocumentReference)
            .Include(mc => mc.TeamMember)
            .FirstAsync(mc => mc.Id == card.Id, cancellationToken);

        return MapToMembershipCardDto(createdCard);
    }

    public async Task<MembershipCardDto?> UpdateMembershipCardAsync(Guid id, UpdateMembershipCardDto updateMembershipCardDto, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateMembershipCardDto);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var card = await context.MembershipCards
            .Where(mc => mc.Id == id && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .Include(mc => mc.DocumentReference)
            .Include(mc => mc.TeamMember)
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            logger.LogWarning("Membership card {CardId} not found for update", id);
            return null;
        }

        var originalCard = await context.MembershipCards
            .AsNoTracking()
            .Where(mc => mc.Id == id && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        card.CardNumber = updateMembershipCardDto.CardNumber ?? card.CardNumber;
        card.Federation = updateMembershipCardDto.Federation ?? card.Federation;
        card.ValidFrom = updateMembershipCardDto.ValidFrom ?? card.ValidFrom;
        card.ValidTo = updateMembershipCardDto.ValidTo ?? card.ValidTo;
        card.DocumentReferenceId = updateMembershipCardDto.DocumentReferenceId;
        card.Category = updateMembershipCardDto.Category;
        card.Notes = updateMembershipCardDto.Notes;
        card.ModifiedBy = currentUser;
        card.ModifiedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(card, "Update", currentUser, originalCard, cancellationToken);

        return MapToMembershipCardDto(card);
    }

    public async Task<bool> DeleteMembershipCardAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var card = await context.MembershipCards
            .Where(mc => mc.Id == id && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            logger.LogWarning("Membership card {CardId} not found for deletion", id);
            return false;
        }

        var originalCard = await context.MembershipCards
            .AsNoTracking()
            .Where(mc => mc.Id == id && mc.TenantId == currentTenantId && !mc.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        card.IsDeleted = true;
        card.DeletedBy = currentUser;
        card.DeletedAt = DateTime.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(card, "Delete", currentUser, originalCard, cancellationToken);

        return true;
    }

    // Insurance Policy operations

}
