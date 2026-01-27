using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service for managing Business Party Groups.
/// </summary>
public interface IBusinessPartyGroupService
{
    // Group Management
    Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 100, BusinessPartyGroupType? groupType = null);
    Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id);
    Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto);
    Task<BusinessPartyGroupDto> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto);
    Task<bool> DeleteGroupAsync(Guid id);
    
    // Member Management
    Task<PagedResult<BusinessPartyGroupMemberDto>> GetGroupMembersAsync(Guid groupId, int page = 1, int pageSize = 100);
    Task<BusinessPartyGroupMemberDto> AddMemberAsync(Guid groupId, AddBusinessPartyToGroupDto createDto);
    Task<BulkOperationResultDto> AddMembersBulkAsync(BulkAddMembersDto bulkDto);
    Task<BusinessPartyGroupMemberDto> UpdateMemberAsync(Guid membershipId, UpdateBusinessPartyGroupMemberDto updateDto);
    Task<bool> RemoveMemberAsync(Guid groupId, Guid businessPartyId);
}

/// <summary>
/// Service implementation for managing Business Party Groups.
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

    public async Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 100, BusinessPartyGroupType? groupType = null)
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

    public async Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateBusinessPartyGroupDto, BusinessPartyGroupDto>(BaseUrl, createDto);
            return result ?? throw new InvalidOperationException("Failed to create business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business party group");
            throw;
        }
    }

    public async Task<BusinessPartyGroupDto> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateBusinessPartyGroupDto, BusinessPartyGroupDto>($"{BaseUrl}/{id}", updateDto);
            return result ?? throw new InvalidOperationException("Failed to update business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business party group with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteGroupAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business party group with ID {Id}", id);
            throw;
        }
    }

    // Member Management Methods

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

    public async Task<BusinessPartyGroupMemberDto> AddMemberAsync(Guid groupId, AddBusinessPartyToGroupDto createDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<AddBusinessPartyToGroupDto, BusinessPartyGroupMemberDto>(
                $"{BaseUrl}/{groupId}/members", 
                createDto);
            return result ?? throw new InvalidOperationException("Failed to add member to business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to business party group {GroupId}", groupId);
            throw;
        }
    }

    public async Task<BulkOperationResultDto> AddMembersBulkAsync(BulkAddMembersDto bulkDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<BulkAddMembersDto, BulkOperationResultDto>(
                $"{BaseUrl}/bulk-add-members", 
                bulkDto);
            return result ?? throw new InvalidOperationException("Failed to bulk add members to business party group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk adding members to business party group {GroupId}", bulkDto.BusinessPartyGroupId);
            throw;
        }
    }

    public async Task<BusinessPartyGroupMemberDto> UpdateMemberAsync(Guid membershipId, UpdateBusinessPartyGroupMemberDto updateDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateBusinessPartyGroupMemberDto, BusinessPartyGroupMemberDto>(
                $"{BaseUrl}/members/{membershipId}", 
                updateDto);
            return result ?? throw new InvalidOperationException("Failed to update member");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership {MembershipId}", membershipId);
            throw;
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid businessPartyId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{groupId}/members/{businessPartyId}");
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {BusinessPartyId} from group {GroupId}", businessPartyId, groupId);
            throw;
        }
    }
}
