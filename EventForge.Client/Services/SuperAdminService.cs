using EventForge.DTOs.SuperAdmin;
using EventForge.DTOs.Tenants;
using System.Net.Http.Json;

namespace EventForge.Client.Services
{
    public interface ISuperAdminService
    {
        // Tenant Management
        Task<IEnumerable<TenantResponseDto>> GetTenantsAsync();
        Task<TenantResponseDto?> GetTenantAsync(Guid id);
        Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto);
        Task<TenantResponseDto> UpdateTenantAsync(Guid id, UpdateTenantDto updateDto);
        Task DeleteTenantAsync(Guid id, string reason = "Soft deleted by superadmin");
        Task<TenantStatisticsDto> GetTenantStatisticsAsync();

        // User Management
        Task<IEnumerable<UserManagementDto>> GetUsersAsync(Guid? tenantId = null);
        Task<UserManagementDto?> GetUserAsync(Guid id);
        Task<UserManagementDto> CreateUserAsync(CreateUserManagementDto createDto);
        Task<UserManagementDto> UpdateUserAsync(Guid id, UpdateUserManagementDto updateDto);
        Task DeleteUserAsync(Guid id);
        Task<UserStatisticsDto> GetUserStatisticsAsync(Guid? tenantId = null);

        // Tenant Switching & Impersonation
        Task<TenantSwitchResponseDto> SwitchTenantAsync(SwitchTenantDto switchDto);
        Task<ImpersonationResponseDto> ImpersonateUserAsync(ImpersonateUserDto impersonateDto);
        Task EndImpersonationAsync(EndImpersonationDto endDto);
        Task<CurrentContextDto> GetCurrentContextAsync();

