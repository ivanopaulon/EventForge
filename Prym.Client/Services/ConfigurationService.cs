using Prym.DTOs.SuperAdmin;

namespace Prym.Client.Services;

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

public class ConfigurationService(
    IHttpClientService httpClientService,
    ILogger<ConfigurationService> logger) : IConfigurationService
{

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync()
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<ConfigurationDto>>("api/v1/super-admin/configuration");
            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configurations");
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<ConfigurationDto>>($"api/v1/super-admin/configuration/category/{category}");
            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configurations for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<string>>("api/v1/super-admin/configuration/categories");
            return response ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configuration categories");
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key)
    {
        try
        {
            return await httpClientService.GetAsync<ConfigurationDto>($"api/v1/super-admin/configuration/{key}");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configuration {Key}", key);
            throw;
        }
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateConfigurationDto, ConfigurationDto>("api/v1/super-admin/configuration", createDto);
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating configuration");
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateConfigurationDto, ConfigurationDto>($"api/v1/super-admin/configuration/{key}", updateDto);
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating configuration {Key}", key);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(string key)
    {
        try
        {
            await httpClientService.DeleteAsync($"api/v1/super-admin/configuration/{key}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting configuration {Key}", key);
            throw;
        }
    }

    public async Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto)
    {
        try
        {
            var result = await httpClientService.PostAsync<SmtpTestDto, SmtpTestResultDto>("api/v1/super-admin/configuration/test-smtp", testDto);
            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing SMTP");
            throw;
        }
    }

    public async Task ReloadConfigurationAsync()
    {
        try
        {
            await httpClientService.PostAsync<object?>("api/v1/super-admin/configuration/reload", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reloading configuration");
            throw;
        }
    }
}