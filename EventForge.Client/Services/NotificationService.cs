using EventForge.DTOs.Common;
using EventForge.DTOs.Notifications;

namespace EventForge.Client.Services;

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

    // Events for real-time updates
    event Action<NotificationResponseDto>? NotificationReceived;
    event Action<Guid>? NotificationRead;
    event Action<Guid>? NotificationArchived;
    event Action<NotificationStatsDto>? StatsUpdated;
}

public class NotificationService : INotificationService
{
    private readonly IHttpClientService _httpClientService;
    private readonly SignalRService _signalRService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientService httpClientService,
        SignalRService signalRService,
        ILogger<NotificationService> logger)
    {
        _httpClientService = httpClientService;
        _signalRService = signalRService;
        _logger = logger;

        // Subscribe to SignalR events
        _signalRService.NotificationReceived += OnNotificationReceived;
        _signalRService.NotificationAcknowledged += OnNotificationAcknowledged;
        _signalRService.NotificationArchived += OnNotificationArchived;
        _signalRService.NotificationStatsReceived += OnStatsReceived;
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
            return pagedResult?.Items?.ToList() ?? new List<NotificationResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return new List<NotificationResponseDto>();
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
            _logger.LogError(ex, "Error getting notification {Id}", id);
            return null;
        }
    }

    public async Task<NotificationResponseDto> SendNotificationAsync(CreateNotificationDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateNotificationDto, NotificationResponseDto>("api/v1/notifications", createDto, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to send notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/read", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
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
            _logger.LogError(ex, "Error marking all notifications as read");
            return false;
        }
    }

    public async Task<bool> ArchiveNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/archive", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {Id}", id);
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
            _logger.LogError(ex, "Error archiving read notifications");
            return false;
        }
    }

    public async Task<bool> SilenceNotificationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.PatchAsync<object, object>($"api/v1/notifications/{id}/silence", new { }, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error silencing notification {Id}", id);
            return false;
        }
    }

    public async Task<NotificationStatsDto> GetNotificationStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<NotificationStatsDto>("api/v1/notifications/stats", cancellationToken) ?? new NotificationStatsDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats");
            return new NotificationStatsDto();
        }
    }

    public async Task<List<NotificationTypes>> GetSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<List<NotificationTypes>>("api/v1/notifications/subscriptions", cancellationToken) ?? new List<NotificationTypes>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification subscriptions");
            return new List<NotificationTypes>();
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
            _logger.LogError(ex, "Error updating notification subscriptions");
            return false;
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