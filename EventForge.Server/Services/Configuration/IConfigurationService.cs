namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service interface for managing system configuration settings.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all configuration settings</returns>
    Task<IEnumerable<ConfigurationDto>> GetAllConfigurationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets configuration settings by category.
    /// </summary>
    /// <param name="category">Configuration category</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of configuration settings in the category</returns>
    Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific configuration setting by key.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Configuration setting or null if not found</returns>
    Task<ConfigurationDto?> GetConfigurationAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Creates a new configuration setting.
    /// </summary>
    /// <param name="createDto">Configuration creation data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created configuration setting</returns>
    Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="updateDto">Configuration update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated configuration setting</returns>
    Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteConfigurationAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the value of a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Configuration value or default value</returns>
    Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default);

    /// <summary>
    /// Sets the value of a configuration setting.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <param name="reason">Reason for the change</param>
    /// <param name="ct">Cancellation token</param>
    Task SetValueAsync(string key, string value, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Tests SMTP configuration by sending a test email.
    /// </summary>
    /// <param name="testDto">SMTP test configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Test result</returns>
    Task<SmtpTestResultDto> TestSmtpAsync(SmtpTestDto testDto, CancellationToken ct = default);

    /// <summary>
    /// Reloads configuration from the database (hot reload).
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task ReloadConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all available configuration categories.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of configuration categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken ct = default);
}