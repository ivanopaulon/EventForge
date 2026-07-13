using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;


namespace EventForge.Server.Services.Teams;

public partial class TeamService
{
    private static TeamDto MapToTeamDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            MemberCount = team.Members.Count(m => !m.IsDeleted),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    private static TeamDetailDto MapToTeamDetailDto(Team team)
    {
        return new TeamDetailDto
        {
            Id = team.Id,
            Name = team.Name,
            ShortDescription = team.ShortDescription,
            LongDescription = team.LongDescription,
            Email = team.Email,
            EventId = team.EventId,
            EventName = team.Event?.Name,
            Members = team.Members.Where(m => !m.IsDeleted).Select(MapToTeamMemberDto).ToList(),
            CreatedAt = team.CreatedAt,
            CreatedBy = team.CreatedBy,
            ModifiedAt = team.ModifiedAt,
            ModifiedBy = team.ModifiedBy
        };
    }

    /// <summary>
    /// Converts the entity-side TeamMemberStatus (Active/Suspended/Retired/Excluded) to the
    /// DTO-side TeamMemberStatus (Active/Suspended/Inactive). Retired/Excluded map to Inactive
    /// since the DTO enum has no equivalent distinct values.
    /// </summary>
    private static Prym.DTOs.Common.TeamMemberStatus ToDtoTeamMemberStatus(EventForge.Server.Data.Entities.Teams.TeamMemberStatus status) => status switch
    {
        EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Active => Prym.DTOs.Common.TeamMemberStatus.Active,
        EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Suspended => Prym.DTOs.Common.TeamMemberStatus.Suspended,
        _ => Prym.DTOs.Common.TeamMemberStatus.Inactive
    };

    /// <summary>
    /// Converts the DTO-side TeamMemberStatus (Active/Suspended/Inactive) to the entity-side
    /// TeamMemberStatus (Active/Suspended/Retired/Excluded). Inactive maps to Retired since that
    /// is the closest analog on the entity side.
    /// </summary>
    private static EventForge.Server.Data.Entities.Teams.TeamMemberStatus ToEntityTeamMemberStatus(Prym.DTOs.Common.TeamMemberStatus status) => status switch
    {
        Prym.DTOs.Common.TeamMemberStatus.Active => EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Active,
        Prym.DTOs.Common.TeamMemberStatus.Suspended => EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Suspended,
        _ => EventForge.Server.Data.Entities.Teams.TeamMemberStatus.Retired
    };

    private static TeamMemberDto MapToTeamMemberDto(TeamMember member)
    {
        return new TeamMemberDto
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Role = member.Role,
            DateOfBirth = member.DateOfBirth,
            Status = ToDtoTeamMemberStatus(member.Status),
            FiscalCode = member.FiscalCode,
            TeamId = member.TeamId,
            TeamName = member.Team?.Name,
            Position = member.Position,
            JerseyNumber = member.JerseyNumber,
            EligibilityStatus = member.EligibilityStatus,
            PhotoDocumentId = member.PhotoDocumentId,
            PhotoUrl = member.PhotoDocument?.Url,
            PhotoConsent = member.PhotoConsent,
            PhotoConsentAt = member.PhotoConsentAt,
            Age = member.Age,
            IsMinor = member.IsMinor,
            CreatedAt = member.CreatedAt,
            CreatedBy = member.CreatedBy,
            ModifiedAt = member.ModifiedAt,
            ModifiedBy = member.ModifiedBy
        };
    }

    private static DocumentReferenceDto MapToDocumentReferenceDto(DocumentReference document)
    {
        return new DocumentReferenceDto
        {
            Id = document.Id,
            OwnerId = document.OwnerId,
            OwnerType = document.OwnerType,
            FileName = document.FileName,
            Type = document.Type,
            SubType = document.SubType,
            MimeType = document.MimeType,
            StorageKey = document.StorageKey,
            Url = document.Url,
            ThumbnailStorageKey = document.ThumbnailStorageKey,
            Expiry = document.Expiry,
            FileSizeBytes = document.FileSizeBytes,
            Title = document.Title,
            Notes = document.Notes,
            CreatedAt = document.CreatedAt,
            CreatedBy = document.CreatedBy,
            ModifiedAt = document.ModifiedAt,
            ModifiedBy = document.ModifiedBy
        };
    }

    private static MembershipCardDto MapToMembershipCardDto(MembershipCard card)
    {
        return new MembershipCardDto
        {
            Id = card.Id,
            TeamMemberId = card.TeamMemberId,
            TeamMemberName = card.TeamMember is not null ? $"{card.TeamMember.FirstName} {card.TeamMember.LastName}" : null,
            CardNumber = card.CardNumber,
            Federation = card.Federation,
            ValidFrom = card.ValidFrom,
            ValidTo = card.ValidTo,
            DocumentReferenceId = card.DocumentReferenceId,
            Category = card.Category,
            Notes = card.Notes,
            CreatedAt = card.CreatedAt,
            CreatedBy = card.CreatedBy,
            ModifiedAt = card.ModifiedAt,
            ModifiedBy = card.ModifiedBy
        };
    }

    private static InsurancePolicyDto MapToInsurancePolicyDto(InsurancePolicy policy)
    {
        return new InsurancePolicyDto
        {
            Id = policy.Id,
            TeamMemberId = policy.TeamMemberId,
            TeamMemberName = policy.TeamMember is not null ? $"{policy.TeamMember.FirstName} {policy.TeamMember.LastName}" : null,
            Provider = policy.Provider,
            PolicyNumber = policy.PolicyNumber,
            ValidFrom = policy.ValidFrom,
            ValidTo = policy.ValidTo,
            CoverageType = policy.CoverageType,
            CoverageAmount = policy.CoverageAmount,
            Currency = policy.Currency,
            DocumentReferenceId = policy.DocumentReferenceId,
            Notes = policy.Notes,
            CreatedAt = policy.CreatedAt,
            CreatedBy = policy.CreatedBy,
            ModifiedAt = policy.ModifiedAt,
            ModifiedBy = policy.ModifiedBy
        };
    }
}
