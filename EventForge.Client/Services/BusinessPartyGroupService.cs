using Prym.DTOs.Business;
using Prym.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing Business Party Groups.
/// </summary>
public class BusinessPartyGroupService(
    IHttpClientService httpClientService,
    ILogger<BusinessPartyGroupService> logger) : IBusinessPartyGroupService
{
    private const string BaseUrl = "api/v1/business-party-groups";

    public async Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 20, BusinessPartyGroupType? groupType = null, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}?page={page}&pageSize={pageSize}";
            if (groupType.HasValue)
            {
                url += $"&groupType={groupType.Value}";
            }

            var result = await httpClientService.GetAsync<PagedResult<BusinessPartyGroupDto>>(url, ct);
            return result ?? new PagedResult<BusinessPartyGroupDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving business party groups");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<BusinessPartyGroupDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving business party group with ID {Id}", id);
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateBusinessPartyGroupDto, BusinessPartyGroupDto>(BaseUrl, createDto, ct);
            return result ?? throw new InvalidOperationException("Failed to create business party group");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating business party group");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto?> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateBusinessPartyGroupDto, BusinessPartyGroupDto>($"{BaseUrl}/{id}", updateDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating business party group with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting business party group with ID {Id}", id);
            throw;
        }
    }

    // Member Management Methods

    public async Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(Guid groupId, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var url = $"{BaseUrl}/{groupId}/members?page={page}&pageSize={pageSize}";
            var result = await httpClientService.GetAsync<PagedResult<BusinessPartyGroupMemberDto>>(url, ct);
            return result ?? new PagedResult<BusinessPartyGroupMemberDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving members for business party group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> AddMemberAsync(Guid groupId, AddBusinessPartyToGroupDto createDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<AddBusinessPartyToGroupDto, BusinessPartyGroupMemberDto>(
                $"{BaseUrl}/{groupId}/members",
                createDto,
                ct);
            return result ?? throw new InvalidOperationException("Failed to add member to business party group");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding member to business party group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<BulkOperationResultDto> AddMembersBulkAsync(BulkAddMembersDto bulkDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<BulkAddMembersDto, BulkOperationResultDto>(
                $"{BaseUrl}/bulk-add-members",
                bulkDto,
                ct);
            return result ?? throw new InvalidOperationException("Failed to bulk add members to business party group");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk adding members to business party group {GroupId}", bulkDto.BusinessPartyGroupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> UpdateMemberAsync(Guid membershipId, UpdateBusinessPartyGroupMemberDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateBusinessPartyGroupMemberDto, BusinessPartyGroupMemberDto>(
                $"{BaseUrl}/members/{membershipId}",
                updateDto,
                ct);
            return result ?? throw new InvalidOperationException("Failed to update member");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating membership {MembershipId}", membershipId);
            throw;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid businessPartyId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{groupId}/members/{businessPartyId}", ct);
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing member {BusinessPartyId} from group {GroupId}", businessPartyId, groupId);
            throw;
        }
    }
}
