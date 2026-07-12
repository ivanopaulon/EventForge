using Prym.DTOs.Common;
using Prym.DTOs.Teams;

namespace Prym.Web.Services.Teams;

/// <summary>
/// Client service for team management operations.
/// </summary>
public interface ITeamService
{
    // Team CRUD
    Task<PagedResult<TeamDto>?> GetTeamsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<TeamDto?> GetTeamByIdAsync(Guid id, CancellationToken ct = default);
    Task<TeamDetailDto?> GetTeamDetailAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<TeamDto>?> GetTeamsByEventAsync(Guid eventId, CancellationToken ct = default);
    Task<TeamDto?> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default);
    Task<TeamDto?> UpdateTeamAsync(Guid id, UpdateTeamDto dto, CancellationToken ct = default);
    Task DeleteTeamAsync(Guid id, CancellationToken ct = default);

    // Team Member CRUD
    Task<IEnumerable<TeamMemberDto>?> GetTeamMembersAsync(Guid teamId, CancellationToken ct = default);
    Task<TeamMemberDto?> GetTeamMemberByIdAsync(Guid memberId, CancellationToken ct = default);
    Task<TeamMemberDto?> CreateTeamMemberAsync(CreateTeamMemberDto dto, CancellationToken ct = default);
    Task<TeamMemberDto?> UpdateTeamMemberAsync(Guid memberId, UpdateTeamMemberDto dto, CancellationToken ct = default);
    Task DeleteTeamMemberAsync(Guid memberId, CancellationToken ct = default);

    // Membership Card CRUD
    Task<IEnumerable<MembershipCardDto>?> GetMembershipCardsAsync(Guid memberId, CancellationToken ct = default);
    Task<MembershipCardDto?> GetMembershipCardByIdAsync(Guid id, CancellationToken ct = default);
    Task<MembershipCardDto?> CreateMembershipCardAsync(CreateMembershipCardDto dto, CancellationToken ct = default);
    Task<MembershipCardDto?> UpdateMembershipCardAsync(Guid id, UpdateMembershipCardDto dto, CancellationToken ct = default);
    Task DeleteMembershipCardAsync(Guid id, CancellationToken ct = default);

    // Insurance Policy CRUD
    Task<IEnumerable<InsurancePolicyDto>?> GetInsurancePoliciesAsync(Guid memberId, CancellationToken ct = default);
    Task<InsurancePolicyDto?> GetInsurancePolicyByIdAsync(Guid id, CancellationToken ct = default);
    Task<InsurancePolicyDto?> CreateInsurancePolicyAsync(CreateInsurancePolicyDto dto, CancellationToken ct = default);
    Task<InsurancePolicyDto?> UpdateInsurancePolicyAsync(Guid id, UpdateInsurancePolicyDto dto, CancellationToken ct = default);
    Task DeleteInsurancePolicyAsync(Guid id, CancellationToken ct = default);

    // Document References
    Task<IEnumerable<DocumentReferenceDto>?> GetDocumentsByOwnerAsync(Guid ownerId, string ownerType, CancellationToken ct = default);
    Task<DocumentReferenceDto?> UpdateDocumentReferenceAsync(Guid id, UpdateDocumentReferenceDto dto, CancellationToken ct = default);
    Task DeleteDocumentReferenceAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Uploads a file and creates a DocumentReference for it, for the given owner.
    /// </summary>
    Task<DocumentReferenceDto?> UploadDocumentAsync(
        Microsoft.AspNetCore.Components.Forms.IBrowserFile file,
        Guid ownerId,
        string ownerType,
        Prym.DTOs.Common.DocumentReferenceType type,
        Prym.DTOs.Common.DocumentReferenceSubType subType = Prym.DTOs.Common.DocumentReferenceSubType.None,
        DateTime? expiry = null,
        string? title = null,
        string? notes = null,
        CancellationToken ct = default);

    // Fiscal Code Conflicts
    Task<List<TeamMemberDto>> GetFiscalCodeConflictsAsync(string fiscalCode, Guid excludeMemberId, CancellationToken ct = default);
}
