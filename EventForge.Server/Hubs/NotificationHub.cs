using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using EventForge.DTOs.Notifications;
using EventForge.Server.Services.Notifications;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time notification management with multi-tenant support.
/// Handles instant notifications, priority/type management, acknowledgments, 
/// silencing, archiving, expiry, history, localization, and accessibility.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly INotificationService _notificationService;

    public NotificationHub(
        ILogger<NotificationHub> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    #region Connection Management

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically joins user to their tenant notification group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();
        
        if (userId.HasValue)
        {
            // Join user-specific group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            
            if (tenantId.HasValue)
            {
                // Join tenant-wide notification group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");
                _logger.LogInformation("User {UserId} joined notification groups for tenant {TenantId}", userId.Value, tenantId.Value);
            }
            else
            {
                // System-wide notifications for superadmin
                if (IsInRole("SuperAdmin"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "system_notifications");
                    _logger.LogInformation("SuperAdmin {UserId} joined system notifications group", userId.Value);
                }
            }
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();
        
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            
            if (tenantId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");
            }
            else if (IsInRole("SuperAdmin"))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system_notifications");
            }
            
            _logger.LogInformation("User {UserId} disconnected from notification hub", userId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Notification Management

    /// <summary>
    /// Subscribes to specific notification types for the current user.
    /// Used for granular notification filtering based on user preferences.
    /// </summary>
    /// <param name="notificationTypes">List of notification types to subscribe to</param>
    public async Task SubscribeToNotificationTypes(List<NotificationTypes> notificationTypes)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            foreach (var type in notificationTypes)
            {
                var groupName = $"notifications_{type}_{GetCurrentTenantId() ?? Guid.Empty}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }

            _logger.LogInformation("User {UserId} subscribed to notification types: {Types}", 
                userId.Value, string.Join(", ", notificationTypes));
            
            await Clients.Caller.SendAsync("SubscriptionConfirmed", notificationTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe user {UserId} to notification types", userId.Value);
            throw new HubException("Failed to subscribe to notification types");
        }
    }

    /// <summary>
    /// Unsubscribes from specific notification types.
    /// </summary>
    /// <param name="notificationTypes">List of notification types to unsubscribe from</param>
    public async Task UnsubscribeFromNotificationTypes(List<NotificationTypes> notificationTypes)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            foreach (var type in notificationTypes)
            {
                var groupName = $"notifications_{type}_{GetCurrentTenantId() ?? Guid.Empty}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            _logger.LogInformation("User {UserId} unsubscribed from notification types: {Types}", 
                userId.Value, string.Join(", ", notificationTypes));
                
            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", notificationTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe user {UserId} from notification types", userId.Value);
            throw new HubException("Failed to unsubscribe from notification types");
        }
    }

    /// <summary>
    /// Acknowledges a notification, marking it as read and acknowledged.
    /// </summary>
    /// <param name="notificationId">ID of the notification to acknowledge</param>
    public async Task AcknowledgeNotification(Guid notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // Call the notification service to acknowledge the notification
            var result = await _notificationService.AcknowledgeNotificationAsync(notificationId, userId.Value);
            
            _logger.LogInformation("User {UserId} acknowledged notification {NotificationId}", userId.Value, notificationId);
            
            // Notify the user that the acknowledgment was successful
            await Clients.Caller.SendAsync("NotificationAcknowledged", notificationId);
            
            // Create update DTO for other user sessions
            var updateDto = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Acknowledged,
                UserId = userId.Value
            };
            
            // Notify other connected devices/sessions of the same user
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge notification {NotificationId} for user {UserId}", notificationId, userId.Value);
            throw new HubException("Failed to acknowledge notification");
        }
    }

    /// <summary>
    /// Silences a notification, preventing further alerts while keeping it visible.
    /// </summary>
    /// <param name="notificationId">ID of the notification to silence</param>
    /// <param name="reason">Optional reason for silencing</param>
    public async Task SilenceNotification(Guid notificationId, string? reason = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // TODO: Implement notification service call
            var updateDto = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Silenced,
                UserId = userId.Value,
                Reason = reason
            };

            _logger.LogInformation("User {UserId} silenced notification {NotificationId} with reason: {Reason}", 
                userId.Value, notificationId, reason ?? "No reason provided");
            
            await Clients.Caller.SendAsync("NotificationSilenced", notificationId);
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to silence notification {NotificationId} for user {UserId}", notificationId, userId.Value);
            throw new HubException("Failed to silence notification");
        }
    }

    /// <summary>
    /// Archives a notification, moving it to the archive for future reference.
    /// </summary>
    /// <param name="notificationId">ID of the notification to archive</param>
    public async Task ArchiveNotification(Guid notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // TODO: Implement notification service call
            var updateDto = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Archived,
                UserId = userId.Value
            };

            _logger.LogInformation("User {UserId} archived notification {NotificationId}", userId.Value, notificationId);
            
            await Clients.Caller.SendAsync("NotificationArchived", notificationId);
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive notification {NotificationId} for user {UserId}", notificationId, userId.Value);
            throw new HubException("Failed to archive notification");
        }
    }

    /// <summary>
    /// Performs bulk operations on multiple notifications (acknowledge, silence, archive).
    /// </summary>
    /// <param name="action">The bulk action to perform</param>
    public async Task BulkNotificationAction(BulkNotificationActionDto action)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        if (action.NotificationIds?.Count > 100)
        {
            throw new HubException("Bulk operations are limited to 100 notifications at a time");
        }

        try
        {
            // TODO: Implement notification service call
            action.UserId = userId.Value;

            _logger.LogInformation("User {UserId} performed bulk action {Action} on {Count} notifications", 
                userId.Value, action.Action, action.NotificationIds?.Count ?? 0);
            
            await Clients.Caller.SendAsync("BulkActionCompleted", action);
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationsBulkUpdated", action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform bulk action for user {UserId}", userId.Value);
            throw new HubException("Failed to perform bulk operation");
        }
    }

    #endregion

    #region Localization Support

    /// <summary>
    /// Updates the user's preferred locale for notifications.
    /// </summary>
    /// <param name="locale">Preferred locale (e.g., "en-US", "it-IT")</param>
    public async Task UpdateNotificationLocale(string locale)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // TODO: Implement user preferences service call
            _logger.LogInformation("User {UserId} updated notification locale to {Locale}", userId.Value, locale);
            
            await Clients.Caller.SendAsync("LocaleUpdated", locale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update locale for user {UserId}", userId.Value);
            throw new HubException("Failed to update locale");
        }
    }

    #endregion

    #region Admin/SuperAdmin Features

    /// <summary>
    /// Sends a system-wide notification (SuperAdmin only).
    /// </summary>
    /// <param name="notification">Notification to send</param>
    public async Task SendSystemNotification(CreateNotificationDto notification)
    {
        if (!IsInRole("SuperAdmin"))
        {
            throw new HubException("Access denied. SuperAdmin role required.");
        }

        try
        {
            // TODO: Implement notification service call
            notification.TenantId = null; // System-wide
            notification.SenderId = GetCurrentUserId();

            _logger.LogInformation("SuperAdmin {UserId} sent system notification", GetCurrentUserId());
            
            // Send to all connected users
            await Clients.Group("system_notifications").SendAsync("SystemNotificationReceived", notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send system notification");
            throw new HubException("Failed to send system notification");
        }
    }

    /// <summary>
    /// Gets notification statistics for a tenant (Admin/SuperAdmin only).
    /// </summary>
    /// <param name="tenantId">Tenant ID to get stats for (null for all tenants)</param>
    public async Task GetNotificationStats(Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var currentTenantId = GetCurrentTenantId();
        
        // Check permissions
        if (!IsInRole("SuperAdmin") && !IsInRole("Admin"))
        {
            throw new HubException("Access denied. Admin role required.");
        }

        // Non-SuperAdmin users can only view their own tenant stats
        if (!IsInRole("SuperAdmin") && tenantId != currentTenantId)
        {
            throw new HubException("Access denied. Cannot view other tenant statistics.");
        }

        try
        {
            // TODO: Implement notification service call
            var stats = new NotificationStatsDto
            {
                TenantId = tenantId ?? currentTenantId,
                // Placeholder data - will be populated by service
                LastCalculated = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("NotificationStatsReceived", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification stats for user {UserId}", userId);
            throw new HubException("Failed to retrieve notification statistics");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the current user ID from the connection context.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current tenant ID from the connection context.
    /// </summary>
    private Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    /// <summary>
    /// Checks if the current user is in a specific role.
    /// </summary>
    private bool IsInRole(string role)
    {
        return Context.User?.IsInRole(role) == true;
    }

    #endregion
}