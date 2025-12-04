using EventForge.DTOs.Store;
using System.Net.Http.Json;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service implementation for managing store users.
/// </summary>
public class StoreUserService : IStoreUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StoreUserService> _logger;
    private const string ApiBase = "api/v1/storeusers";

    public StoreUserService(HttpClient httpClient, ILogger<StoreUserService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<StoreUserDto>> GetAllAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBase}?page=1&pageSize=1000");
            response.EnsureSuccessStatusCode();
            
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<StoreUserDto>>();
            return pagedResult?.Items?.ToList() ?? new List<StoreUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all store users");
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user {Id}", id);
            throw;
        }
    }

    public async Task<StoreUserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreUserDto>($"{ApiBase}/by-username/{username}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting store user by username {Username}", username);
            throw;
        }
    }

    public async Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiBase, createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StoreUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating store user");
            throw;
        }
    }

    public async Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{ApiBase}/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<StoreUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating store user {Id}", id);
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
            _logger.LogError(ex, "Error deleting store user {Id}", id);
            throw;
        }
    }
}

/// <summary>
/// Helper class for paginated results.
/// </summary>
public class PagedResult<T>
{
    public List<T>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
