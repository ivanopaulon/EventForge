using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Client-side service for managing Business Party Groups.
/// </summary>
public interface IBusinessPartyGroupService
{
    Task<PagedResult<BusinessPartyGroupDto>> GetGroupsAsync(int page = 1, int pageSize = 100, BusinessPartyGroupType? groupType = null);
    Task<BusinessPartyGroupDto?> GetGroupByIdAsync(Guid id);
    Task<BusinessPartyGroupDto> CreateGroupAsync(CreateBusinessPartyGroupDto createDto);
    Task<BusinessPartyGroupDto> UpdateGroupAsync(Guid id, UpdateBusinessPartyGroupDto updateDto);
    Task<bool> DeleteGroupAsync(Guid id);
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
}