        // Configuration Management
        Task<IEnumerable<ConfigurationDto>> GetConfigurationsAsync();
        Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category);
        Task<IEnumerable<string>> GetConfigurationCategoriesAsync();
        Task<ConfigurationDto?> GetConfigurationAsync(string key);
        Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto);
        Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto);
        Task DeleteConfigurationAsync(string key);
        Task<bool> TestSmtpConfigurationAsync(SmtpTestDto testDto);
        Task ReloadConfigurationAsync();

        // Backup Management
        Task<BackupOperationDto> CreateBackupAsync(CreateBackupDto createDto);
        Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId);
        Task<IEnumerable<BackupListItemDto>> GetBackupsAsync();
        Task CancelBackupAsync(Guid backupId);
        Task<Stream> DownloadBackupAsync(Guid backupId);
        Task DeleteBackupAsync(Guid backupId);

        // Event Management
        Task<IEnumerable<EventManagementDto>> GetEventsAsync(Guid? tenantId = null);
        Task<EventManagementDto?> GetEventAsync(Guid id);
        Task<EventManagementDto> CreateEventAsync(CreateEventManagementDto createDto);
        Task<EventManagementDto> UpdateEventAsync(Guid id, UpdateEventManagementDto updateDto);
        Task DeleteEventAsync(Guid id);
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid? tenantId = null);

        // Event Type Management
        Task<IEnumerable<EventTypeDto>> GetEventTypesAsync();
        Task<EventTypeDto?> GetEventTypeAsync(Guid id);
        Task<EventTypeDto> CreateEventTypeAsync(CreateEventTypeDto createDto);
        Task<EventTypeDto> UpdateEventTypeAsync(Guid id, UpdateEventTypeDto updateDto);
        Task DeleteEventTypeAsync(Guid id);

        // Event Category Management
        Task<IEnumerable<EventCategoryDto>> GetEventCategoriesAsync();
        Task<EventCategoryDto?> GetEventCategoryAsync(Guid id);
        Task<EventCategoryDto> CreateEventCategoryAsync(CreateEventCategoryDto createDto);
        Task<EventCategoryDto> UpdateEventCategoryAsync(Guid id, UpdateEventCategoryDto updateDto);
        Task DeleteEventCategoryAsync(Guid id);
    }

    public class SuperAdminService : ISuperAdminService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthService _authService;
        private readonly ILogger<SuperAdminService> _logger;

        public SuperAdminService(IHttpClientFactory httpClientFactory, IAuthService authService, ILogger<SuperAdminService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            _logger = logger;
        }

        private async Task<HttpClient> GetConfiguredHttpClientAsync()
        {
            var token = await _authService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("User not authenticated");

            var httpClient = _httpClientFactory.CreateClient("ApiClient");

            // Set authentication header for this request
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return httpClient;
        }

        #region Tenant Management

        public async Task<IEnumerable<TenantResponseDto>> GetTenantsAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/Tenants");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<TenantResponseDto>>() ?? new List<TenantResponseDto>();
        }

        public async Task<TenantResponseDto?> GetTenantAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/Tenants/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        }

        public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/Tenants", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TenantResponseDto>() ??
                   throw new InvalidOperationException("Failed to create tenant");
        }

        public async Task<TenantResponseDto> UpdateTenantAsync(Guid id, UpdateTenantDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/Tenants/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TenantResponseDto>() ??
                   throw new InvalidOperationException("Failed to update tenant");
        }

        public async Task DeleteTenantAsync(Guid id, string reason = "Soft deleted by superadmin")
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var requestUri = $"api/Tenants/{id}/soft";
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(reason), System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri)
            {
                Content = content
            };

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        public async Task<TenantStatisticsDto> GetTenantStatisticsAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/Tenants/statistics");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TenantStatisticsDto>() ??
                   new TenantStatisticsDto();
        }

        #endregion

        #region User Management

        public async Task<IEnumerable<UserManagementDto>> GetUsersAsync(Guid? tenantId = null)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var url = "api/UserManagement";
            if (tenantId.HasValue)
                url += $"?tenantId={tenantId}";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<UserManagementDto>>() ?? new List<UserManagementDto>();
        }

        public async Task<UserManagementDto?> GetUserAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/UserManagement/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserManagementDto>();
        }

        public async Task<UserManagementDto> CreateUserAsync(CreateUserManagementDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/UserManagement", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserManagementDto>() ??
                   throw new InvalidOperationException("Failed to create user");
        }

        public async Task<UserManagementDto> UpdateUserAsync(Guid id, UpdateUserManagementDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/UserManagement/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserManagementDto>() ??
                   throw new InvalidOperationException("Failed to update user");
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/UserManagement/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid? tenantId = null)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var url = "api/UserManagement/statistics";
            if (tenantId.HasValue)
                url += $"?tenantId={tenantId}";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserStatisticsDto>() ??
                   new UserStatisticsDto();
        }

        #endregion

        #region Tenant Switching & Impersonation

        public async Task<TenantSwitchResponseDto> SwitchTenantAsync(SwitchTenantDto switchDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/TenantSwitch/switch", switchDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TenantSwitchResponseDto>() ??
                   throw new InvalidOperationException("Failed to switch tenant");
        }

        public async Task<ImpersonationResponseDto> ImpersonateUserAsync(ImpersonateUserDto impersonateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/TenantSwitch/impersonate", impersonateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ImpersonationResponseDto>() ??
                   throw new InvalidOperationException("Failed to impersonate user");
        }

        public async Task EndImpersonationAsync(EndImpersonationDto endDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/TenantSwitch/end-impersonation", endDto);
            response.EnsureSuccessStatusCode();
        }

        public async Task<CurrentContextDto> GetCurrentContextAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/TenantContext/current");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CurrentContextDto>() ??
                   new CurrentContextDto();
        }

        #endregion

        #region Configuration Management

        public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/SuperAdmin/configuration");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ConfigurationDto>>() ?? new List<ConfigurationDto>();
        }

        public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/configuration/category/{category}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<ConfigurationDto>>() ?? new List<ConfigurationDto>();
        }

        public async Task<IEnumerable<string>> GetConfigurationCategoriesAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/SuperAdmin/configuration/categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<string>>() ?? new List<string>();
        }

        public async Task<ConfigurationDto?> GetConfigurationAsync(string key)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/configuration/{key}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConfigurationDto>();
        }

        public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/configuration", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConfigurationDto>() ??
                   throw new InvalidOperationException("Failed to create configuration");
        }

        public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/SuperAdmin/configuration/{key}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConfigurationDto>() ??
                   throw new InvalidOperationException("Failed to update configuration");
        }

        public async Task DeleteConfigurationAsync(string key)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/SuperAdmin/configuration/{key}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> TestSmtpConfigurationAsync(SmtpTestDto testDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/configuration/test-smtp", testDto);
            return response.IsSuccessStatusCode;
        }

        public async Task ReloadConfigurationAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsync("api/SuperAdmin/configuration/reload", null);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Backup Management

        public async Task<BackupOperationDto> CreateBackupAsync(CreateBackupDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/backup", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BackupOperationDto>() ??
                   throw new InvalidOperationException("Failed to create backup");
        }

        public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/backup/{backupId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BackupStatusDto>();
        }

        public async Task<IEnumerable<BackupListItemDto>> GetBackupsAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/SuperAdmin/backup");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<BackupListItemDto>>() ?? new List<BackupListItemDto>();
        }

        public async Task CancelBackupAsync(Guid backupId)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsync($"api/SuperAdmin/backup/{backupId}/cancel", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task<Stream> DownloadBackupAsync(Guid backupId)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/backup/{backupId}/download");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task DeleteBackupAsync(Guid backupId)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/SuperAdmin/backup/{backupId}");
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Event Management

        public async Task<IEnumerable<EventManagementDto>> GetEventsAsync(Guid? tenantId = null)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var url = tenantId.HasValue ? $"api/SuperAdmin/events?tenantId={tenantId}" : "api/SuperAdmin/events";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<EventManagementDto>>() ?? new List<EventManagementDto>();
        }

        public async Task<EventManagementDto?> GetEventAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/events/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventManagementDto>();
        }

        public async Task<EventManagementDto> CreateEventAsync(CreateEventManagementDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/events", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventManagementDto>() ?? throw new Exception("Failed to create event");
        }

        public async Task<EventManagementDto> UpdateEventAsync(Guid id, UpdateEventManagementDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/SuperAdmin/events/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventManagementDto>() ?? throw new Exception("Failed to update event");
        }

        public async Task DeleteEventAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/SuperAdmin/events/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid? tenantId = null)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var url = tenantId.HasValue ? $"api/SuperAdmin/events/statistics?tenantId={tenantId}" : "api/SuperAdmin/events/statistics";
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventStatisticsDto>() ?? new EventStatisticsDto();
        }

        #endregion

        #region Event Type Management

        public async Task<IEnumerable<EventTypeDto>> GetEventTypesAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/SuperAdmin/event-types");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<EventTypeDto>>() ?? new List<EventTypeDto>();
        }

        public async Task<EventTypeDto?> GetEventTypeAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/event-types/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventTypeDto>();
        }

        public async Task<EventTypeDto> CreateEventTypeAsync(CreateEventTypeDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/event-types", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventTypeDto>() ?? throw new Exception("Failed to create event type");
        }

        public async Task<EventTypeDto> UpdateEventTypeAsync(Guid id, UpdateEventTypeDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/SuperAdmin/event-types/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventTypeDto>() ?? throw new Exception("Failed to update event type");
        }

        public async Task DeleteEventTypeAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/SuperAdmin/event-types/{id}");
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region Event Category Management

        public async Task<IEnumerable<EventCategoryDto>> GetEventCategoriesAsync()
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync("api/SuperAdmin/event-categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<EventCategoryDto>>() ?? new List<EventCategoryDto>();
        }

        public async Task<EventCategoryDto?> GetEventCategoryAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.GetAsync($"api/SuperAdmin/event-categories/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventCategoryDto>();
        }

        public async Task<EventCategoryDto> CreateEventCategoryAsync(CreateEventCategoryDto createDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/SuperAdmin/event-categories", createDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventCategoryDto>() ?? throw new Exception("Failed to create event category");
        }

        public async Task<EventCategoryDto> UpdateEventCategoryAsync(Guid id, UpdateEventCategoryDto updateDto)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/SuperAdmin/event-categories/{id}", updateDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventCategoryDto>() ?? throw new Exception("Failed to update event category");
        }

        public async Task DeleteEventCategoryAsync(Guid id)
        {
            var httpClient = await GetConfiguredHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/SuperAdmin/event-categories/{id}");
            response.EnsureSuccessStatusCode();
        }

        #endregion
    }
}