using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Acknowledges a notification with audit logging.
    /// Implements database update with status change and audit trail.
    /// </summary>
    public async Task<NotificationResponseDto> AcknowledgeNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        try
        {
            // Find the notification recipient
            var notificationRecipient = await context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient is null)
            {
                throw new InvalidOperationException($"Notification {notificationId} not found for user {userId}");
            }

            // Update status
            var previousStatus = notificationRecipient.Status;
            notificationRecipient.Status = NotificationStatus.Acknowledged;
            notificationRecipient.AcknowledgedAt = now;
            notificationRecipient.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt

            // If not already read, mark as read too
            if (notificationRecipient.ReadAt is null)
            {
                notificationRecipient.ReadAt = now;
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Acknowledged",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Acknowledgment: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} acknowledged notification {NotificationId} with reason: {Reason}",
                userId, notificationId, reason ?? "No reason provided");

            // Return updated notification
            var senderName3 = await ResolveUserNameAsync(notificationRecipient.Notification.SenderId, cancellationToken);
            return new NotificationResponseDto
            {
                Id = notificationRecipient.Notification.Id,
                TenantId = notificationRecipient.TenantId,
                SenderId = notificationRecipient.Notification.SenderId,
                SenderName = senderName3,
                RecipientIds = new List<Guid> { userId },
                Type = notificationRecipient.Notification.Type,
                Priority = notificationRecipient.Notification.Priority,
                Status = NotificationStatus.Acknowledged,
                Payload = new NotificationPayloadDto
                {
                    Title = notificationRecipient.Notification.Title,
                    Message = notificationRecipient.Notification.Message,
                    ActionUrl = notificationRecipient.Notification.ActionUrl,
                    IconUrl = notificationRecipient.Notification.IconUrl,
                    Locale = notificationRecipient.Notification.PayloadLocale
                },
                ExpiresAt = notificationRecipient.Notification.ExpiresAt,
                CreatedAt = notificationRecipient.Notification.CreatedAt,
                ReadAt = notificationRecipient.ReadAt,
                AcknowledgedAt = now,
                Metadata = new Dictionary<string, object>
                {
                    ["AcknowledgedBy"] = userId,
                    ["AcknowledgedReason"] = reason ?? "No reason provided"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to acknowledge notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to acknowledge notification", ex);
        }
    }

    /// <summary>
    /// Silences a notification with optional expiry.
    /// Implements database update with status change and audit trail.
    /// </summary>
    public async Task<NotificationResponseDto> SilenceNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        try
        {
            // Find the notification recipient
            var notificationRecipient = await context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient is null)
            {
                throw new InvalidOperationException($"Notification {notificationId} not found for user {userId}");
            }

            // Update status
            var previousStatus = notificationRecipient.Status;
            notificationRecipient.Status = NotificationStatus.Silenced;
            notificationRecipient.SilencedAt = now;
            notificationRecipient.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt

            // Update silence expiry if provided
            if (expiresAt.HasValue)
            {
                notificationRecipient.SilencedUntil = expiresAt.Value;
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Silenced",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Silenced: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} silenced notification {NotificationId} until {ExpiresAt} with reason: {Reason}",
                userId, notificationId, expiresAt?.ToString() ?? "permanent", reason ?? "No reason provided");

            return new NotificationResponseDto
            {
                Id = notificationRecipient.Notification.Id,
                TenantId = notificationRecipient.TenantId,
                Status = NotificationStatus.Silenced,
                SilencedAt = now,
                Metadata = new Dictionary<string, object>
                {
                    ["SilencedBy"] = userId,
                    ["SilencedReason"] = reason ?? "No reason provided",
                    ["SilencedUntil"] = expiresAt?.ToString() ?? "permanent"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to silence notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to silence notification", ex);
        }
    }

    /// <summary>
    /// Archives a notification for long-term storage.
    /// Implements database update with status change and audit trail.
    /// </summary>
    public async Task<NotificationResponseDto> ArchiveNotificationAsync(
        Guid notificationId,
        Guid userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        try
        {
            // Find the notification recipient
            var notificationRecipient = await context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient is null)
            {
                throw new InvalidOperationException($"Notification {notificationId} not found for user {userId}");
            }

            // Update status
            var previousStatus = notificationRecipient.Status;
            notificationRecipient.Status = NotificationStatus.Archived;
            notificationRecipient.ArchivedAt = now;
            notificationRecipient.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt

            // Update notification archive flag if all recipients have archived
            var allRecipients = await context.NotificationRecipients
                .AsNoTracking()
                .Where(nr => nr.NotificationId == notificationId)
                .ToListAsync(cancellationToken);

            if (allRecipients.All(nr => nr.Status == NotificationStatus.Archived || nr.UserId == userId))
            {
                notificationRecipient.Notification.IsArchived = true;
                notificationRecipient.Notification.ArchivedAt = now;
                notificationRecipient.Notification.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt
            }

            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Archived",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Archived: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} archived notification {NotificationId} with reason: {Reason}",
                userId, notificationId, reason ?? "No reason provided");

            return new NotificationResponseDto
            {
                Id = notificationRecipient.Notification.Id,
                TenantId = notificationRecipient.TenantId,
                Status = NotificationStatus.Archived,
                ArchivedAt = now,
                Metadata = new Dictionary<string, object>
                {
                    ["ArchivedBy"] = userId,
                    ["ArchivedReason"] = reason ?? "No reason provided"
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to archive notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to archive notification", ex);
        }
    }

    /// <summary>
    /// Processes bulk status operations on notifications.
    /// Applies the requested action to each notification individually.
    /// </summary>
    public async Task<BulkNotificationResultDto> ProcessBulkActionAsync(
        BulkNotificationActionDto bulkAction,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<NotificationOperationResult>();
        var successCount = 0;
        var failureCount = 0;

        logger.LogInformation(
            "Processing bulk action {Action} on {Count} notifications for user {UserId}",
            bulkAction.Action, bulkAction.NotificationIds.Count, bulkAction.UserId);

        foreach (var notificationId in bulkAction.NotificationIds)
        {
            try
            {
                NotificationResponseDto result = bulkAction.Action switch
                {
                    NotificationStatus.Acknowledged => await AcknowledgeNotificationAsync(
                        notificationId, bulkAction.UserId ?? Guid.Empty, bulkAction.Reason, cancellationToken),
                    NotificationStatus.Silenced => await SilenceNotificationAsync(
                        notificationId, bulkAction.UserId ?? Guid.Empty, bulkAction.Reason, null, cancellationToken),
                    NotificationStatus.Archived => await ArchiveNotificationAsync(
                        notificationId, bulkAction.UserId ?? Guid.Empty, bulkAction.Reason, cancellationToken),
                    _ => throw new ArgumentException($"Unsupported bulk action: {bulkAction.Action}")
                };

                results.Add(new NotificationOperationResult
                {
                    NotificationId = notificationId,
                    Success = true,
                    Metadata = new Dictionary<string, object> { ["ProcessedAt"] = DateTime.UtcNow }
                });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new NotificationOperationResult
                {
                    NotificationId = notificationId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorCode = "BULK_ACTION_FAILED"
                });
                failureCount++;

                logger.LogWarning(ex, "Failed to process bulk action for notification {NotificationId}", notificationId);
            }
        }

        return new BulkNotificationResultDto
        {
            TotalCount = bulkAction.NotificationIds.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            ProcessingTime = stopwatch.Elapsed
        };
    }

}
