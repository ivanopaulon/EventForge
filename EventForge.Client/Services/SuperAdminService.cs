using EventForge.DTOs.SuperAdmin;
using EventForge.DTOs.Tenants;

namespace EventForge.Client.Services
{
    public interface ISuperAdminService
    {
        // Role Management
        Task<IEnumerable<RoleDto>> GetRolesAsync();
        Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(Guid roleId);

        // Tenant Management
        Task<IEnumerable<TenantResponseDto>> GetTenantsAsync();
        Task<TenantResponseDto?> GetTenantAsync(Guid id);
        Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto);
        Task<TenantResponseDto> UpdateTenantAsync(Guid id, UpdateTenantDto updateDto);
        Task DeleteTenantAsync(Guid id, string reason = "Soft deleted by superadmin");
        Task EnableTenantAsync(Guid id, string reason = "Enabled by admin");
        Task DisableTenantAsync(Guid id, string reason = "Disabled by admin");
        Task<TenantStatisticsDto> GetTenantStatisticsAsync();
        Task<TenantDetailDto?> GetTenantDetailsAsync(Guid id);
        Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid id);
        Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid id, UpdateTenantLimitsDto updateDto);

        // User Management
        Task<IEnumerable<UserManagementDto>> GetUsersAsync(Guid? tenantId = null);
        Task<UserManagementDto?> GetUserAsync(Guid id);
        Task<UserManagementDto> CreateUserAsync(CreateUserManagementDto createDto);
        Task<UserManagementDto> UpdateUserAsync(Guid id, UpdateUserManagementDto updateDto);
        Task DeleteUserAsync(Guid id);
        Task<PasswordResetResultDto> ResetUserPasswordAsync(Guid id, ResetPasswordDto resetDto);
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
    }

    public class SuperAdminService : ISuperAdminService
    {
        private const string BaseUrl = "api/v1/superadmin";
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<SuperAdminService> _logger;
        private readonly ILoadingDialogService _loadingDialogService;

        public SuperAdminService(IHttpClientService httpClientService, ILogger<SuperAdminService> logger, ILoadingDialogService loadingDialogService)
        {
            _httpClientService = httpClientService;
            _logger = logger;
            _loadingDialogService = loadingDialogService;
        }

        #region Role Management

        public async Task<IEnumerable<RoleDto>> GetRolesAsync()
        {
            try
            {
                var roles = await _httpClientService.GetAsync<IEnumerable<object>>("api/v1/user-management/roles");
                if (roles == null)
                {
                    return new List<RoleDto>();
                }

                // Fetch all users once to avoid N+1 query problem
                var users = await _httpClientService.GetAsync<IEnumerable<UserManagementDto>>("api/v1/user-management");
                var usersList = users?.ToList() ?? new List<UserManagementDto>();

                // Define system roles
                var systemRoleNames = new HashSet<string> { "SuperAdmin", "Admin", "Manager", "User" };

                // Map the anonymous objects to RoleDto
                var roleDtos = new List<RoleDto>();
                foreach (var role in roles)
                {
                    var roleType = role.GetType();
                    var id = (Guid)roleType.GetProperty("Id")?.GetValue(role)!;
                    var name = (string)roleType.GetProperty("Name")?.GetValue(role)!;
                    var description = (string?)roleType.GetProperty("Description")?.GetValue(role);

                    // Count users in this role from the pre-fetched list
                    var usersInRole = usersList.Count(u => u.Roles.Contains(name));

                    roleDtos.Add(new RoleDto
                    {
                        Id = id,
                        RoleName = name,
                        Description = description,
                        UsersCount = usersInRole,
                        PermissionsCount = 0, // Permissions count not available yet
                        IsSystemRole = systemRoleNames.Contains(name),
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return roleDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return new List<RoleDto>();
            }
        }

        public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
        {
            // Permissions endpoint not yet available
            // Return empty list until backend implementation is ready
            await Task.CompletedTask;
            return new List<PermissionDto>();
        }

        #endregion

        #region Tenant Management

        public async Task<IEnumerable<TenantResponseDto>> GetTenantsAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<TenantResponseDto>>("api/v1/tenants") ?? new List<TenantResponseDto>();
        }

        public async Task<TenantResponseDto?> GetTenantAsync(Guid id)
        {
            return await _httpClientService.GetAsync<TenantResponseDto>($"api/v1/tenants/{id}");
        }

        public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto)
        {
            try
            {
                await _loadingDialogService.ShowAsync("Creazione Tenant", "Preparazione nuovo tenant...", true);
                await _loadingDialogService.UpdateProgressAsync(20);

                await _loadingDialogService.UpdateOperationAsync("Validazione dati tenant...");
                await _loadingDialogService.UpdateProgressAsync(40);

                await _loadingDialogService.UpdateOperationAsync("Creazione tenant nel database...");
                await _loadingDialogService.UpdateProgressAsync(70);

                var result = await _httpClientService.PostAsync<CreateTenantDto, TenantResponseDto>("api/v1/tenants", createDto) ??
                       throw new InvalidOperationException("Failed to create tenant");

                await _loadingDialogService.UpdateOperationAsync("Tenant creato con successo");
                await _loadingDialogService.UpdateProgressAsync(100);

                await Task.Delay(1000);
                await _loadingDialogService.HideAsync();

                return result;
            }
            catch (Exception)
            {
                await _loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<TenantResponseDto> UpdateTenantAsync(Guid id, UpdateTenantDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateTenantDto, TenantResponseDto>($"api/v1/tenants/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update tenant");
        }

        public async Task DeleteTenantAsync(Guid id, string reason = "Soft deleted by superadmin")
        {
            await _httpClientService.DeleteAsync($"api/v1/tenants/{id}/soft");
        }

        public async Task EnableTenantAsync(Guid id, string reason = "Enabled by admin")
        {
            await _httpClientService.PostAsync($"api/v1/tenants/{id}/enable", reason);
        }

        public async Task DisableTenantAsync(Guid id, string reason = "Disabled by admin")
        {
            await _httpClientService.PostAsync($"api/v1/tenants/{id}/disable", reason);
        }

        public async Task<TenantStatisticsDto> GetTenantStatisticsAsync()
        {
            return await _httpClientService.GetAsync<TenantStatisticsDto>("api/v1/tenants/statistics") ??
                   new TenantStatisticsDto();
        }

        public async Task<TenantDetailDto?> GetTenantDetailsAsync(Guid id)
        {
            return await _httpClientService.GetAsync<TenantDetailDto>($"api/v1/tenants/{id}/details");
        }

        public async Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid id)
        {
            return await _httpClientService.GetAsync<TenantLimitsDto>($"api/v1/tenants/{id}/limits");
        }

        public async Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid id, UpdateTenantLimitsDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateTenantLimitsDto, TenantLimitsDto>($"api/v1/tenants/{id}/limits", updateDto) ??
                   throw new InvalidOperationException("Failed to update tenant limits");
        }

        #endregion

        #region User Management

        public async Task<IEnumerable<UserManagementDto>> GetUsersAsync(Guid? tenantId = null)
        {
            var url = "api/v1/user-management";
            if (tenantId.HasValue)
                url += $"?tenantId={tenantId}";

            return await _httpClientService.GetAsync<IEnumerable<UserManagementDto>>(url) ?? new List<UserManagementDto>();
        }

        public async Task<UserManagementDto?> GetUserAsync(Guid id)
        {
            return await _httpClientService.GetAsync<UserManagementDto>($"api/v1/user-management/{id}");
        }

        public async Task<UserManagementDto> CreateUserAsync(CreateUserManagementDto createDto)
        {
            return await _httpClientService.PostAsync<CreateUserManagementDto, UserManagementDto>("api/v1/user-management/management", createDto) ??
                   throw new InvalidOperationException("Failed to create user");
        }

        public async Task<UserManagementDto> UpdateUserAsync(Guid id, UpdateUserManagementDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateUserManagementDto, UserManagementDto>($"api/v1/user-management/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update user");
        }

        public async Task DeleteUserAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/user-management/{id}");
        }

        public async Task<PasswordResetResultDto> ResetUserPasswordAsync(Guid id, ResetPasswordDto resetDto)
        {
            return await _httpClientService.PostAsync<ResetPasswordDto, PasswordResetResultDto>($"api/v1/user-management/{id}/reset-password", resetDto) ??
                   throw new InvalidOperationException("Failed to reset user password");
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid? tenantId = null)
        {
            var url = "api/v1/user-management/statistics";
            if (tenantId.HasValue)
                url += $"?tenantId={tenantId}";

            return await _httpClientService.GetAsync<UserStatisticsDto>(url) ??
                   new UserStatisticsDto();
        }

        #endregion

        #region Tenant Switching & Impersonation

        public async Task<TenantSwitchResponseDto> SwitchTenantAsync(SwitchTenantDto switchDto)
        {
            return await _httpClientService.PostAsync<SwitchTenantDto, TenantSwitchResponseDto>("api/v1/tenant-switch/switch", switchDto) ??
                   throw new InvalidOperationException("Failed to switch tenant");
        }

        public async Task<ImpersonationResponseDto> ImpersonateUserAsync(ImpersonateUserDto impersonateDto)
        {
            return await _httpClientService.PostAsync<ImpersonateUserDto, ImpersonationResponseDto>("api/v1/tenant-switch/impersonate", impersonateDto) ??
                   throw new InvalidOperationException("Failed to impersonate user");
        }

        public async Task EndImpersonationAsync(EndImpersonationDto endDto)
        {
            await _httpClientService.PostAsync("api/v1/tenant-switch/end-impersonation", endDto);
        }

        public async Task<CurrentContextDto> GetCurrentContextAsync()
        {
            return await _httpClientService.GetAsync<CurrentContextDto>("api/v1/tenant-context/current") ??
                   new CurrentContextDto();
        }

        #endregion

        #region Configuration Management

        public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<ConfigurationDto>>("api/v1/super-admin/configuration") ?? new List<ConfigurationDto>();
        }

        public async Task<IEnumerable<ConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
        {
            return await _httpClientService.GetAsync<IEnumerable<ConfigurationDto>>($"api/v1/super-admin/configuration/category/{category}") ?? new List<ConfigurationDto>();
        }

        public async Task<IEnumerable<string>> GetConfigurationCategoriesAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<string>>("api/v1/super-admin/configuration/categories") ?? new List<string>();
        }

        public async Task<ConfigurationDto?> GetConfigurationAsync(string key)
        {
            return await _httpClientService.GetAsync<ConfigurationDto>($"api/v1/super-admin/configuration/{key}");
        }

        public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto)
        {
            return await _httpClientService.PostAsync<CreateConfigurationDto, ConfigurationDto>("api/v1/super-admin/configuration", createDto) ??
                   throw new InvalidOperationException("Failed to create configuration");
        }

        public async Task<ConfigurationDto> UpdateConfigurationAsync(string key, UpdateConfigurationDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateConfigurationDto, ConfigurationDto>($"api/v1/super-admin/configuration/{key}", updateDto) ??
                   throw new InvalidOperationException("Failed to update configuration");
        }

        public async Task DeleteConfigurationAsync(string key)
        {
            await _httpClientService.DeleteAsync($"api/v1/super-admin/configuration/{key}");
        }

        public async Task<bool> TestSmtpConfigurationAsync(SmtpTestDto testDto)
        {
            var result = await _httpClientService.PostAsync<SmtpTestDto, bool>("api/v1/super-admin/configuration/test-smtp", testDto);
            return result;
        }

        public async Task ReloadConfigurationAsync()
        {
            await _httpClientService.PostAsync("api/v1/super-admin/configuration/reload", new { });
        }

        #endregion

        #region Backup Management

        public async Task<BackupOperationDto> CreateBackupAsync(CreateBackupDto createDto)
        {
            try
            {
                await _loadingDialogService.ShowAsync("Creazione Backup", "Inizializzazione processo di backup...", true);
                await _loadingDialogService.UpdateProgressAsync(10);

                await _loadingDialogService.UpdateOperationAsync("Invio richiesta al server...");
                await _loadingDialogService.UpdateProgressAsync(30);

                var result = await _httpClientService.PostAsync<CreateBackupDto, BackupOperationDto>("api/v1/super-admin/backup", createDto) ??
                       throw new InvalidOperationException("Failed to create backup");

                await _loadingDialogService.UpdateOperationAsync("Backup avviato con successo");
                await _loadingDialogService.UpdateProgressAsync(100);

                // Hide after a short delay to show completion
                await Task.Delay(1000);
                await _loadingDialogService.HideAsync();

                return result;
            }
            catch (Exception)
            {
                await _loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId)
        {
            return await _httpClientService.GetAsync<BackupStatusDto>($"api/v1/super-admin/backup/{backupId}");
        }

        public async Task<IEnumerable<BackupListItemDto>> GetBackupsAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<BackupListItemDto>>("api/v1/super-admin/backup") ?? new List<BackupListItemDto>();
        }

        public async Task CancelBackupAsync(Guid backupId)
        {
            await _httpClientService.PostAsync($"api/v1/super-admin/backup/{backupId}/cancel", new { });
        }

        public async Task<Stream> DownloadBackupAsync(Guid backupId)
        {
            try
            {
                await _loadingDialogService.ShowAsync("Download Backup", "Preparazione download...", true);
                await _loadingDialogService.UpdateProgressAsync(20);

                await _loadingDialogService.UpdateOperationAsync("Richiesta file dal server...");
                await _loadingDialogService.UpdateProgressAsync(50);

                var stream = await _httpClientService.GetStreamAsync($"api/v1/super-admin/backup/{backupId}/download");

                await _loadingDialogService.UpdateOperationAsync("Download in corso...");
                await _loadingDialogService.UpdateProgressAsync(90);

                await _loadingDialogService.UpdateOperationAsync("Download completato");
                await _loadingDialogService.UpdateProgressAsync(100);

                await Task.Delay(500);
                await _loadingDialogService.HideAsync();

                return stream;
            }
            catch (Exception)
            {
                await _loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task DeleteBackupAsync(Guid backupId)
        {
            await _httpClientService.DeleteAsync($"api/v1/super-admin/backup/{backupId}");
        }

        #endregion
    }
}