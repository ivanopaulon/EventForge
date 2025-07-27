using System.Net.Http.Json;

namespace EventForge.Client.Services;

/// <summary>
/// DTOs for SuperAdmin operations - Client side.
/// </summary>
public class ConfigurationDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public bool IsEncrypted { get; set; } = false;
    public bool RequiresRestart { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class CreateConfigurationDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public bool IsEncrypted { get; set; } = false;
    public bool RequiresRestart { get; set; } = false;
}

public class UpdateConfigurationDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresRestart { get; set; } = false;
}

public class SmtpTestDto
{
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SmtpTestResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    public double DurationMs { get; set; }
}

public class BackupRequestDto
{
    public bool IncludeAuditLogs { get; set; } = true;
    public bool IncludeUserData { get; set; } = true;
    public bool IncludeConfiguration { get; set; } = true;
    public string? Description { get; set; }
}

public class BackupStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? CurrentOperation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public string StartedBy { get; set; } = string.Empty;
}

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
        _logger.LogDebug("ConfigurationService: Using HttpClient with BaseAddress: {BaseAddress}", httpClient.BaseAddress);
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