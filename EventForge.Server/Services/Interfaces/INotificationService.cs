using EventForge.DTOs.Notifications;

namespace EventForge.Server.Services.Interfaces;

/// <summary>
/// Interface for notification service operations.
/// This interface defines the contract for notification management operations
/// that will be implemented in Step 3 of the roadmap.
/// </summary>
public interface INotificationService
{
    Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationDto notification);
    Task<bool> AcknowledgeNotificationAsync(Guid notificationId, Guid userId);
    Task<bool> SilenceNotificationAsync(Guid notificationId, Guid userId, string? reason = null);
    Task<bool> ArchiveNotificationAsync(Guid notificationId, Guid userId);
    Task<bool> BulkUpdateNotificationsAsync(BulkNotificationActionDto action);
    Task<NotificationStatsDto> GetNotificationStatsAsync(Guid? tenantId = null);
    Task<IEnumerable<NotificationResponseDto>> GetUserNotificationsAsync(Guid userId, NotificationSearchDto search);
    Task<bool> UpdateUserPreferencesAsync(Guid userId, NotificationPreferencesDto preferences);
    Task ExpireNotificationsAsync(); // Background service method
}