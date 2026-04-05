using EventForge.DTOs.Dashboard;
using System.Net;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Service for managing dashboard configurations.
    /// </summary>
    public class DashboardConfigurationService(IHttpClientService httpClientService) : IDashboardConfigurationService
    {

        public async Task<IEnumerable<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType, CancellationToken ct = default)
        {
            var response = await httpClientService.GetAsync<List<DashboardConfigurationDto>>($"api/v1/DashboardConfiguration?entityType={entityType}", ct);
            return response ?? [];
        }

        public async Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await httpClientService.GetAsync<DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}", ct);
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
        }

        public async Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto createDto, CancellationToken ct = default)
        {
            var response = await httpClientService.PostAsync<CreateDashboardConfigurationDto, DashboardConfigurationDto>("api/v1/DashboardConfiguration", createDto, ct);
            if (response == null)
            {
                throw new InvalidOperationException("Failed to create dashboard configuration");
            }
            return response;
        }

        public async Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto updateDto, CancellationToken ct = default)
        {
            var response = await httpClientService.PutAsync<UpdateDashboardConfigurationDto, DashboardConfigurationDto>($"api/v1/DashboardConfiguration/{id}", updateDto, ct);
            if (response == null)
            {
                throw new InvalidOperationException("Failed to update dashboard configuration");
            }
            return response;
        }

        public async Task DeleteConfigurationAsync(Guid id, CancellationToken ct = default)
        {
            await httpClientService.DeleteAsync($"api/v1/DashboardConfiguration/{id}", ct);
        }

        public async Task SetAsDefaultAsync(Guid id, CancellationToken ct = default)
        {
            await httpClientService.PostAsync<object>($"api/v1/DashboardConfiguration/{id}/set-default", new { }, ct);
        }
    }
}
