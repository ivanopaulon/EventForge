using EventForge.DTOs.Profile;

namespace EventForge.Client.Services;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync();
    Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileDto updateDto);
    Task<UserProfileDto?> UploadAvatarAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteAvatarAsync();
    Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<UserProfileDto?> UpdateNotificationPreferencesAsync(UpdateNotificationPreferencesDto preferencesDto);
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync();
    Task<bool> TerminateSessionAsync(Guid sessionId);
    Task<bool> TerminateAllOtherSessionsAsync();
    Task<List<LoginHistoryDto>> GetLoginHistoryAsync(int days = 30);
}
