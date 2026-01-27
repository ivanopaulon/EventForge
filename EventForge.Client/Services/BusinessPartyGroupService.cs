using EventForge.DTOs.Common;
using EventForge.DTOs.Business;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing business party groups.
/// </summary>
public class BusinessPartyGroupService : IBusinessPartyGroupService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BusinessPartyGroupService> _logger;
    private const string BaseUrl = "api/v1/business-party-groups";

    public BusinessPartyGroupService(IHttpClientService httpClientService, ILogger<BusinessPartyGroupService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 20, BusinessPartyGroupType? groupType = null)
    {
        try
        {
            var url = $"{BaseUrl}?page={page}&pageSize={pageSize}";
            if (groupType.HasValue)
            {
                url += $"&groupType={groupType.Value}";
            }

            var result = await _httpClientService.GetAsync<PagedResult<BusinessPartyGroupDto>>(url);
            return result ?? new PagedResult<BusinessPartyGroupDto> { Items = new List<BusinessPartyGroupDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party groups");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<BusinessPartyGroupDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party group with ID {Id}", id);
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto, string currentUser)
    {
        try
        {
            var result = await _httpClientService.PostAsync<BusinessPartyGroupDto>($"{BaseUrl}?currentUser={currentUser}", createDto);
            return result ?? throw new InvalidOperationException("Failed to create business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business party group");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto?> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto, string currentUser)
    {
        try
        {
            return await _httpClientService.PutAsync<BusinessPartyGroupDto>($"{BaseUrl}/{id}?currentUser={currentUser}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business party group with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id, string currentUser)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}?currentUser={currentUser}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business party group with ID {Id}", id);
            return false;
        }
    }

    public async Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(Guid groupId, int page = 1, int pageSize = 100)
    {
        try
        {
            var url = $"{BaseUrl}/{groupId}/members?page={page}&pageSize={pageSize}";
            var result = await _httpClientService.GetAsync<PagedResult<BusinessPartyGroupMemberDto>>(url);
            return result ?? new PagedResult<BusinessPartyGroupMemberDto> { Items = new List<BusinessPartyGroupMemberDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for business party group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> AddMemberAsync(Guid groupId, AddBusinessPartyToGroupDto addDto, string currentUser)
    {
        try
        {
            var result = await _httpClientService.PostAsync<BusinessPartyGroupMemberDto>($"{BaseUrl}/{groupId}/members?currentUser={currentUser}", addDto);
            return result ?? throw new InvalidOperationException("Failed to add member to business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to business party group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid memberId, string currentUser)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{groupId}/members/{memberId}?currentUser={currentUser}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {MemberId} from business party group {GroupId}", memberId, groupId);
            return false;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> UpdateMembershipAsync(Guid membershipId, UpdateBusinessPartyGroupMemberDto updateDto, string currentUser)
    {
        try
        {
            var result = await _httpClientService.PutAsync<BusinessPartyGroupMemberDto>($"{BaseUrl}/members/{membershipId}?currentUser={currentUser}", updateDto);
            return result ?? throw new InvalidOperationException("Failed to update membership");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership {MembershipId}", membershipId);
            throw;
        }
    }
}
