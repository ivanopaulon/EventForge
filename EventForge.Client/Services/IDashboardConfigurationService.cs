using EventForge.DTOs.Dashboard;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Interface for dashboard configuration service.
    /// </summary>
    public interface IDashboardConfigurationService
    {
        /// <summary>
        /// Gets all dashboard configurations for the current user and entity type.
        /// </summary>
        Task<IEnumerable<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType);

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
        Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto createDto);

        /// <summary>
        /// Updates an existing dashboard configuration.
        /// </summary>
        Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto updateDto);

        /// <summary>
        /// Deletes a dashboard configuration.
        /// </summary>
        Task DeleteConfigurationAsync(Guid id);

        /// <summary>
        /// Sets a configuration as default for its entity type.
        /// </summary>
        Task SetAsDefaultAsync(Guid id);
    }
}
