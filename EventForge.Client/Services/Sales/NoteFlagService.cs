using EventForge.DTOs.Sales;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for note flags.
/// </summary>
public class NoteFlagService : INoteFlagService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NoteFlagService> _logger;
    private const string BaseUrl = "api/v1/note-flags";

    public NoteFlagService(IHttpClientFactory httpClientFactory, ILogger<NoteFlagService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<NoteFlagDto>?> GetAllAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<NoteFlagDto>>(BaseUrl, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all note flags");
            return null;
        }
    }

    public async Task<List<NoteFlagDto>?> GetActiveAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<NoteFlagDto>>($"{BaseUrl}/active", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active note flags");
            return null;
        }
    }

    public async Task<NoteFlagDto?> GetByIdAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NoteFlagDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve note flag {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving note flag {Id}", id);
            return null;
        }
    }

    public async Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NoteFlagDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create note flag. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note flag");
            return null;
        }
    }

    public async Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NoteFlagDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update note flag {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note flag {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note flag {Id}", id);
            return false;
        }
    }
}
