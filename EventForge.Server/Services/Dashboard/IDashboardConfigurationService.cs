using EventForge.DTOs.Dashboard;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Service interface for managing dashboard configurations.
/// </summary>
public interface IDashboardConfigurationService
{
    /// <summary>
    /// Gets all dashboard configurations for the current user and entity type.
    /// </summary>
    Task<List<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType);

    /// <summary>
    /// Gets a specific dashboard configuration by ID.
    /// </summary>
    Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id);

    /// <summary>
    /// Gets the default dashboard configuration for an entity type.
    /// </summary>
    Task<DashboardConfigurationDto?> GetDefaultConfigurationAsync(string entityType);

    /// <summary>
    /// Creates a new dashboard configuration.
    /// </summary>
    Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto dto);

    /// <summary>
    /// Updates an existing dashboard configuration.
    /// </summary>
    Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto dto);

    /// <summary>
    /// Deletes a dashboard configuration.
    /// </summary>
    Task DeleteConfigurationAsync(Guid id);

    /// <summary>
    /// Sets a configuration as default for its entity type.
    /// </summary>
    Task SetAsDefaultAsync(Guid id);
}
