using EventForge.DTOs.Store;
using EventForge.DTOs.Common;
using System.Net.Http.Json;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store user groups.
/// </summary>
public class StoreUserGroupService : IStoreUserGroupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StoreUserGroupService> _logger;
    private const string ApiBase = "api/v1/storeusers/groups";
    private const int MaxPageSize = 1000; // Maximum page size for GetAll operations

    public StoreUserGroupService(HttpClient httpClient, ILogger<StoreUserGroupService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<StoreUserGroupDto>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page=1&pageSize={MaxPageSize}");
            response.EnsureSuccessStatusCode();
            
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserGroupDto>>();
            return pagedResult?.Items?.ToList() ?? new List<StoreUserGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all store user groups");
            throw;
        }
    }

    public async Task<PagedResult<StoreUserGroupDto>> GetPagedAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page={page}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadFromJsonAsync<PagedResult<StoreUserGroupDto>>() 
                ?? new PagedResult<StoreUserGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged store user groups (page: {Page}, pageSize: {PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserGroupDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user group {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> CreateAsync(CreateStoreUserGroupDto createDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBase, createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StoreUserGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user group");
            throw;
        }
    }

    public async Task<StoreUserGroupDto?> UpdateAsync(Guid id, UpdateStoreUserGroupDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StoreUserGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user group {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{ApiBase}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store user group {Id}", id);
            return false;
        }
    }
}
