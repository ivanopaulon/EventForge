using Prym.DTOs.Common;
using Prym.DTOs.Notifications;

namespace Prym.Web.Services;

/// <summary>
/// Service for handling notification operations and real-time updates.
/// Integrates with REST API endpoints and SignalR for real-time functionality.
/// </summary>
public interface INotificationService
{
    Task<List<NotificationResponseDto>> GetNotificationsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default);
    Task<NotificationResponseDto?> GetNotificationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationResponseDto> SendNotificationAsync(CreateNotificationDto createDto, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task<bool> ArchiveNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAllReadAsync(CancellationToken cancellationToken = default);
    Task<bool> SilenceNotificationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationStatsDto> GetNotificationStatsAsync(CancellationToken cancellationToken = default);
    Task<List<NotificationTypes>> GetSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateSubscriptionsAsync(List<NotificationTypes> subscriptions, CancellationToken cancellationToken = default);

    // Preferences
    Task<NotificationPreferencesDto> GetUserPreferencesAsync(CancellationToken cancellationToken = default);
    Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(NotificationPreferencesDto preferences, CancellationToken cancellationToken = default);

    // Activity feed
    Task<PagedResult<ActivityFeedEntryDto>> GetActivityFeedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    // Events for real-time updates
    event Action<NotificationResponseDto>? NotificationReceived;
    event Action<Guid>? NotificationRead;
    event Action<Guid>? NotificationArchived;
    event Action<NotificationStatsDto>? StatsUpdated;
}

public class NotificationService : INotificationService
{
    private const string BaseUrl = "api/v1/notifications";
    private readonly IHttpClientService _httpClientService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientService httpClientService,
        IRealtimeService realtimeService,
        ILogger<NotificationService> logger)
    {
        _httpClientService = httpClientService;
        _realtimeService = realtimeService;
        _logger = logger;

        // Subscribe to real-time events
        _realtimeService.NotificationReceived += OnNotificationReceived;
        _realtimeService.NotificationAcknowledged += OnNotificationAcknowledged;
        _realtimeService.NotificationArchived += OnNotificationArchived;
    }

    #region Events
    public event Action<NotificationResponseDto>? NotificationReceived;
    public event Action<Guid>? NotificationRead;
    public event Action<Guid>? NotificationArchived;
    public event Action<NotificationStatsDto>? StatsUpdated;
    #endregion

    #region API Operations

    public async Task<List<NotificationResponseDto>> GetNotificationsAsync(int page = 1, int pageSize = 50, string? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"status={filter}");
            }

            var query = string.Join("&", queryParams);
            var pagedResult = await _httpClientService.GetAsync<PagedResult<NotificationResponseDto>>($"api/v1/notifications?{query}", cancellationToken);
            return pagedResult?.Items?.ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications (page={Page}, pageSize={PageSize}, filter={Filter}): {ExceptionType} - {Message}",
                page, pageSize, filter, ex.GetType().Name, ex.Message);
            return [];
        }
    }

    public async Task<NotificationResponseDto?> GetNotificationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<NotificationResponseDto>($"api/v1/notifications/{id}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {Id}: {ExceptionType} - {Message}", id, ex.GetType().Name, ex.Message);
            return null;
        }
    }

    public async Task<NotificationResponseDto> SendNotificationAsync(CreateNotificationDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateNotificationDto, NotificationResponseDto>("api/v1/notifications", createDto, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to send notification: the HTTP call succeeded but the server returned an empty response body");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification (Type={Type}): {ExceptionType} - {Message}",
                createDto.Type, ex.GetType().Name, ex.Message);
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/read", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read: {ExceptionType} - {Message}", id, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PostAsync("api/v1/notifications/mark-all-read", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<bool> ArchiveNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/archive", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {Id}: {ExceptionType} - {Message}", id, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<bool> ArchiveAllReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PostAsync("api/v1/notifications/archive-read", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving read notifications: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<bool> SilenceNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/silence", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error silencing notification {Id}: {ExceptionType} - {Message}", id, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<NotificationStatsDto>("api/v1/notifications/statistics", cancellationToken) ?? new NotificationStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return new NotificationStatsDto();
        }
    }

    public async Task<List<NotificationTypes>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<List<NotificationTypes>>("api/v1/notifications/subscriptions", cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification subscriptions: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return [];
        }
    }

    public async Task<bool> UpdateSubscriptionsAsync(List<NotificationTypes> subscriptions, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PutAsync("api/v1/notifications/subscriptions", subscriptions, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification subscriptions ({Count} items): {ExceptionType} - {Message}",
                subscriptions.Count, ex.GetType().Name, ex.Message);
            return false;
        }
    }

    #endregion

    #region Preferences & Activity Feed

    public async Task<NotificationPreferencesDto> GetUserPreferencesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<NotificationPreferencesDto>("api/v1/notifications/preferences", cancellationToken)
                ?? new NotificationPreferencesDto { NotificationsEnabled = true, MinPriority = NotificationPriority.Low, SoundEnabled = true, AutoArchiveAfterDays = 30 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notification preferences.");
            return new NotificationPreferencesDto { NotificationsEnabled = true, MinPriority = NotificationPriority.Low, SoundEnabled = true, AutoArchiveAfterDays = 30 };
        }
    }

    public async Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(NotificationPreferencesDto preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PutAsync<NotificationPreferencesDto, NotificationPreferencesDto>("api/v1/notifications/preferences", preferences, cancellationToken);
            return result ?? preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification preferences.");
            throw;
        }
    }

    public async Task<PagedResult<ActivityFeedEntryDto>> GetActivityFeedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<PagedResult<ActivityFeedEntryDto>>(
                $"api/v1/notifications/activity-feed?page={page}&pageSize={pageSize}", cancellationToken)
                ?? new PagedResult<ActivityFeedEntryDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load activity feed.");
            return new PagedResult<ActivityFeedEntryDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
    }

    #endregion

    #region SignalR Event Handlers

    private void OnNotificationReceived(NotificationResponseDto notification)
    {
        NotificationReceived?.Invoke(notification);
    }

    private void OnNotificationAcknowledged(Guid notificationId)
    {
        NotificationRead?.Invoke(notificationId);
    }

    private void OnNotificationArchived(Guid notificationId)
    {
        NotificationArchived?.Invoke(notificationId);
    }

    private void OnStatsReceived(NotificationStatsDto stats)
    {
        StatsUpdated?.Invoke(stats);
    }

    #endregion
}