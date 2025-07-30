using System.Net.Http.Json;
using EventForge.DTOs.SuperAdmin;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing system configuration from the client.
/// </summary>
public interface IConfigurationService
{
    Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync();
    Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<ConfigurationDto?> GetConfigurationAsync(string key);
    Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto);
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto);
    Task DeleteConfigurationAsync(string key);
    Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto);
    Task ReloadConfigurationAsync();
}

public class ConfigurationService : IConfigurationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(IHttpClientFactory httpClientFactory, ILogger<ConfigurationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        return httpClient;
    }

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync()
    {
        try
        {
            var httpClient = CreateHttpClient();
            var response = await httpClient.GetFromJsonAsync<IEnumerable<ConfigurationDto>>("api/SuperAdmin/configuration");
            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations");
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
    {
        try
        {
            var response = await CreateHttpClient().GetFromJsonAsync<IEnumerable<ConfigurationDto>>($"api/SuperAdmin/configuration/category/{category}");
            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        try
        {
            var response = await CreateHttpClient().GetFromJsonAsync<IEnumerable<string>>("api/SuperAdmin/configuration/categories");
            return response ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration categories");
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key)
    {
        try
        {
            return await CreateHttpClient().GetFromJsonAsync<ConfigurationDto>($"api/SuperAdmin/configuration/{key}");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Key}", key);
            throw;
        }
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto)
    {
        try
        {
            var response = await CreateHttpClient().PostAsJsonAsync("api/SuperAdmin/configuration", createDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConfigurationDto>();
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration");
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto)
    {
        try
        {
            var response = await CreateHttpClient().PutAsJsonAsync($"api/SuperAdmin/configuration/{key}", updateDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ConfigurationDto>();
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Key}", key);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(string key)
    {
        try
        {
            var response = await CreateHttpClient().DeleteAsync($"api/SuperAdmin/configuration/{key}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            throw;
        }
    }

    public async Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto)
    {
        try
        {
            var response = await CreateHttpClient().PostAsJsonAsync("api/SuperAdmin/configuration/test-smtp", testDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SmtpTestResultDto>();
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMTP");
            throw;
        }
    }

    public async Task ReloadConfigurationAsync()
    {
        try
        {
            var response = await CreateHttpClient().PostAsync("api/SuperAdmin/configuration/reload", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            throw;
        }
    }
}