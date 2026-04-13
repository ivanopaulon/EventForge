using Prym.DTOs.Profile;
using MudBlazor;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EventForge.Client.Services;

public class ProfileService(
    IHttpClientService httpClientService,
    IHttpClientFactory httpClientFactory,
    IAuthService authService,
    ILogger<ProfileService> logger,
    ISnackbar snackbar,
    ITranslationService translationService) : IProfileService
{

    public async Task<UserProfileDto?> GetProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var profile = await httpClientService.GetAsync<UserProfileDto>("/api/v1/profile", ct);

            return profile;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profile");
            snackbar.Add(
                translationService.GetTranslation("profile.error.loadFailed", "Failed to load profile"),
                Severity.Error);
            return null;
        }
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var profile = await httpClientService.PutAsync<UpdateProfileDto, UserProfileDto>("/api/v1/profile", updateDto, ct);

            if (profile != null)
            {
                snackbar.Add(
                    translationService.GetTranslation("profile.success.updated", "Profile updated successfully"),
                    Severity.Success);
            }

            return profile;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile");
            snackbar.Add(
                translationService.GetTranslation("profile.error.updateFailed", "Failed to update profile"),
                Severity.Error);
            return null;
        }
    }

    public async Task<UserProfileDto?> UploadAvatarAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var httpClient = httpClientFactory.CreateClient("ApiClient");
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

            var token = await authService.GetAccessTokenAsync();
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
                    snackbar.Add(
                        translationService.GetTranslation("profile.success.avatarUploaded", "Avatar uploaded successfully"),
                        Severity.Success);
                }

                return profile;
            }
            else
            {
                snackbar.Add(
                    translationService.GetTranslation("profile.error.avatarUploadFailed", "Failed to upload avatar"),
                    Severity.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading avatar");
            snackbar.Add(
                translationService.GetTranslation("profile.error.avatarUploadFailed", "Failed to upload avatar"),
                Severity.Error);
            return null;
        }
    }

    public async Task<bool> DeleteAvatarAsync(CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync("/api/v1/profile/avatar");
            snackbar.Add(
                translationService.GetTranslation("profile.success.avatarDeleted", "Avatar deleted successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting avatar");
            snackbar.Add(
                translationService.GetTranslation("profile.error.avatarDeleteFailed", "Failed to delete avatar"),
                Severity.Error);
            return false;
        }
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PutAsync("/api/v1/profile/password", changePasswordDto);
            snackbar.Add(
                translationService.GetTranslation("profile.success.passwordChanged", "Password changed successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password");
            snackbar.Add(
                translationService.GetTranslation("profile.error.passwordChangeFailed", "Failed to change password"),
                Severity.Error);
            return false;
        }
    }

    public async Task<UserProfileDto?> UpdateNotificationPreferencesAsync(UpdateNotificationPreferencesDto preferencesDto, CancellationToken ct = default)
    {
        try
        {
            var profile = await httpClientService.PutAsync<UpdateNotificationPreferencesDto, UserProfileDto>(
                "/api/v1/profile/notifications",
                preferencesDto);

            if (profile != null)
            {
                snackbar.Add(
                    translationService.GetTranslation("profile.success.notificationsUpdated", "Notification preferences updated"),
                    Severity.Success);
            }

            return profile;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating notification preferences");
            snackbar.Add(
                translationService.GetTranslation("profile.error.notificationsUpdateFailed", "Failed to update notification preferences"),
                Severity.Error);
            return null;
        }
    }

    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            var sessions = await httpClientService.GetAsync<List<ActiveSessionDto>>("/api/v1/profile/sessions", ct);

            return sessions ?? new List<ActiveSessionDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active sessions");
            snackbar.Add(
                translationService.GetTranslation("profile.error.sessionsLoadFailed", "Failed to load active sessions"),
                Severity.Error);
            return new List<ActiveSessionDto>();
        }
    }

    public async Task<bool> TerminateSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"/api/v1/profile/sessions/{sessionId}");
            snackbar.Add(
                translationService.GetTranslation("profile.success.sessionTerminated", "Session terminated successfully"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error terminating session {SessionId}", sessionId);
            snackbar.Add(
                translationService.GetTranslation("profile.error.sessionTerminateFailed", "Failed to terminate session"),
                Severity.Error);
            return false;
        }
    }

    public async Task<bool> TerminateAllOtherSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync("/api/v1/profile/sessions/all");
            snackbar.Add(
                translationService.GetTranslation("profile.success.allSessionsTerminated", "All other sessions terminated"),
                Severity.Success);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error terminating all sessions");
            snackbar.Add(
                translationService.GetTranslation("profile.error.allSessionsTerminateFailed", "Failed to terminate sessions"),
                Severity.Error);
            return false;
        }
    }

    public async Task<List<LoginHistoryDto>> GetLoginHistoryAsync(int days = 30, CancellationToken ct = default)
    {
        try
        {
            var history = await httpClientService.GetAsync<List<LoginHistoryDto>>($"/api/v1/profile/login-history?days={days}", ct);

            return history ?? new List<LoginHistoryDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving login history");
            snackbar.Add(
                translationService.GetTranslation("profile.error.historyLoadFailed", "Failed to load login history"),
                Severity.Error);
            return new List<LoginHistoryDto>();
        }
    }
}
