using Prym.DTOs.Profile;

namespace EventForge.Client.Services;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync(CancellationToken ct = default);
    Task<UserProfileDto?> UpdateProfileAsync(UpdateProfileDto updateDto, CancellationToken ct = default);
    Task<UserProfileDto?> UploadAvatarAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<bool> DeleteAvatarAsync(CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(ChangePasswordDto changePasswordDto, CancellationToken ct = default);
    Task<UserProfileDto?> UpdateNotificationPreferencesAsync(UpdateNotificationPreferencesDto preferencesDto, CancellationToken ct = default);
    Task<List<ActiveSessionDto>> GetActiveSessionsAsync(CancellationToken ct = default);
    Task<bool> TerminateSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<bool> TerminateAllOtherSessionsAsync(CancellationToken ct = default);
    Task<List<LoginHistoryDto>> GetLoginHistoryAsync(int days = 30, CancellationToken ct = default);
}
