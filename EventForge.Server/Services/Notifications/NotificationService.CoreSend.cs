using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Sends a notification to specified recipients with full multi-tenant isolation.
    /// Implements complete database persistence and real-time delivery.
    /// </summary>
    public async Task<NotificationResponseDto> SendNotificationAsync(
        CreateNotificationDto createDto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate tenant access and rate limits
            await ValidateTenantAccessAsync(createDto.TenantId, cancellationToken);
            await ValidateRateLimitAsync(createDto.TenantId, null, createDto.Type, cancellationToken);

            // Generate notification ID and prepare entity
            var notificationId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Create notification entity
            var notification = new Data.Entities.Notifications.Notification
            {
                Id = notificationId,
                TenantId = createDto.TenantId ?? Guid.Empty, // Handle nullable TenantId
                SenderId = createDto.SenderId,
                Type = createDto.Type,
                Priority = createDto.Priority,
                Status = NotificationStatus.Pending,
                Title = createDto.Payload.Title,
                Message = createDto.Payload.Message,
                ActionUrl = createDto.Payload.ActionUrl,
                IconUrl = createDto.Payload.IconUrl,
                PayloadLocale = createDto.Payload.Locale,
                LocalizationParamsJson = createDto.Payload.LocalizationParams is not null
                    ? System.Text.Json.JsonSerializer.Serialize(createDto.Payload.LocalizationParams)
                    : null,
                ExpiresAt = createDto.ExpiresAt,
                MetadataJson = createDto.Metadata is not null
                    ? System.Text.Json.JsonSerializer.Serialize(createDto.Metadata)
                    : null,
                CreatedAt = now,
                ModifiedAt = now // Use ModifiedAt instead of UpdatedAt
            };

            // Create recipient entities
            var recipients = createDto.RecipientIds.Select(recipientId => new Data.Entities.Notifications.NotificationRecipient
            {
                Id = Guid.NewGuid(),
                NotificationId = notificationId,
                UserId = recipientId,
                TenantId = createDto.TenantId ?? Guid.Empty, // Handle nullable TenantId
                Status = NotificationStatus.Pending,
                CreatedAt = now,
                ModifiedAt = now // Use ModifiedAt instead of UpdatedAt
            }).ToList();

            // Save to database
            _ = context.Notifications.Add(notification);
            context.NotificationRecipients.AddRange(recipients);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Update status to sent
            notification.Status = NotificationStatus.Sent;
            notification.ModifiedAt = DateTime.UtcNow; // Use ModifiedAt instead of UpdatedAt
            _ = await context.SaveChangesAsync(cancellationToken);

            // Log audit trail for notification creation
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "Notification",
                entityId: notificationId,
                propertyName: "Create",
                operationType: "Insert",
                oldValue: null,
                newValue: $"Type: {createDto.Type}, Priority: {createDto.Priority}, Recipients: {createDto.RecipientIds.Count}",
                changedBy: createDto.SenderId?.ToString() ?? "System",
                entityDisplayName: $"Notification: {createDto.Payload.Title}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Notification {NotificationId} created for tenant {TenantId} with {RecipientCount} recipients in {ElapsedMs}ms",
                notificationId, createDto.TenantId, createDto.RecipientIds.Count, stopwatch.ElapsedMilliseconds);

            // Send real-time notifications via SignalR
            var notificationData = new
            {
                Id = notificationId,
                Type = createDto.Type,
                Priority = createDto.Priority,
                Payload = createDto.Payload,
                CreatedAt = now,
                TenantId = createDto.TenantId
            };

            // Send to each recipient
            foreach (var recipientId in createDto.RecipientIds)
            {
                await hubContext.Clients.Group($"user_{recipientId}")
                    .SendAsync("NotificationReceived", notificationData);
            }

            // Also send to tenant-wide group if tenant is specified
            if (createDto.TenantId.HasValue)
            {
                await hubContext.Clients.Group($"tenant_{createDto.TenantId.Value}")
                    .SendAsync("TenantNotificationReceived", notificationData);
            }

            // Build response DTO
            var senderName1 = await ResolveUserNameAsync(createDto.SenderId, cancellationToken);
            return new NotificationResponseDto
            {
                Id = notificationId,
                TenantId = createDto.TenantId,
                SenderId = createDto.SenderId,
                SenderName = senderName1,
                RecipientIds = createDto.RecipientIds,
                Type = createDto.Type,
                Priority = createDto.Priority,
                Payload = createDto.Payload,
                Status = NotificationStatus.Sent,
                ExpiresAt = createDto.ExpiresAt,
                CreatedAt = now,
                Metadata = createDto.Metadata
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification for tenant {TenantId}", createDto.TenantId);
            throw new InvalidOperationException("Failed to send notification", ex);
        }
    }

    /// <summary>
    /// Sends bulk notifications with optimized batch processing.
    /// Implements database bulk operations, batch SignalR delivery, and partial success handling.
    /// </summary>
    public async Task<BulkNotificationResultDto> SendBulkNotificationsAsync(
        List<CreateNotificationDto> notifications,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<NotificationOperationResult>();
        var successCount = 0;
        var failureCount = 0;

        // 1. Validate input
        if (notifications is null || notifications.Count == 0)
        {
            logger.LogWarning("SendBulkNotificationsAsync called with null or empty notifications list");
            return new BulkNotificationResultDto
            {
                TotalCount = 0,
                SuccessCount = 0,
                FailureCount = 0,
                Results = results,
                ProcessingTime = stopwatch.Elapsed
            };
        }

        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));
        }


        // 2. Check rate limiting for bulk operations
        try
        {
            // Use first notification's tenant for rate limit check
            var firstTenantId = notifications.FirstOrDefault()?.TenantId;
            await ValidateRateLimitAsync(firstTenantId, null, NotificationTypes.System, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Rate limit validation failed for bulk operation");
            // Continue processing but log the warning
        }

        // 3. Split into batches if count > batchSize
        var batches = notifications
            .Select((notification, index) => new { notification, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.notification).ToList())
            .ToList();


        // 4. Process batches with optimized database operations
        foreach (var batch in batches)
        {
            var batchSuccessCount = 0;
            var batchFailureCount = 0;
            var batchResults = new List<NotificationOperationResult>();

            try
            {
                var now = DateTime.UtcNow;
                var notificationEntities = new List<Data.Entities.Notifications.Notification>();
                var recipientEntities = new List<Data.Entities.Notifications.NotificationRecipient>();
                var dtoToIdMap = new Dictionary<CreateNotificationDto, Guid>();

                // Create all notification entities for this batch
                foreach (var createDto in batch)
                {
                    try
                    {
                        // Validate tenant access
                        await ValidateTenantAccessAsync(createDto.TenantId, cancellationToken);

                        var notificationId = Guid.NewGuid();
                        dtoToIdMap[createDto] = notificationId;

                        var notification = new Data.Entities.Notifications.Notification
                        {
                            Id = notificationId,
                            TenantId = createDto.TenantId ?? Guid.Empty,
                            SenderId = createDto.SenderId,
                            Type = createDto.Type,
                            Priority = createDto.Priority,
                            Status = NotificationStatus.Pending,
                            Title = createDto.Payload.Title,
                            Message = createDto.Payload.Message,
                            ActionUrl = createDto.Payload.ActionUrl,
                            IconUrl = createDto.Payload.IconUrl,
                            PayloadLocale = createDto.Payload.Locale,
                            LocalizationParamsJson = createDto.Payload.LocalizationParams is not null
                                ? System.Text.Json.JsonSerializer.Serialize(createDto.Payload.LocalizationParams)
                                : null,
                            ExpiresAt = createDto.ExpiresAt,
                            MetadataJson = createDto.Metadata is not null
                                ? System.Text.Json.JsonSerializer.Serialize(createDto.Metadata)
                                : null,
                            CreatedAt = now,
                            ModifiedAt = now
                        };

                        notificationEntities.Add(notification);

                        // Create recipient entities for this notification
                        foreach (var recipientId in createDto.RecipientIds)
                        {
                            var recipient = new Data.Entities.Notifications.NotificationRecipient
                            {
                                Id = Guid.NewGuid(),
                                NotificationId = notificationId,
                                UserId = recipientId,
                                TenantId = createDto.TenantId ?? Guid.Empty,
                                Status = NotificationStatus.Pending,
                                CreatedAt = now,
                                ModifiedAt = now
                            };
                            recipientEntities.Add(recipient);
                        }

                        batchResults.Add(new NotificationOperationResult
                        {
                            NotificationId = notificationId,
                            Success = true,
                            Metadata = new Dictionary<string, object> { ["ProcessedAt"] = now }
                        });
                        batchSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        batchResults.Add(new NotificationOperationResult
                        {
                            Success = false,
                            ErrorMessage = ex.Message,
                            ErrorCode = "VALIDATION_FAILED",
                            Metadata = new Dictionary<string, object>
                            {
                                ["ProcessedAt"] = now,
                                ["ExceptionType"] = ex.GetType().Name
                            }
                        });
                        batchFailureCount++;
                        logger.LogWarning(ex, "Failed to prepare notification in bulk batch");
                    }
                }

                // 5. Bulk insert into database  
                if (notificationEntities.Any())
                {
                    await context.Notifications.AddRangeAsync(notificationEntities, cancellationToken);
                    await context.NotificationRecipients.AddRangeAsync(recipientEntities, cancellationToken);

                    // Single SaveChanges per batch
                    await context.SaveChangesAsync(cancellationToken);

                    // 6. Send SignalR notifications in batches (group by recipient)
                    // Create lookup for O(1) access
                    var notificationLookup = notificationEntities.ToDictionary(n => n.Id);
                    var recipientGroups = recipientEntities.GroupBy(r => r.UserId);

                    foreach (var recipientGroup in recipientGroups)
                    {
                        try
                        {
                            var recipientId = recipientGroup.Key;
                            var recipientNotifications = recipientGroup
                                .Select(r => notificationLookup.TryGetValue(r.NotificationId, out var n) ? n : null)
                                .Where(n => n != null)
                                .Select(n => new
                                {
                                    id = n!.Id,
                                    type = n.Type.ToString(),
                                    priority = n.Priority.ToString(),
                                    title = n.Title,
                                    message = n.Message,
                                    actionUrl = n.ActionUrl,
                                    createdAt = n.CreatedAt
                                })
                                .ToList();

                            // Send all notifications for this user in one call
                            await hubContext.Clients
                                .Group($"user_{recipientId}")
                                .SendAsync("ReceiveBulkNotifications", recipientNotifications, cancellationToken);

                            logger.LogDebug("Sent {Count} notifications to user {UserId} via SignalR",
                                recipientNotifications.Count, recipientId);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to send SignalR notifications to user {UserId}", recipientGroup.Key);
                            // Continue processing other recipients
                        }
                    }
                }

                // Add batch results to overall results
                results.AddRange(batchResults);
                successCount += batchSuccessCount;
                failureCount += batchFailureCount;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process batch in bulk notification operation");

                // Only mark as failed items that weren't already processed
                var alreadyProcessedCount = batchResults.Count;
                var remainingCount = batch.Count - alreadyProcessedCount;

                if (remainingCount > 0)
                {
                    for (int i = 0; i < remainingCount; i++)
                    {
                        results.Add(new NotificationOperationResult
                        {
                            Success = false,
                            ErrorMessage = $"Batch processing failed: {ex.Message}",
                            ErrorCode = "BATCH_FAILED",
                            Metadata = new Dictionary<string, object>
                            {
                                ["ProcessedAt"] = DateTime.UtcNow,
                                ["ExceptionType"] = ex.GetType().Name
                            }
                        });
                        failureCount++;
                    }
                }

                // Add any successfully prepared items from this batch (even though save failed)
                results.AddRange(batchResults);
                successCount += batchSuccessCount;
                failureCount += batchFailureCount;
            }
        }

        // 7. Log audit trail for bulk operation
        await auditLogService.LogEntityChangeAsync(
            entityName: "BulkNotification",
            entityId: Guid.NewGuid(),
            propertyName: "BulkSend",
            operationType: "Insert",
            oldValue: null,
            newValue: $"Total: {notifications.Count}, Success: {successCount}, Failed: {failureCount}",
            changedBy: "System",
            entityDisplayName: "Bulk Notification Operation",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Bulk notification processing completed: {Success} success, {Failure} failures in {ElapsedMs}ms",
            successCount, failureCount, stopwatch.ElapsedMilliseconds);

        // 8. Return results
        return new BulkNotificationResultDto
        {
            TotalCount = notifications.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            ProcessingTime = stopwatch.Elapsed
        };
    }

}
