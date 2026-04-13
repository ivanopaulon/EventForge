using Prym.DTOs.Notifications;
using EventForge.Server.Services.Alerts;
using EventForge.Server.Services.Notifications;
using EventForge.Server.Services.Updates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// Unified SignalR hub that consolidates all general-purpose real-time channels:
/// notifications, audit-log updates, price alerts, configuration events and update/maintenance broadcasts.
///
/// Replaces the five separate hubs previously mapped at:
///   /hubs/audit-log, /hubs/notifications, /hubs/alerts, /hubs/configuration, /hubs/update-notifications
///
/// Groups used:
///   user_{userId}            – per-user notifications and status events
///   tenant_{tenantId}        – tenant-wide notifications and alerts
///   system_notifications     – SuperAdmin system-wide broadcast
///   AuditLogUpdates          – SuperAdmin audit stream
///   SuperAdminUpdates        – SuperAdmin management events (impersonation, tenant switch)
///   superadmin               – SuperAdmin update-availability counter
///   all_clients              – maintenance / client-update events sent to everyone
/// </summary>
[Authorize]
public class AppHub(
    ILogger<AppHub> logger,
    INotificationService notificationService,
    ISupplierPriceAlertService alertService,
    UpdatesAvailableRefreshService refreshService) : Hub
{
    #region Connection lifecycle

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        // Every authenticated user gets an individual delivery group and the all_clients maintenance channel.
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_clients");

        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }

        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");
        }

        if (IsInRole("SuperAdmin"))
        {
            // SuperAdmin gets audit, management, system-notifications and update-counter groups.
            await Groups.AddToGroupAsync(Context.ConnectionId, "AuditLogUpdates");
            await Groups.AddToGroupAsync(Context.ConnectionId, "SuperAdminUpdates");
            await Groups.AddToGroupAsync(Context.ConnectionId, "system_notifications");
            await Groups.AddToGroupAsync(Context.ConnectionId, "superadmin");

            logger.LogDebug("AppHub: SuperAdmin {UserId} joined all privileged groups", userId);

            // Immediately push the current update-available count so the badge is up-to-date.
            _ = Task.Run(async () =>
            {
                try { await refreshService.BroadcastCountAsync(); }
                catch (Exception ex) { logger.LogWarning(ex, "AppHub: failed initial UpdatesAvailable broadcast"); }
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_clients");

        if (userId.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

        if (tenantId.HasValue)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");

        if (IsInRole("SuperAdmin"))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuditLogUpdates");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "SuperAdminUpdates");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system_notifications");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "superadmin");
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Notification methods (from NotificationHub)

    /// <summary>Subscribes the caller to specific notification type groups.</summary>
    public async Task SubscribeToNotificationTypes(List<NotificationTypes> notificationTypes)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            var tenantId = GetCurrentTenantId() ?? Guid.Empty;
            foreach (var type in notificationTypes)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications_{type}_{tenantId}");

            logger.LogInformation("AppHub: user {UserId} subscribed to notification types {Types}",
                userId.Value, string.Join(", ", notificationTypes));

            await Clients.Caller.SendAsync("SubscriptionConfirmed", notificationTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to subscribe user {UserId} to notification types", userId.Value);
            throw new HubException("Failed to subscribe to notification types");
        }
    }

    /// <summary>Unsubscribes the caller from specific notification type groups.</summary>
    public async Task UnsubscribeFromNotificationTypes(List<NotificationTypes> notificationTypes)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            var tenantId = GetCurrentTenantId() ?? Guid.Empty;
            foreach (var type in notificationTypes)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications_{type}_{tenantId}");

            logger.LogInformation("AppHub: user {UserId} unsubscribed from notification types {Types}",
                userId.Value, string.Join(", ", notificationTypes));

            await Clients.Caller.SendAsync("UnsubscriptionConfirmed", notificationTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to unsubscribe user {UserId} from notification types", userId.Value);
            throw new HubException("Failed to unsubscribe from notification types");
        }
    }

    /// <summary>Acknowledges a notification and notifies all sessions of the same user.</summary>
    public async Task AcknowledgeNotification(Guid notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            await notificationService.AcknowledgeNotificationAsync(notificationId, userId.Value);

            logger.LogInformation("AppHub: user {UserId} acknowledged notification {NotificationId}",
                userId.Value, notificationId);

            await Clients.Caller.SendAsync("NotificationAcknowledged", notificationId);

            var update = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Acknowledged,
                UserId = userId.Value
            };
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", update);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to acknowledge notification {NotificationId}", notificationId);
            throw new HubException("Failed to acknowledge notification");
        }
    }

    /// <summary>Archives a notification and notifies all sessions of the same user.</summary>
    public async Task ArchiveNotification(Guid notificationId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            await notificationService.ArchiveNotificationAsync(notificationId, userId.Value);

            logger.LogInformation("AppHub: user {UserId} archived notification {NotificationId}",
                userId.Value, notificationId);

            await Clients.Caller.SendAsync("NotificationArchived", notificationId);

            var update = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Archived,
                UserId = userId.Value
            };
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", update);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to archive notification {NotificationId}", notificationId);
            throw new HubException("Failed to archive notification");
        }
    }

    /// <summary>Sends a system-wide notification (SuperAdmin only).</summary>
    public async Task SendSystemNotification(CreateNotificationDto notification)
    {
        if (!IsInRole("SuperAdmin"))
            throw new HubException("Access denied. SuperAdmin role required.");

        notification.TenantId = null;
        notification.SenderId = GetCurrentUserId();

        await Clients.Group("system_notifications").SendAsync("SystemNotificationReceived", notification);

        logger.LogInformation("AppHub: SuperAdmin {UserId} sent system notification", GetCurrentUserId());
    }

    /// <summary>Silences a notification for the caller and notifies all sessions of the same user.</summary>
    public async Task SilenceNotification(Guid notificationId, string? reason = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            await notificationService.SilenceNotificationAsync(notificationId, userId.Value, reason);

            var update = new UpdateNotificationStatusDto
            {
                NotificationId = notificationId,
                Status = NotificationStatus.Silenced,
                UserId = userId.Value,
                Reason = reason
            };

            logger.LogInformation("AppHub: user {UserId} silenced notification {NotificationId}", userId.Value, notificationId);

            await Clients.Caller.SendAsync("NotificationSilenced", notificationId);
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationStatusUpdated", update);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to silence notification {NotificationId}", notificationId);
            throw new HubException("Failed to silence notification");
        }
    }

    /// <summary>Performs a bulk action on multiple notifications (max 100) for the calling user.</summary>
    public async Task BulkNotificationAction(BulkNotificationActionDto action)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        if (action.NotificationIds?.Count > 100)
            throw new HubException("Bulk operations are limited to 100 notifications at a time");

        try
        {
            action.UserId = userId.Value;
            var result = await notificationService.ProcessBulkActionAsync(action);

            logger.LogInformation("AppHub: user {UserId} performed bulk action {Action} on {Count} notifications",
                userId.Value, action.Action, action.NotificationIds?.Count ?? 0);

            await Clients.Caller.SendAsync("BulkActionCompleted", result);
            await Clients.Group($"user_{userId.Value}").SendAsync("NotificationsBulkUpdated", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to perform bulk action for user {UserId}", userId.Value);
            throw new HubException("Failed to perform bulk operation");
        }
    }

    /// <summary>Acknowledges the caller's preferred locale for notification payloads.</summary>
    public async Task UpdateNotificationLocale(string locale)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) throw new HubException("User not authenticated");

        try
        {
            logger.LogInformation("AppHub: user {UserId} updated notification locale to {Locale}", userId.Value, locale);
            await Clients.Caller.SendAsync("LocaleUpdated", locale);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to update notification locale for user {UserId}", userId.Value);
            throw new HubException("Failed to update notification locale");
        }
    }

    /// <summary>Returns notification statistics for the given tenant (Admin/SuperAdmin only).</summary>
    public async Task GetNotificationStats(Guid? tenantId = null)
    {
        var userId = GetCurrentUserId();
        var currentTenantId = GetCurrentTenantId();

        if (!IsInRole("SuperAdmin") && !IsInRole("Admin"))
            throw new HubException("Access denied. Admin role required.");

        if (!IsInRole("SuperAdmin") && tenantId != currentTenantId)
            throw new HubException("Access denied. Cannot view other tenant statistics.");

        try
        {
            var stats = await notificationService.GetNotificationStatisticsAsync(tenantId ?? currentTenantId);
            await Clients.Caller.SendAsync("NotificationStatsReceived", stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to get notification stats for user {UserId}", userId);
            throw new HubException("Failed to retrieve notification statistics");
        }
    }

    #endregion

    #region Audit log methods (from AuditLogHub)

    /// <summary>
    /// Explicitly joins the AuditLogUpdates group (SuperAdmin only).
    /// The hub auto-joins on connect; this method exists for backward compatibility.
    /// </summary>
    public async Task JoinAuditLogGroup()
    {
        if (!IsInRole("SuperAdmin"))
            throw new HubException("Access denied. SuperAdmin role required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, "AuditLogUpdates");
    }

    /// <summary>Leaves the AuditLogUpdates group.</summary>
    public async Task LeaveAuditLogGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuditLogUpdates");
    }

    #endregion

    #region Alert methods (from AlertHub)

    /// <summary>Returns the count of unread price alerts for the current user.</summary>
    public async Task<int> GetUnreadAlertCount()
    {
        try { return await alertService.GetUnreadAlertCountAsync(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "AppHub: failed to get unread alert count");
            return 0;
        }
    }

    #endregion

    #region Configuration methods (from ConfigurationHub)

    /// <summary>Broadcasts a configuration-key change to all connected SuperAdmins (SuperAdmin only).</summary>
    public async Task NotifyConfigurationChanged(string key, string newValue, string changedBy)
    {
        if (!IsInRole("SuperAdmin"))
            throw new HubException("Access denied. SuperAdmin role required.");

        logger.LogInformation("AppHub: configuration changed: {Key} by {User}", key, changedBy);

        await Clients.OthersInGroup("AuditLogUpdates").SendAsync("ConfigurationChanged", new
        {
            Key = key,
            Value = newValue,
            ChangedBy = changedBy,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Notifies all clients that a server restart is required (SuperAdmin only).</summary>
    public async Task NotifyRestartRequired(List<string> reasons)
    {
        if (!IsInRole("SuperAdmin"))
            throw new HubException("Access denied. SuperAdmin role required.");

        logger.LogWarning("AppHub: server restart required: {Reasons}", string.Join(", ", reasons));

        await Clients.All.SendAsync("RestartRequired", new
        {
            Reasons = reasons,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Broadcasts a system-operation outcome to all connected clients (SuperAdmin only).</summary>
    public async Task NotifySystemOperation(string operationType, string action, string description, bool success)
    {
        if (!IsInRole("SuperAdmin"))
            throw new HubException("Access denied. SuperAdmin role required.");

        logger.LogInformation("AppHub: system operation {Type} – {Action} – {Success}", operationType, action, success);

        await Clients.All.SendAsync("SystemOperation", new
        {
            OperationType = operationType,
            Action = action,
            Description = description,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Server-to-client method name constants

    // Maintenance / update events (broadcast by server services, received by all_clients or superadmin groups)
    public const string MaintenanceStarted = "MaintenanceStarted";
    public const string MaintenanceEnded = "MaintenanceEnded";
    public const string ClientUpdateDeployed = "ClientUpdateDeployed";
    public const string UpdateProgress = "UpdateProgress";
    public const string UpdatesAvailable = "UpdatesAvailable";

    // Log-cleanup lifecycle events (SuperAdmin only — sent to "superadmin" group)
    public const string LogCleanupStarted      = "LogCleanupStarted";
    public const string LogCleanupPhaseChanged = "LogCleanupPhaseChanged";
    public const string LogCleanupCompleted    = "LogCleanupCompleted";

    // Notification events
    public const string NotificationReceived = "NotificationReceived";
    public const string TenantNotificationReceived = "TenantNotificationReceived";
    public const string ReceiveBulkNotifications = "ReceiveBulkNotifications";

    // Audit / admin events
    public const string BackupStatusChanged = "BackupStatusChanged";
    public const string TenantSwitched = "TenantSwitched";
    public const string ImpersonationStarted = "ImpersonationStarted";
    public const string ImpersonationEnded = "ImpersonationEnded";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string UserRolesChanged = "UserRolesChanged";
    public const string PasswordReset = "PasswordReset";
    public const string PasswordChangeForced = "PasswordChangeForced";
    public const string BulkUserActionCompleted = "BulkUserActionCompleted";
    public const string UserUpdated = "UserUpdated";
    public const string UserDeleted = "UserDeleted";

    // Alert events
    public const string NewAlert = "NewAlert";

    #endregion

    #region Helpers

    private Guid? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private Guid? GetCurrentTenantId()
    {
        var claim = Context.User?.FindFirst("TenantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private bool IsInRole(string role) => Context.User?.IsInRole(role) == true;

    #endregion
}
