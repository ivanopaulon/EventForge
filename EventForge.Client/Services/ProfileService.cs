using EventForge.DTOs.Profile;
using MudBlazor;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EventForge.Client.Services;

public class ProfileService : IProfileService
{
    private readonly IHttpClientService _httpClientService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthService _authService;
    private readonly ILogger<ProfileService> _logger;
    private readonly ISnackbar _snackbar;
    private readonly ITranslationService _translationService;

    public ProfileService(
        IHttpClientService httpClientService,
        IHttpClientFactory httpClientFactory,
        IAuthService authService,
        ILogger<ProfileService> logger,
        ISnackbar snackbar,
        ITranslationService translationService)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
    }

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        try
        {
            var profile = await _httpClientService.GetAsync<UserProfileDto>("/api/v1/profile");
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.loadFailed", "Failed to load profile"),
                Severity.Error);
            return null;
        }
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileDto updateDto)
    {
        try
        {
            var profile = await _httpClientService.PutAsync<UpdateProfileDto, UserProfileDto>("/api/v1/profile", updateDto);
            
            if (profile != null)
            {
                _snackbar.Add(
                    _translationService.GetTranslation("profile.success.updated", "Profile updated successfully"),
                    Severity.Success);
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.updateFailed", "Failed to update profile"),
                Severity.Error);
            return null;
        }
    }

    public async Task<UserProfileDto?> UploadAvatarAsync(Stream fileStream, string fileName, string contentType)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/profile/avatar")
            {
                Content = content
            };

            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                
                if (profile != null)
                {
                    _snackbar.Add(
                        _translationService.GetTranslation("profile.success.avatarUploaded", "Avatar uploaded successfully"),
                        Severity.Success);
                }
                
                return profile;
            }
            else
            {
                _snackbar.Add(
                    _translationService.GetTranslation("profile.error.avatarUploadFailed", "Failed to upload avatar"),
                    Severity.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.avatarUploadFailed", "Failed to upload avatar"),
                Severity.Error);
            return null;
        }
    }

    public async Task<bool> DeleteAvatarAsync()
    {
        try
        {
            await _httpClientService.DeleteAsync("/api/v1/profile/avatar");
            _snackbar.Add(
                _translationService.GetTranslation("profile.success.avatarDeleted", "Avatar deleted successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.avatarDeleteFailed", "Failed to delete avatar"),
                Severity.Error);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        try
        {
            await _httpClientService.PutAsync("/api/v1/profile/password", changePasswordDto);
            _snackbar.Add(
                _translationService.GetTranslation("profile.success.passwordChanged", "Password changed successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.passwordChangeFailed", "Failed to change password"),
                Severity.Error);
            return false;
        }
    }

    public async Task<UserProfileDto?> UpdateNotificationPreferencesAsync(UpdateNotificationPreferencesDto preferencesDto)
    {
        try
        {
            var profile = await _httpClientService.PutAsync<UpdateNotificationPreferencesDto, UserProfileDto>(
                "/api/v1/profile/notifications", 
                preferencesDto);
            
            if (profile != null)
            {
                _snackbar.Add(
                    _translationService.GetTranslation("profile.success.notificationsUpdated", "Notification preferences updated"),
                    Severity.Success);
            }
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.notificationsUpdateFailed", "Failed to update notification preferences"),
                Severity.Error);
            return null;
        }
    }

    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
    {
        try
        {
            var sessions = await _httpClientService.GetAsync<List<ActiveSessionDto>>("/api/v1/profile/sessions");
            return sessions ?? new List<ActiveSessionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.sessionsLoadFailed", "Failed to load active sessions"),
                Severity.Error);
            return new List<ActiveSessionDto>();
        }
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"/api/v1/profile/sessions/{sessionId}");
            _snackbar.Add(
                _translationService.GetTranslation("profile.success.sessionTerminated", "Session terminated successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.sessionTerminateFailed", "Failed to terminate session"),
                Severity.Error);
            return false;
        }
    }

    public async Task<bool> TerminateAllOtherSessionsAsync()
    {
        try
        {
            await _httpClientService.DeleteAsync("/api/v1/profile/sessions/all");
            _snackbar.Add(
                _translationService.GetTranslation("profile.success.allSessionsTerminated", "All other sessions terminated"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating all sessions");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.allSessionsTerminateFailed", "Failed to terminate sessions"),
                Severity.Error);
            return false;
        }
    }

    public async Task<List<LoginHistoryDto>> GetLoginHistoryAsync(int days = 30)
    {
        try
        {
            var history = await _httpClientService.GetAsync<List<LoginHistoryDto>>($"/api/v1/profile/login-history?days={days}");
            return history ?? new List<LoginHistoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving login history");
            _snackbar.Add(
                _translationService.GetTranslation("profile.error.historyLoadFailed", "Failed to load login history"),
                Severity.Error);
            return new List<LoginHistoryDto>();
        }
    }
}
