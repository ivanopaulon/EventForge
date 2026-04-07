using EventForge.DTOs.Business;

namespace EventForge.Server.Services.Business;

public interface IBusinessPartyGroupService
{
    // CRUD Gruppi
    Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(
        int page = 1,
        int pageSize = 20,
        BusinessPartyGroupType? groupType = null,
        CancellationToken cancellationToken = default);

    Task<BusinessPartyGroupDto?> GetGroupByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BusinessPartyGroupDto> CreateGroupAsync(
        CreateBusinessPartyGroupDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    Task<BusinessPartyGroupDto> UpdateGroupAsync(
        Guid id,
        UpdateBusinessPartyGroupDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGroupAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Gestione Members
    Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(
        Guid groupId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<BusinessPartyGroupMemberDto> AddMemberToGroupAsync(
        Guid groupId,
        AddBusinessPartyToGroupDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    Task<BulkOperationResultDto> BulkAddMembersAsync(
        BulkAddMembersDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveMemberFromGroupAsync(
        Guid groupId,
        Guid businessPartyId,
        string currentUser,
        CancellationToken cancellationToken = default);

    Task<BusinessPartyGroupMemberDto> UpdateMembershipAsync(
        Guid membershipId,
        UpdateBusinessPartyGroupMemberDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Query Helpers
    Task<List<BusinessPartyGroupDto>> GetGroupsForBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetActiveGroupIdsForBusinessPartyAsync(
        Guid businessPartyId,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsBusinessPartyInGroupAsync(
        Guid businessPartyId,
        Guid groupId,
        CancellationToken cancellationToken = default);
}
