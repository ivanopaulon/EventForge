namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service interface for managing system configuration settings.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    /// <returns>List of all configuration settings</returns>
    Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync();

    /// <summary>
    /// Gets configuration settings by category.
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <returns>List of configuration settings in the category</returns>
    Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category);

    /// <summary>
    /// Gets a specific configuration setting by key.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>Configuration setting or null if not found</returns>
    Task<ConfigurationDto?> GetConfigurationAsync(string key);

    /// <summary>
    /// Creates a new configuration setting.
    /// </summary>
    /// <param name="createDto">Configuration creation data</param>
    /// <returns>Created configuration setting</returns>
    Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto);

    /// <summary>
    /// Updates a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="updateDto">Configuration update data</param>
    /// <returns>Updated configuration setting</returns>
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto);

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    Task DeleteConfigurationAsync(string key);

    /// <summary>
    /// Gets the value of a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Configuration value or default value</returns>
    Task<string> GetValueAsync(string key, string defaultValue = "");

    /// <summary>
    /// Sets the value of a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="reason">Reason for the change</param>
    Task SetValueAsync(string key, string value, string? reason = null);

    /// <summary>
    /// Tests SMTP configuration by sending a test email.
    /// </summary>
    /// <param name="testDto">SMTP test configuration</param>
    /// <returns>Test result</returns>
    Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto);

    /// <summary>
    /// Reloads configuration from the database (hot reload).
    /// </summary>
    Task ReloadConfigurationAsync();

    /// <summary>
    /// Gets all available configuration categories.
    /// </summary>
    /// <returns>List of configuration categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync();
}