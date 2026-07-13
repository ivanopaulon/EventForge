using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    public async Task<bool> ValidateJerseyNumberAsync(Guid teamId, int jerseyNumber, Guid? excludeTeamMemberId = null, CancellationToken cancellationToken = default)
    {
        var query = context.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.TeamId == teamId && tm.JerseyNumber == jerseyNumber && !tm.IsDeleted);

        if (excludeTeamMemberId.HasValue)
        {
            query = query.Where(tm => tm.Id != excludeTeamMemberId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        return !exists; // Return true if number is available (not taken)
    }

    public async Task<List<TeamMemberDto>> GetOtherActiveTeamsForFiscalCodeAsync(string fiscalCode, Guid excludeTeamMemberId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fiscalCode);

        // Non-blocking check: looks for other TeamMembers sharing the same fiscal code,
        // active, and belonging to a different team than the excluded member's own team.
        // This never prevents saving — the caller (client) is responsible for surfacing the warning.
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for team operations.");

        var excludedMemberTeamId = await context.TeamMembers
            .AsNoTracking()
            .Where(m => m.Id == excludeTeamMemberId && m.TenantId == currentTenantId && !m.IsDeleted)
            .Select(m => (Guid?)m.TeamId)
            .FirstOrDefaultAsync(cancellationToken);

        var query = context.TeamMembers
            .AsNoTracking()
            .Where(m => !m.IsDeleted
                && m.FiscalCode == fiscalCode
                && m.Status == EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Active
                && m.Id != excludeTeamMemberId
                && m.TenantId == currentTenantId);

        if (excludedMemberTeamId.HasValue)
        {
            query = query.Where(m => m.TeamId != excludedMemberTeamId.Value);
        }

        var conflicts = await query
            .Include(m => m.Team)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return conflicts.Select(MapToTeamMemberDto).ToList();
    }

    public async Task<IEnumerable<TeamMemberDto>> GetMembersWithExpiringDocumentsAsync(int daysBeforeExpiry = 30, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        var membersWithExpiringDocs = await context.TeamMembers
            .AsNoTracking()
            .Where(tm => !tm.IsDeleted)
            .Include(tm => tm.Team)
            .Include(tm => tm.MembershipCards.Where(mc => !mc.IsDeleted && mc.ValidTo <= expiryDate))
            .Include(tm => tm.InsurancePolicies.Where(ip => !ip.IsDeleted && ip.ValidTo <= expiryDate))
            .Where(tm => tm.MembershipCards.Any(mc => !mc.IsDeleted && mc.ValidTo <= expiryDate) ||
                        tm.InsurancePolicies.Any(ip => !ip.IsDeleted && ip.ValidTo <= expiryDate))
            .ToListAsync(cancellationToken);

        return membersWithExpiringDocs.Select(MapToTeamMemberDto);
    }

    public async Task<EligibilityValidationResult> ValidateTeamMemberEligibilityAsync(Guid teamMemberId, CancellationToken cancellationToken = default)
    {
        var result = new EligibilityValidationResult
        {
            IsEligible = true,
            ValidatedAt = DateTime.UtcNow
        };

        var member = await context.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.Id == teamMemberId && !tm.IsDeleted)
            .Include(tm => tm.MembershipCards.Where(mc => !mc.IsDeleted))
            .Include(tm => tm.InsurancePolicies.Where(ip => !ip.IsDeleted))
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            result.IsEligible = false;
            result.Issues.Add(new EligibilityIssue
            {
                Type = EligibilityIssueType.InvalidData,
                Severity = EligibilityIssueSeverity.Critical,
                Description = "Team member not found",
                Field = "TeamMemberId"
            });
            return result;
        }

        // Check for valid membership card
        var validMembershipCard = member.MembershipCards
            .FirstOrDefault(mc => mc.IsValid);

        if (validMembershipCard is null)
        {
            result.IsEligible = false;
            result.Issues.Add(new EligibilityIssue
            {
                Type = EligibilityIssueType.MissingDocument,
                Severity = EligibilityIssueSeverity.Error,
                Description = "No valid membership card found",
                Field = "MembershipCard",
                SuggestedAction = "Add a valid membership card"
            });
        }

        // Check for valid insurance policy
        var validInsurancePolicy = member.InsurancePolicies
            .FirstOrDefault(ip => ip.IsValid);

        if (validInsurancePolicy is null)
        {
            result.Issues.Add(new EligibilityIssue
            {
                Type = EligibilityIssueType.MissingDocument,
                Severity = EligibilityIssueSeverity.Warning,
                Description = "No valid insurance policy found",
                Field = "InsurancePolicy",
                SuggestedAction = "Add a valid insurance policy"
            });
        }

        // Check for expiring documents
        var expiringCards = member.MembershipCards
            .Where(mc => mc.DaysUntilExpiration <= 30 && mc.DaysUntilExpiration > 0);

        foreach (var card in expiringCards)
        {
            result.Warnings.Add(new EligibilityIssue
            {
                Type = EligibilityIssueType.ExpiredDocument,
                Severity = EligibilityIssueSeverity.Warning,
                Description = $"Membership card expires in {card.DaysUntilExpiration} days",
                Field = "MembershipCard",
                DueDate = card.ValidTo,
                SuggestedAction = "Renew membership card before expiry"
            });
        }

        return result;
    }

    // Private mapping methods

}
