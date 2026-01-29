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
    Task<List<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific dashboard configuration by ID.
    /// </summary>
    Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the default dashboard configuration for an entity type.
    /// </summary>
    Task<DashboardConfigurationDto?> GetDefaultConfigurationAsync(string entityType, CancellationToken ct = default);

    /// <summary>
    /// Creates a new dashboard configuration.
    /// </summary>
    Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing dashboard configuration.
    /// </summary>
    Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a dashboard configuration.
    /// </summary>
    Task DeleteConfigurationAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets a configuration as default for its entity type.
    /// </summary>
    Task SetAsDefaultAsync(Guid id, CancellationToken ct = default);
}
