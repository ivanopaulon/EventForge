using Prym.DTOs.Dashboard;
using System.Net;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Service for managing dashboard configurations.
    /// </summary>
    public class DashboardConfigurationService(
        IHttpClientService httpClientService,
        ILogger<DashboardConfigurationService> logger) : IDashboardConfigurationService
    {

        public async Task<IEnumerable<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType, CancellationToken ct = default)
        {
            try
            {
                var response = await httpClientService.GetAsync<List<DashboardConfigurationDto>>($"api/v1/DashboardConfiguration?entityType={entityType}", ct);
                return response ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting dashboard configurations for entity type {EntityType}", entityType);
                throw;
            }
        }

        public async Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting dashboard configuration {Id}", id);
                throw;
            }
        }

        public async Task<DashboardConfigurationDto?> GetDefaultConfigurationAsync(string entityType, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<DashboardConfigurationDto>($"api/v1/DashboardConfiguration/default/{entityType}", ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Business logic: return null if no default exists yet (no logging)
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting default dashboard configuration for entity type {EntityType}", entityType);
                throw;
            }
        }

        public async Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto createDto, CancellationToken ct = default)
        {
            try
            {
                var response = await httpClientService.PostAsync<CreateDashboardConfigurationDto, DashboardConfigurationDto>("api/v1/DashboardConfiguration", createDto, ct);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to create dashboard configuration");
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating dashboard configuration");
                throw;
            }
        }

        public async Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto updateDto, CancellationToken ct = default)
        {
            try
            {
                var response = await httpClientService.PutAsync<UpdateDashboardConfigurationDto, DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}", updateDto, ct);
                if (response == null)
                {
                    throw new InvalidOperationException("Failed to update dashboard configuration");
                }
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating dashboard configuration {Id}", id);
                throw;
            }
        }

        public async Task DeleteConfigurationAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/DashboardConfiguration/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting dashboard configuration {Id}", id);
                throw;
            }
        }

        public async Task SetAsDefaultAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.PostAsync<object>($"api/v1/DashboardConfiguration/{id}/set-default", new { }, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting dashboard configuration {Id} as default", id);
                throw;
            }
        }
    }
}
