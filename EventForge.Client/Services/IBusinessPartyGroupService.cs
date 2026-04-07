using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing business party groups.
/// </summary>
public interface IBusinessPartyGroupService
{
    /// <summary>
    /// Gets all business party groups with optional pagination and filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="groupType">Optional filter by group type</param>
    /// <returns>Paginated list of business party groups</returns>
    Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 20, BusinessPartyGroupType? groupType = null);

    /// <summary>
    /// Gets a business party group by ID.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <returns>Business party group DTO or null if not found</returns>
    Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id);

    /// <summary>
    /// Creates a new business party group.
    /// </summary>
    /// <param name="createDto">Group creation data</param>
    /// <returns>Created group DTO</returns>
    Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto);

    /// <summary>
    /// Updates an existing business party group.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="updateDto">Group update data</param>
    /// <returns>Updated group DTO or null if not found</returns>
    Task<BusinessPartyGroupDto?> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto);

    /// <summary>
    /// Deletes a business party group.
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteGroupAsync(Guid id);

    /// <summary>
    /// Gets members of a business party group with pagination.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of group members</returns>
    Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(Guid groupId, int page = 1, int pageSize = 100);

    /// <summary>
    /// Adds a member to a business party group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="addDto">Member addition data</param>
    /// <returns>Created member DTO</returns>
    Task<BusinessPartyGroupMemberDto> AddMemberAsync(Guid groupId, AddBusinessPartyToGroupDto addDto);

    /// <summary>
    /// Adds multiple members to a business party group in a single operation.
    /// </summary>
    /// <param name="bulkDto">Bulk add members data</param>
    /// <returns>Bulk operation result with success/failure counts</returns>
    Task<BulkOperationResultDto> AddMembersBulkAsync(BulkAddMembersDto bulkDto);

    /// <summary>
    /// Removes a member from a business party group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="businessPartyId">Business Party ID to remove</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveMemberAsync(Guid groupId, Guid businessPartyId);

    /// <summary>
    /// Updates a group membership.
    /// </summary>
    /// <param name="membershipId">Membership ID</param>
    /// <param name="updateDto">Membership update data</param>
    /// <returns>Updated member DTO</returns>
    Task<BusinessPartyGroupMemberDto> UpdateMemberAsync(Guid membershipId, UpdateBusinessPartyGroupMemberDto updateDto);
}
