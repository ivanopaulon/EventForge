using EventForge.DTOs.SuperAdmin;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing system configuration from the client.
/// </summary>
public interface IConfigurationService
{
    Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync(CancellationToken ct = default);
    Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category, CancellationToken ct = default);
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ConfigurationDto?> GetConfigurationAsync(string key, CancellationToken ct = default);
    Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, CancellationToken ct = default);
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto, CancellationToken ct = default);
    Task DeleteConfigurationAsync(string key, CancellationToken ct = default);
    Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto, CancellationToken ct = default);
    Task ReloadConfigurationAsync(CancellationToken ct = default);
}

public class ConfigurationService(
    IHttpClientService httpClientService,
    ILogger<ConfigurationService> logger) : IConfigurationService
{

    public async Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<ConfigurationDto>>("api/v1/super-admin/configuration", ct);

            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configurations");
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<ConfigurationDto>>($"api/v1/super-admin/configuration/category/{category}", ct);

            return response ?? Enumerable.Empty<ConfigurationDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configurations for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClientService.GetAsync<IEnumerable<string>>("api/v1/super-admin/configuration/categories", ct);

            return response ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving configuration categories");
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<ConfigurationDto>($"api/v1/super-admin/configuration/{key}", ct);

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

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateConfigurationDto, ConfigurationDto>("api/v1/super-admin/configuration", createDto, ct);

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating configuration");
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateConfigurationDto, ConfigurationDto>($"api/v1/super-admin/configuration/{key}", updateDto, ct);

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating configuration {Key}", key);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(string key, CancellationToken ct = default)
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

    public async Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<SmtpTestDto, SmtpTestResultDto>("api/v1/super-admin/configuration/test-smtp", testDto, ct);

            return result ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing SMTP");
            throw;
        }
    }

    public async Task ReloadConfigurationAsync(CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync<object?>("api/v1/super-admin/configuration/reload", null, ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reloading configuration");
            throw;
        }
    }
}