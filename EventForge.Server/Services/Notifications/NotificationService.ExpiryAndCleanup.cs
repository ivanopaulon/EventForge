using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Processes expired notifications with cleanup policies.
    /// Implements batch processing with retention policies: >90 days delete, >30 days archive, else expire.
    /// </summary>
    public async Task<ExpiryProcessingResultDto> ProcessExpiredNotificationsAsync(
        Guid? tenantId = null,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var now = DateTime.UtcNow;

        var expiredCount = 0;
        var archivedCount = 0;
        var deletedCount = 0;
        var processedCount = 0;

        logger.LogInformation(
            "Processing expired notifications for tenant {TenantId} with batch size {BatchSize}",
            tenantId?.ToString() ?? "ALL", batchSize);

        try
        {
            // 1. Query expired notifications and get all IDs upfront to avoid infinite loop
            var query = context.Notifications
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now)
                .Where(n => n.Status != NotificationStatus.Archived && n.Status != NotificationStatus.Expired);

            // 2. Apply tenant filter if provided
            if (tenantId.HasValue)
            {
                query = query.Where(n => n.TenantId == tenantId.Value);
            }

            // Get all IDs that need processing to avoid fetching new items in loop
            var expiredNotificationIds = await query
                .OrderBy(n => n.ExpiresAt)
                .Select(n => new { n.Id, n.ExpiresAt })
                .ToListAsync(cancellationToken);


            // 3. Process in batches to avoid memory issues
            var remainingIds = expiredNotificationIds.Select(n => n.Id).ToList();

            while (remainingIds.Any())
            {
                // Get batch of IDs
                var batchIds = remainingIds.Take(batchSize).ToList();

                // Fetch the actual entities for this batch
                var batch = await context.Notifications
                    .Where(n => batchIds.Contains(n.Id))
                    .ToListAsync(cancellationToken);

                foreach (var notification in batch)
                {
                    // 4. Apply expiry policy based on days since expiry
                    var daysSinceExpiry = (now - notification.ExpiresAt!.Value).Days;

                    if (daysSinceExpiry > 90)
                    {
                        // Hard delete old notifications (>90 days expired)
                        context.Notifications.Remove(notification);
                        deletedCount++;
                        logger.LogDebug("Deleting notification {NotificationId} expired {Days} days ago",
                            notification.Id, daysSinceExpiry);
                    }
                    else if (daysSinceExpiry > 30)
                    {
                        // Archive moderately old notifications (>30 days expired)
                        notification.Status = NotificationStatus.Archived;
                        notification.IsArchived = true;
                        notification.ArchivedAt = now;
                        notification.ModifiedAt = now;
                        archivedCount++;
                        logger.LogDebug("Archiving notification {NotificationId} expired {Days} days ago",
                            notification.Id, daysSinceExpiry);
                    }
                    else
                    {
                        // Mark as expired (recently expired, <30 days)
                        notification.Status = NotificationStatus.Expired;
                        notification.ModifiedAt = now;
                        expiredCount++;
                        logger.LogDebug("Expiring notification {NotificationId} expired {Days} days ago",
                            notification.Id, daysSinceExpiry);
                    }
                }

                // 5. Save changes for this batch
                await context.SaveChangesAsync(cancellationToken);
                processedCount += batch.Count;

                // Remove processed IDs from remaining list
                remainingIds = remainingIds.Skip(batchSize).ToList();

                // 6. Log progress
                logger.LogInformation(
                    "Processed {ProcessedCount} expired notifications ({Expired} expired, {Archived} archived, {Deleted} deleted)",
                    processedCount, expiredCount, archivedCount, deletedCount);
            }

            // 7. Log audit trail for cleanup operation
            await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationCleanup",
                entityId: Guid.NewGuid(),
                propertyName: "Cleanup",
                operationType: "Delete",
                oldValue: null,
                newValue: $"Processed: {processedCount}, Expired: {expiredCount}, Archived: {archivedCount}, Deleted: {deletedCount}",
                changedBy: "System",
                entityDisplayName: "Notification Expiry Cleanup",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Completed expired notification processing: {ProcessedCount} processed, {ExpiredCount} expired, {ArchivedCount} archived, {DeletedCount} deleted in {ElapsedMs}ms",
                processedCount, expiredCount, archivedCount, deletedCount, stopwatch.ElapsedMilliseconds);

            // 8. Return results
            return new ExpiryProcessingResultDto
            {
                ProcessedCount = processedCount,
                ExpiredCount = expiredCount,
                ArchivedCount = archivedCount,
                DeletedCount = deletedCount,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process expired notifications");

            return new ExpiryProcessingResultDto
            {
                ProcessedCount = processedCount,
                ExpiredCount = expiredCount,
                ArchivedCount = archivedCount,
                DeletedCount = deletedCount,
                ProcessingTime = stopwatch.Elapsed,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Cleans up notification data based on retention policies — deletes expired/archived records from the database.
    /// </summary>
    public async Task<CleanupResultDto> CleanupNotificationDataAsync(
        NotificationCleanupPolicyDto cleanupPolicy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var cutoff = DateTime.UtcNow - cleanupPolicy.RetentionPeriod;

            logger.LogInformation(
                "Cleaning up notification data for tenant {TenantId} older than {Cutoff}",
                cleanupPolicy.TenantId?.ToString() ?? "ALL", cutoff);

            var query = context.Notifications
                .Where(n => n.CreatedAt < cutoff && !n.IsDeleted);

            if (cleanupPolicy.TenantId.HasValue)
                query = query.Where(n => n.TenantId == cleanupPolicy.TenantId.Value);

            if (!cleanupPolicy.IncludeArchived)
                query = query.Where(n => !n.IsArchived);

            if (cleanupPolicy.TypeFilter?.Count > 0)
                query = query.Where(n => cleanupPolicy.TypeFilter.Contains(n.Type));

            var toDelete = await query.ToListAsync(cancellationToken);

            if (toDelete.Count == 0)
            {
                return new CleanupResultDto
                {
                    CleanedCount = 0,
                    AnonymizedCount = 0,
                    FreedBytes = 0,
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            var notificationIds = toDelete.Select(n => n.Id).ToList();

            // Remove associated recipients first to respect FK constraints
            var recipients = await context.NotificationRecipients
                .Where(r => notificationIds.Contains(r.NotificationId))
                .ToListAsync(cancellationToken);

            context.NotificationRecipients.RemoveRange(recipients);
            context.Notifications.RemoveRange(toDelete);
            _ = await context.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Cleaned up {Count} notifications (+{RecipCount} recipients) for tenant {TenantId} in {Elapsed}ms.",
                toDelete.Count, recipients.Count, cleanupPolicy.TenantId?.ToString() ?? "ALL", stopwatch.ElapsedMilliseconds);

            return new CleanupResultDto
            {
                CleanedCount = toDelete.Count,
                AnonymizedCount = 0,
                FreedBytes = 0,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up notification data.");
            throw;
        }
    }

}
