using EventForge.DTOs.Dashboard;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.UI
{
    /// <summary>
    /// Service for managing dashboard configurations.
    /// </summary>
    public class DashboardConfigurationService : IDashboardConfigurationService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<DashboardConfigurationService> _logger;

        public DashboardConfigurationService(IHttpClientService httpClientService, ILogger<DashboardConfigurationService> logger)
        {
            _httpClientService = httpClientService;
            _logger = logger;
        }

        public async Task<IEnumerable<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType)
        {
            try
            {
                var response = await _httpClientService.GetAsync<List<DashboardConfigurationDto>>($"api/v1/DashboardConfiguration?entityType={entityType}");
                return response ?? new List<DashboardConfigurationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard configurations for entity type {EntityType}", entityType);
                throw;
            }
        }

        public async Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id)
        {
            try
            {
                return await _httpClientService.GetAsync<DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard configuration {ConfigurationId}", id);
                throw;
            }
        }

        public async Task<DashboardConfigurationDto?> GetDefaultConfigurationAsync(string entityType)
        {
            try
            {
                return await _httpClientService.GetAsync<DashboardConfigurationDto>($"api/v1/DashboardConfiguration/default/{entityType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default dashboard configuration for entity type {EntityType}", entityType);
                return null; // Return null if no default exists yet
            }
        }

        public async Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto createDto)
        {
            try
            {
                var response = await _httpClientService.PostAsync<CreateDashboardConfigurationDto, DashboardConfigurationDto>("api/v1/DashboardConfiguration", createDto);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to create dashboard configuration");
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dashboard configuration");
                throw;
            }
        }

        public async Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto updateDto)
        {
            try
            {
                var response = await _httpClientService.PutAsync<UpdateDashboardConfigurationDto, DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}", updateDto);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to update dashboard configuration");
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dashboard configuration {ConfigurationId}", id);
                throw;
            }
        }

        public async Task DeleteConfigurationAsync(Guid id)
        {
            try
            {
                await _httpClientService.DeleteAsync($"api/v1/DashboardConfiguration/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dashboard configuration {ConfigurationId}", id);
                throw;
            }
        }

        public async Task SetAsDefaultAsync(Guid id)
        {
            try
            {
                await _httpClientService.PostAsync<object>($"api/v1/DashboardConfiguration/{id}/set-default", new { });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting dashboard configuration as default {ConfigurationId}", id);
                throw;
            }
        }
    }
}
