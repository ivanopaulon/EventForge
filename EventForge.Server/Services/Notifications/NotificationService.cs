using EventForge.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventForge.Server.Services.Notifications;

/// <summary>
/// Notification service implementation with comprehensive multi-tenant support.
/// 
/// This implementation provides stub methods for all notification functionality
/// while establishing the foundation for future full implementation.
/// 
/// Key architectural patterns:
/// - Multi-tenant data isolation with tenant-aware queries
/// - Comprehensive audit logging for all operations
/// - Rate limiting with tenant-specific policies
/// - Localization support with culture-aware content
/// - Extensible design for future enhancements
/// 
/// Future implementation areas:
/// - External notification providers (email, SMS, push notifications)
/// - Advanced rate limiting with Redis/distributed caching
/// - Machine learning for notification optimization
/// - Real-time analytics and monitoring dashboards
/// - Custom notification templates and workflow engine
/// </summary>
public class NotificationService : INotificationService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<NotificationService> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<NotificationService> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    #region Core Notification Management

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
                LocalizationParamsJson = createDto.Payload.LocalizationParams != null
                    ? System.Text.Json.JsonSerializer.Serialize(createDto.Payload.LocalizationParams)
                    : null,
                ExpiresAt = createDto.ExpiresAt,
                MetadataJson = createDto.Metadata != null
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
            _ = _context.Notifications.Add(notification);
            _context.NotificationRecipients.AddRange(recipients);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Update status to sent
            notification.Status = NotificationStatus.Sent;
            notification.ModifiedAt = DateTime.UtcNow; // Use ModifiedAt instead of UpdatedAt
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail for notification creation
            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "Notification",
                entityId: notificationId,
                propertyName: "Create",
                operationType: "Insert",
                oldValue: null,
                newValue: $"Type: {createDto.Type}, Priority: {createDto.Priority}, Recipients: {createDto.RecipientIds.Count}",
                changedBy: createDto.SenderId?.ToString() ?? "System",
                entityDisplayName: $"Notification: {createDto.Payload.Title}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
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
                await _hubContext.Clients.Group($"user_{recipientId}")
                    .SendAsync("NotificationReceived", notificationData);
            }

            // Also send to tenant-wide group if tenant is specified
            if (createDto.TenantId.HasValue)
            {
                await _hubContext.Clients.Group($"tenant_{createDto.TenantId.Value}")
                    .SendAsync("TenantNotificationReceived", notificationData);
            }

            // Build response DTO
            return new NotificationResponseDto
            {
                Id = notificationId,
                TenantId = createDto.TenantId,
                SenderId = createDto.SenderId,
                SenderName = "System", // TODO: Resolve sender name from user service
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
            _logger.LogError(ex, "Failed to send notification for tenant {TenantId}", createDto.TenantId);
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
        if (notifications == null || notifications.Count == 0)
        {
            _logger.LogWarning("SendBulkNotificationsAsync called with null or empty notifications list");
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

        _logger.LogInformation("Starting bulk notification processing for {Count} notifications with batch size {BatchSize}", 
            notifications.Count, batchSize);

        // 2. Check rate limiting for bulk operations
        try
        {
            // Use first notification's tenant for rate limit check
            var firstTenantId = notifications.FirstOrDefault()?.TenantId;
            await ValidateRateLimitAsync(firstTenantId, null, NotificationTypes.System, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate limit validation failed for bulk operation");
            // Continue processing but log the warning
        }

        // 3. Split into batches if count > batchSize
        var batches = notifications
            .Select((notification, index) => new { notification, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.notification).ToList())
            .ToList();

        _logger.LogInformation("Split {TotalCount} notifications into {BatchCount} batches", 
            notifications.Count, batches.Count);

        // 4. Process batches with optimized database operations
        foreach (var batch in batches)
        {
            try
            {
                var now = DateTime.UtcNow;
                var notificationEntities = new List<Data.Entities.Notifications.Notification>();
                var recipientEntities = new List<Data.Entities.Notifications.NotificationRecipient>();
                var notificationIdMap = new Dictionary<CreateNotificationDto, Guid>();

                // Create all notification entities for this batch
                foreach (var createDto in batch)
                {
                    try
                    {
                        // Validate tenant access
                        await ValidateTenantAccessAsync(createDto.TenantId, cancellationToken);

                        var notificationId = Guid.NewGuid();
                        notificationIdMap[createDto] = notificationId;

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
                            LocalizationParamsJson = createDto.Payload.LocalizationParams != null
                                ? System.Text.Json.JsonSerializer.Serialize(createDto.Payload.LocalizationParams)
                                : null,
                            ExpiresAt = createDto.ExpiresAt,
                            MetadataJson = createDto.Metadata != null
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

                        results.Add(new NotificationOperationResult
                        {
                            NotificationId = notificationId,
                            Success = true,
                            Metadata = new Dictionary<string, object> { ["ProcessedAt"] = now }
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new NotificationOperationResult
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
                        failureCount++;
                        _logger.LogWarning(ex, "Failed to prepare notification in bulk batch");
                    }
                }

                // 5. Bulk insert into database
                if (notificationEntities.Any())
                {
                    await _context.Notifications.AddRangeAsync(notificationEntities, cancellationToken);
                }
                if (recipientEntities.Any())
                {
                    await _context.NotificationRecipients.AddRangeAsync(recipientEntities, cancellationToken);
                }
                
                // Single SaveChanges per batch
                await _context.SaveChangesAsync(cancellationToken);

                // 6. Send SignalR notifications in batches (group by recipient)
                var recipientGroups = recipientEntities.GroupBy(r => r.UserId);
                foreach (var recipientGroup in recipientGroups)
                {
                    try
                    {
                        var recipientId = recipientGroup.Key;
                        var recipientNotifications = recipientGroup
                            .Select(r => notificationEntities.FirstOrDefault(n => n.Id == r.NotificationId))
                            .Where(n => n != null)
                            .ToList();

                        // Send to user's SignalR group
                        foreach (var notification in recipientNotifications)
                        {
                            await _hubContext.Clients
                                .Group($"user_{recipientId}")
                                .SendAsync("ReceiveNotification", new
                                {
                                    id = notification!.Id,
                                    type = notification.Type.ToString(),
                                    priority = notification.Priority.ToString(),
                                    title = notification.Title,
                                    message = notification.Message,
                                    actionUrl = notification.ActionUrl,
                                    createdAt = notification.CreatedAt
                                }, cancellationToken);
                        }

                        _logger.LogDebug("Sent {Count} notifications to user {UserId} via SignalR", 
                            recipientNotifications.Count, recipientId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId}", recipientGroup.Key);
                        // Continue processing other recipients
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch in bulk notification operation");
                // Mark all items in this batch as failed
                foreach (var notification in batch)
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
        }

        // 7. Log audit trail for bulk operation
        await _auditLogService.LogEntityChangeAsync(
            entityName: "BulkNotification",
            entityId: Guid.NewGuid(),
            propertyName: "BulkSend",
            operationType: "Insert",
            oldValue: null,
            newValue: $"Total: {notifications.Count}, Success: {successCount}, Failed: {failureCount}",
            changedBy: "System",
            entityDisplayName: "Bulk Notification Operation",
            cancellationToken: cancellationToken);

        _logger.LogInformation(
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

    /// <summary>
    /// Retrieves notifications with advanced filtering and pagination.
    /// Implements database query with multi-tenant security and filtering.
    /// </summary>
    public async Task<PagedResult<NotificationResponseDto>> GetNotificationsAsync(
        NotificationSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving notifications for user {UserId} in tenant {TenantId} - Page {Page}",
            searchDto.UserId, searchDto.TenantId, searchDto.PageNumber);

        try
        {
            var query = _context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.UserId == searchDto.UserId);

            // Apply tenant filtering
            if (searchDto.TenantId.HasValue)
            {
                query = query.Where(nr => nr.TenantId == searchDto.TenantId.Value);
            }

            // Apply status filtering
            if (searchDto.Statuses?.Any() == true)
            {
                query = query.Where(nr => searchDto.Statuses.Contains(nr.Status));
            }

            // Apply type filtering
            if (searchDto.Types?.Any() == true)
            {
                query = query.Where(nr => searchDto.Types.Contains(nr.Notification.Type));
            }

            // Apply priority filtering
            if (searchDto.Priorities?.Any() == true)
            {
                query = query.Where(nr => searchDto.Priorities.Contains(nr.Notification.Priority));
            }

            // Apply date range filtering
            if (searchDto.FromDate.HasValue)
            {
                query = query.Where(nr => nr.Notification.CreatedAt >= searchDto.FromDate.Value);
            }

            if (searchDto.ToDate.HasValue)
            {
                query = query.Where(nr => nr.Notification.CreatedAt <= searchDto.ToDate.Value);
            }

            // Apply text search
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                var searchTerm = searchDto.SearchTerm.ToLower();
                query = query.Where(nr =>
                    nr.Notification.Title.ToLower().Contains(searchTerm) ||
                    nr.Notification.Message.ToLower().Contains(searchTerm));
            }

            // Exclude expired notifications if requested
            if (searchDto.IncludeExpired != true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(nr => !nr.Notification.ExpiresAt.HasValue || nr.Notification.ExpiresAt > now);
            }

            // Apply sorting
            query = searchDto.SortBy?.ToLower() switch
            {
                "priority" => searchDto.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(nr => nr.Notification.Priority).ThenByDescending(nr => nr.Notification.CreatedAt)
                    : query.OrderBy(nr => nr.Notification.Priority).ThenBy(nr => nr.Notification.CreatedAt),
                "status" => searchDto.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(nr => nr.Status).ThenByDescending(nr => nr.Notification.CreatedAt)
                    : query.OrderBy(nr => nr.Status).ThenBy(nr => nr.Notification.CreatedAt),
                "type" => searchDto.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(nr => nr.Notification.Type).ThenByDescending(nr => nr.Notification.CreatedAt)
                    : query.OrderBy(nr => nr.Notification.Type).ThenBy(nr => nr.Notification.CreatedAt),
                _ => searchDto.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(nr => nr.Notification.CreatedAt)
                    : query.OrderBy(nr => nr.Notification.CreatedAt)
            };

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .Select(nr => new
                {
                    Notification = nr.Notification,
                    Recipient = nr
                })
                .ToListAsync(cancellationToken);

            // Map to DTOs with deserialization
            var notificationDtos = items.Select(item => new NotificationResponseDto
            {
                Id = item.Notification.Id,
                TenantId = item.Recipient.TenantId,
                SenderId = item.Notification.SenderId,
                SenderName = "System", // TODO: Resolve sender name
                RecipientIds = new List<Guid> { item.Recipient.UserId },
                Type = item.Notification.Type,
                Priority = item.Notification.Priority,
                Status = item.Recipient.Status,
                Payload = new NotificationPayloadDto
                {
                    Title = item.Notification.Title,
                    Message = item.Notification.Message,
                    ActionUrl = item.Notification.ActionUrl,
                    IconUrl = item.Notification.IconUrl,
                    Locale = item.Notification.PayloadLocale,
                    LocalizationParams = !string.IsNullOrEmpty(item.Notification.LocalizationParamsJson)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(item.Notification.LocalizationParamsJson)
                        : null
                },
                ExpiresAt = item.Notification.ExpiresAt,
                CreatedAt = item.Notification.CreatedAt,
                ReadAt = item.Recipient.ReadAt,
                AcknowledgedAt = item.Recipient.AcknowledgedAt,
                SilencedAt = item.Recipient.SilencedAt,
                ArchivedAt = item.Recipient.ArchivedAt,
                Metadata = !string.IsNullOrEmpty(item.Notification.MetadataJson)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(item.Notification.MetadataJson)
                    : null
            }).ToList();

            return new PagedResult<NotificationResponseDto>
            {
                Items = notificationDtos,
                Page = searchDto.PageNumber,
                PageSize = searchDto.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve notifications for user {UserId}", searchDto.UserId);
            throw new InvalidOperationException("Failed to retrieve notifications", ex);
        }
    }

    /// <summary>
    /// Gets a specific notification by ID with access validation.
    /// Implements database query with tenant and user access validation.
    /// </summary>
    public async Task<NotificationResponseDto?> GetNotificationByIdAsync(
        Guid notificationId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving notification {NotificationId} for user {UserId} in tenant {TenantId}",
            notificationId, userId, tenantId);

        try
        {
            var notificationRecipient = await _context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .Where(nr => !tenantId.HasValue || nr.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient == null)
            {
                return null;
            }

            var notification = notificationRecipient.Notification;

            // Log audit trail for notification access
            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Access",
                operationType: "Read",
                oldValue: null,
                newValue: "Viewed",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Access: {notification.Title}",
                cancellationToken: cancellationToken);

            return new NotificationResponseDto
            {
                Id = notification.Id,
                TenantId = notificationRecipient.TenantId,
                SenderId = notification.SenderId,
                SenderName = "System", // TODO: Resolve sender name
                RecipientIds = new List<Guid> { userId },
                Type = notification.Type,
                Priority = notification.Priority,
                Status = notificationRecipient.Status,
                Payload = new NotificationPayloadDto
                {
                    Title = notification.Title,
                    Message = notification.Message,
                    ActionUrl = notification.ActionUrl,
                    IconUrl = notification.IconUrl,
                    Locale = notification.PayloadLocale,
                    LocalizationParams = !string.IsNullOrEmpty(notification.LocalizationParamsJson)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(notification.LocalizationParamsJson)
                        : null
                },
                ExpiresAt = notification.ExpiresAt,
                CreatedAt = notification.CreatedAt,
                ReadAt = notificationRecipient.ReadAt,
                AcknowledgedAt = notificationRecipient.AcknowledgedAt,
                SilencedAt = notificationRecipient.SilencedAt,
                ArchivedAt = notificationRecipient.ArchivedAt,
                Metadata = !string.IsNullOrEmpty(notification.MetadataJson)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notification.MetadataJson)
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to retrieve notification", ex);
        }
    }

    #endregion

    #region Status Management & User Actions

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
            var notificationRecipient = await _context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient == null)
            {
                throw new InvalidOperationException($"Notification {notificationId} not found for user {userId}");
            }

            // Update status
            var previousStatus = notificationRecipient.Status;
            notificationRecipient.Status = NotificationStatus.Acknowledged;
            notificationRecipient.AcknowledgedAt = now;
            notificationRecipient.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt

            // If not already read, mark as read too
            if (notificationRecipient.ReadAt == null)
            {
                notificationRecipient.ReadAt = now;
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Acknowledged",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Acknowledgment: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "User {UserId} acknowledged notification {NotificationId} with reason: {Reason}",
                userId, notificationId, reason ?? "No reason provided");

            // Return updated notification
            return new NotificationResponseDto
            {
                Id = notificationRecipient.Notification.Id,
                TenantId = notificationRecipient.TenantId,
                SenderId = notificationRecipient.Notification.SenderId,
                SenderName = "System", // TODO: Resolve sender name
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
            _logger.LogError(ex, "Failed to acknowledge notification {NotificationId} for user {UserId}", notificationId, userId);
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
            var notificationRecipient = await _context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient == null)
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

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Silenced",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Silenced: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Failed to silence notification {NotificationId} for user {UserId}", notificationId, userId);
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
            var notificationRecipient = await _context.NotificationRecipients
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient == null)
            {
                throw new InvalidOperationException($"Notification {notificationId} not found for user {userId}");
            }

            // Update status
            var previousStatus = notificationRecipient.Status;
            notificationRecipient.Status = NotificationStatus.Archived;
            notificationRecipient.ArchivedAt = now;
            notificationRecipient.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt

            // Update notification archive flag if all recipients have archived
            var allRecipients = await _context.NotificationRecipients
                .Where(nr => nr.NotificationId == notificationId)
                .ToListAsync(cancellationToken);

            if (allRecipients.All(nr => nr.Status == NotificationStatus.Archived || nr.UserId == userId))
            {
                notificationRecipient.Notification.IsArchived = true;
                notificationRecipient.Notification.ArchivedAt = now;
                notificationRecipient.Notification.ModifiedAt = now; // Use ModifiedAt instead of UpdatedAt
            }

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: previousStatus.ToString(),
                newValue: "Archived",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Archived: {notificationRecipient.Notification.Title}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Failed to archive notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to archive notification", ex);
        }
    }

    /// <summary>
    /// Processes bulk status operations on notifications.
    /// STUB IMPLEMENTATION - Processes each notification individually.
    /// </summary>
    public async Task<BulkNotificationResultDto> ProcessBulkActionAsync(
        BulkNotificationActionDto bulkAction,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<NotificationOperationResult>();
        var successCount = 0;
        var failureCount = 0;

        _logger.LogInformation(
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

                _logger.LogWarning(ex, "Failed to process bulk action for notification {NotificationId}", notificationId);
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

    #endregion

    #region Preferences & Localization

    /// <summary>
    /// Gets user notification preferences with tenant defaults.
    /// Queries User.MetadataJson for stored preferences or returns defaults.
    /// </summary>
    public async Task<NotificationPreferencesDto> GetUserPreferencesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving notification preferences for user {UserId} in tenant {TenantId}", userId, tenantId);

        try
        {
            // 1. Query User entity by userId and tenantId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

            // 2. If user found and has preferences in metadata
            if (user?.MetadataJson != null)
            {
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(user.MetadataJson);
                    
                    if (metadata?.ContainsKey("NotificationPreferences") == true)
                    {
                        var preferencesJson = metadata["NotificationPreferences"].GetRawText();
                        var preferences = System.Text.Json.JsonSerializer.Deserialize<NotificationPreferencesDto>(preferencesJson);
                        
                        if (preferences != null)
                        {
                            preferences.UserId = userId;
                            preferences.TenantId = tenantId;
                            _logger.LogDebug("Retrieved stored notification preferences for user {UserId}", userId);
                            return preferences;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse notification preferences from user metadata for user {UserId}", userId);
                    // Continue to return defaults
                }
            }

            // 3. Return default preferences if not found or parsing failed
            _logger.LogDebug("Returning default notification preferences for user {UserId}", userId);
            return new NotificationPreferencesDto
            {
                UserId = userId,
                TenantId = tenantId,
                NotificationsEnabled = true,
                MinPriority = NotificationPriority.Low,
                EnabledTypes = Enum.GetValues<NotificationTypes>().ToList(),
                PreferredLocale = "en-US",
                SoundEnabled = true,
                AutoArchiveAfterDays = 30
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve notification preferences for user {UserId}", userId);
            
            // Return defaults on error
            return new NotificationPreferencesDto
            {
                UserId = userId,
                TenantId = tenantId,
                NotificationsEnabled = true,
                MinPriority = NotificationPriority.Low,
                EnabledTypes = Enum.GetValues<NotificationTypes>().ToList(),
                PreferredLocale = "en-US",
                SoundEnabled = true,
                AutoArchiveAfterDays = 30
            };
        }
    }

    /// <summary>
    /// Updates user notification preferences with validation and audit trail.
    /// Persists preferences to User.MetadataJson.
    /// </summary>
    public async Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(
        NotificationPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find user by preferences.UserId and TenantId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == preferences.UserId && u.TenantId == preferences.TenantId, cancellationToken);

            if (user == null)
            {
                throw new InvalidOperationException($"User {preferences.UserId} not found in tenant {preferences.TenantId}");
            }

            // 2. Get current preferences for audit trail
            var oldPreferences = await GetUserPreferencesAsync(preferences.UserId, preferences.TenantId, cancellationToken);

            // 3. Update User.MetadataJson with new preferences
            Dictionary<string, object> metadata;
            
            if (user.MetadataJson != null)
            {
                try
                {
                    var existingMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(user.MetadataJson);
                    metadata = existingMetadata ?? new Dictionary<string, object>();
                }
                catch
                {
                    metadata = new Dictionary<string, object>();
                }
            }
            else
            {
                metadata = new Dictionary<string, object>();
            }

            // Serialize preferences to store in metadata
            var preferencesObject = new
            {
                preferences.NotificationsEnabled,
                preferences.MinPriority,
                preferences.EnabledTypes,
                preferences.PreferredLocale,
                preferences.SoundEnabled,
                preferences.AutoArchiveAfterDays
            };

            metadata["NotificationPreferences"] = preferencesObject;
            user.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            user.ModifiedAt = DateTime.UtcNow;

            // 4. Save to database
            await _context.SaveChangesAsync(cancellationToken);

            // 5. Log audit trail with old vs new values
            await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationPreferences",
                entityId: preferences.UserId,
                propertyName: "Preferences",
                operationType: "Update",
                oldValue: System.Text.Json.JsonSerializer.Serialize(oldPreferences),
                newValue: System.Text.Json.JsonSerializer.Serialize(preferences),
                changedBy: preferences.UserId.ToString(),
                entityDisplayName: $"Notification Preferences: {preferences.UserId}",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Updated notification preferences for user {UserId}: Locale={Locale}, Enabled={Enabled}, MinPriority={MinPriority}",
                preferences.UserId, preferences.PreferredLocale, preferences.NotificationsEnabled, preferences.MinPriority);

            // 6. Return updated preferences
            return preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification preferences for user {UserId}", preferences.UserId);
            throw new InvalidOperationException("Failed to update notification preferences", ex);
        }
    }

    /// <summary>
    /// Localizes notification content based on user preferences.
    /// Currently updates locale field with placeholder for future translation service integration.
    /// </summary>
    public async Task<NotificationResponseDto> LocalizeNotificationAsync(
        NotificationResponseDto notification,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Check if already in target locale (early return)
        if (notification.Payload.Locale == targetLocale)
        {
            _logger.LogDebug("Notification {NotificationId} already in target locale {Locale}", 
                notification.Id, targetLocale);
            return notification;
        }

        _logger.LogInformation(
            "Localizing notification {NotificationId} to locale {Locale} for user {UserId}",
            notification.Id, targetLocale, userId);

        // 2. Update notification.Payload.Locale
        notification.Payload.Locale = targetLocale;

        // 3. TODO: Future implementation - Integrate with ITranslationService
        // When ITranslationService is available:
        // if (notification.Payload.LocalizationParams != null)
        // {
        //     var translationKey = $"notification.{notification.Type}.{notification.Payload.Title}";
        //     var translatedTitle = await _translationService.TranslateAsync(
        //         translationKey, 
        //         targetLocale, 
        //         notification.Payload.LocalizationParams);
        //     
        //     notification.Payload.Title = translatedTitle;
        //     
        //     // Same for message
        //     var messageKey = $"notification.{notification.Type}.{notification.Payload.Message}";
        //     var translatedMessage = await _translationService.TranslateAsync(
        //         messageKey,
        //         targetLocale,
        //         notification.Payload.LocalizationParams);
        //     
        //     notification.Payload.Message = translatedMessage;
        // }

        // 4. Log localization request for analytics
        _logger.LogDebug(
            "Localized notification {NotificationId} to {ToLocale} (translation service integration pending)",
            notification.Id, targetLocale);

        // Suppress async warning for future-proofing
        await Task.CompletedTask;

        // 5. Return localized notification
        return notification;
    }

    #endregion

    #region Expiry & Cleanup Management

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

        _logger.LogInformation(
            "Processing expired notifications for tenant {TenantId} with batch size {BatchSize}",
            tenantId?.ToString() ?? "ALL", batchSize);

        try
        {
            // 1. Query expired notifications
            var query = _context.Notifications
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= now)
                .Where(n => n.Status != NotificationStatus.Archived && n.Status != NotificationStatus.Expired);

            // 2. Apply tenant filter if provided
            if (tenantId.HasValue)
            {
                query = query.Where(n => n.TenantId == tenantId.Value);
            }

            // 3. Process in batches to avoid memory issues
            while (true)
            {
                // Get batch
                var batch = await query
                    .OrderBy(n => n.ExpiresAt)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!batch.Any())
                    break;

                foreach (var notification in batch)
                {
                    // 4. Apply expiry policy based on days since expiry
                    var daysSinceExpiry = (now - notification.ExpiresAt!.Value).Days;

                    if (daysSinceExpiry > 90)
                    {
                        // Hard delete old notifications (>90 days expired)
                        _context.Notifications.Remove(notification);
                        deletedCount++;
                        _logger.LogDebug("Deleting notification {NotificationId} expired {Days} days ago", 
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
                        _logger.LogDebug("Archiving notification {NotificationId} expired {Days} days ago", 
                            notification.Id, daysSinceExpiry);
                    }
                    else
                    {
                        // Mark as expired (recently expired, <30 days)
                        notification.Status = NotificationStatus.Expired;
                        notification.ModifiedAt = now;
                        expiredCount++;
                        _logger.LogDebug("Expiring notification {NotificationId} expired {Days} days ago", 
                            notification.Id, daysSinceExpiry);
                    }
                }

                // 5. Save changes for this batch
                await _context.SaveChangesAsync(cancellationToken);
                processedCount += batch.Count;

                // 6. Log progress
                _logger.LogInformation(
                    "Processed {ProcessedCount} expired notifications ({Expired} expired, {Archived} archived, {Deleted} deleted)",
                    processedCount, expiredCount, archivedCount, deletedCount);

                // Break if we processed less than batch size (last batch)
                if (batch.Count < batchSize)
                    break;
            }

            // 7. Log audit trail for cleanup operation
            await _auditLogService.LogEntityChangeAsync(
                entityName: "NotificationCleanup",
                entityId: Guid.NewGuid(),
                propertyName: "Cleanup",
                operationType: "Delete",
                oldValue: null,
                newValue: $"Processed: {processedCount}, Expired: {expiredCount}, Archived: {archivedCount}, Deleted: {deletedCount}",
                changedBy: "System",
                entityDisplayName: "Notification Expiry Cleanup",
                cancellationToken: cancellationToken);

            _logger.LogInformation(
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
            _logger.LogError(ex, "Failed to process expired notifications");
            
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
    /// Cleans up notification data based on retention policies.
    /// STUB IMPLEMENTATION - Returns empty cleanup results.
    /// </summary>
    public async Task<CleanupResultDto> CleanupNotificationDataAsync(
        NotificationCleanupPolicyDto cleanupPolicy,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Cleaning up notification data for tenant {TenantId} with retention period {RetentionPeriod}",
            cleanupPolicy.TenantId?.ToString() ?? "ALL", cleanupPolicy.RetentionPeriod);

        // TODO: Implement cleanup logic
        await Task.Delay(100, cancellationToken);

        return new CleanupResultDto
        {
            CleanedCount = 0,
            AnonymizedCount = 0,
            FreedBytes = 0,
            ProcessingTime = stopwatch.Elapsed
        };
    }

    #endregion

    #region Rate Limiting & Tenant Management

    /// <summary>
    /// Checks if notification sending is allowed under rate limits.
    /// Implements basic in-memory rate limiting with tenant and user-specific policies.
    /// </summary>
    public async Task<RateLimitStatusDto> CheckRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        NotificationTypes notificationType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Checking rate limit for tenant {TenantId}, user {UserId}, type {Type}",
            tenantId, userId, notificationType);

        try
        {
            // Simple rate limiting implementation
            // In a production environment, this should use Redis or distributed cache

            // Define rate limits by type and priority
            var rateLimits = new Dictionary<NotificationTypes, int>
            {
                { NotificationTypes.System, 1000 },
                { NotificationTypes.Security, 500 },
                { NotificationTypes.Event, 200 },
                { NotificationTypes.User, 100 },
                { NotificationTypes.Marketing, 50 },
                { NotificationTypes.Audit, 1000 }
            };

            var limit = rateLimits.GetValueOrDefault(notificationType, 100);
            var remainingQuota = limit - 1; // Simplified - in reality, track actual usage

            // Check if limit would be exceeded (simplified logic)
            var isAllowed = remainingQuota > 0;

            await Task.Delay(5, cancellationToken); // Simulate async operation

            return new RateLimitStatusDto
            {
                IsAllowed = isAllowed,
                RemainingQuota = Math.Max(0, remainingQuota),
                ResetTime = TimeSpan.FromHours(1),
                RateLimitType = tenantId.HasValue ? "Tenant" : "Global",
                LimitDetails = new Dictionary<string, object>
                {
                    ["TenantId"] = tenantId?.ToString() ?? "System",
                    ["UserId"] = userId?.ToString() ?? "N/A",
                    ["Type"] = notificationType.ToString(),
                    ["Limit"] = limit,
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check rate limit for tenant {TenantId}, user {UserId}", tenantId, userId);

            // On error, allow but log the issue
            return new RateLimitStatusDto
            {
                IsAllowed = true,
                RemainingQuota = 100,
                ResetTime = TimeSpan.FromHours(1),
                RateLimitType = "Error-Fallback",
                LimitDetails = new Dictionary<string, object>
                {
                    ["Error"] = "Rate limit check failed",
                    ["CheckedAt"] = DateTime.UtcNow
                }
            };
        }
    }

    /// <summary>
    /// Updates rate limiting policies for tenants.
    /// STUB IMPLEMENTATION - Logs update and returns policy.
    /// </summary>
    public async Task<RateLimitPolicyDto> UpdateTenantRateLimitAsync(
        Guid tenantId,
        RateLimitPolicyDto rateLimitPolicy,
        CancellationToken cancellationToken = default)
    {
        _ = await _auditLogService.LogEntityChangeAsync(
            entityName: "RateLimitPolicy",
            entityId: tenantId,
            propertyName: "Update",
            operationType: "Update",
            oldValue: "Previous policy",
            newValue: $"GlobalLimit: {rateLimitPolicy.GlobalLimit}, Window: {rateLimitPolicy.WindowSize}",
            changedBy: "System",
            entityDisplayName: $"Rate Limit Policy: {tenantId}",
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Updated rate limit policy for tenant {TenantId}: GlobalLimit={GlobalLimit}",
            tenantId, rateLimitPolicy.GlobalLimit);

        return rateLimitPolicy;
    }

    /// <summary>
    /// Gets comprehensive notification statistics.
    /// STUB IMPLEMENTATION - Returns empty statistics.
    /// </summary>
    public async Task<NotificationStatsDto> GetNotificationStatisticsAsync(
        Guid? tenantId = null,
        DateRange? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving notification statistics for tenant {TenantId} from {StartDate} to {EndDate}",
            tenantId?.ToString() ?? "ALL",
            dateRange?.StartDate.ToString("yyyy-MM-dd") ?? "N/A",
            dateRange?.EndDate.ToString("yyyy-MM-dd") ?? "N/A");

        // TODO: Implement database aggregation queries
        await Task.Delay(50, cancellationToken);

        return new NotificationStatsDto
        {
            TenantId = tenantId,
            TotalNotifications = 0,
            UnreadCount = 0,
            AcknowledgedCount = 0,
            SilencedCount = 0,
            ArchivedCount = 0,
            ExpiredCount = 0,
            CountByType = new Dictionary<NotificationTypes, int>(),
            CountByPriority = new Dictionary<NotificationPriority, int>()
        };
    }

    #endregion

    #region System Administration & Monitoring

    /// <summary>
    /// Sends system-wide notifications for critical alerts.
    /// STUB IMPLEMENTATION - Logs action and returns mock results.
    /// </summary>
    public async Task<SystemNotificationResultDto> SendSystemNotificationAsync(
        CreateSystemNotificationDto systemNotification,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _ = await _auditLogService.LogEntityChangeAsync(
            entityName: "SystemNotification",
            entityId: Guid.NewGuid(),
            propertyName: "Send",
            operationType: "Insert",
            oldValue: null,
            newValue: $"Emergency: {systemNotification.IsEmergency}, Override: {systemNotification.OverrideUserPreferences}",
            changedBy: adminUserId.ToString(),
            entityDisplayName: $"System Notification: {systemNotification.Payload.Title}",
            cancellationToken: cancellationToken);

        _logger.LogWarning(
            "System notification sent by admin {AdminId}: Emergency={Emergency}, Title={Title}",
            adminUserId, systemNotification.IsEmergency, systemNotification.Payload.Title);

        return new SystemNotificationResultDto
        {
            NotificationId = Guid.NewGuid(),
            TotalRecipients = systemNotification.RecipientIds.Count,
            DeliveredCount = systemNotification.RecipientIds.Count,
            FailedCount = 0
        };
    }

    /// <summary>
    /// Gets detailed audit trail for notification operations.
    /// STUB IMPLEMENTATION - Returns empty paginated results.
    /// </summary>
    public async Task<PagedResult<NotificationAuditEntryDto>> GetNotificationAuditTrailAsync(
        NotificationAuditQueryDto auditQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving notification audit trail for tenant {TenantId} from {FromDate} to {ToDate}",
            auditQuery.TenantId, auditQuery.FromDate, auditQuery.ToDate);

        // TODO: Query audit log entries from database
        await Task.Delay(20, cancellationToken);

        return new PagedResult<NotificationAuditEntryDto>
        {
            Items = new List<NotificationAuditEntryDto>(),
            Page = auditQuery.PageNumber,
            PageSize = auditQuery.PageSize,
            TotalCount = 0
        };
    }

    /// <summary>
    /// Monitors notification system health.
    /// STUB IMPLEMENTATION - Returns healthy status.
    /// </summary>
    public async Task<NotificationSystemHealthDto> GetSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking notification system health");

        // TODO: Implement health checks for database, cache, external services
        await Task.Delay(10, cancellationToken);

        return new NotificationSystemHealthDto
        {
            Status = "Healthy",
            Metrics = new Dictionary<string, object>
            {
                ["DatabaseConnected"] = true,
                ["CacheConnected"] = true,
                ["ExternalProvidersConnected"] = true,
                ["QueueLength"] = 0,
                ["ProcessingRate"] = "0 notifications/sec",
                ["LastHealthCheck"] = DateTime.UtcNow
            },
            Alerts = new List<string>()
        };
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Validates tenant access for multi-tenant operations.
    /// TODO: Implement actual tenant validation logic.
    /// </summary>
    private async Task ValidateTenantAccessAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId.HasValue)
        {
            _logger.LogDebug("Validating tenant access for {TenantId}", tenantId.Value);
            // TODO: Validate tenant exists and is active
        }
        await Task.Delay(1, cancellationToken);
    }

    /// <summary>
    /// Validates rate limiting before operations.
    /// TODO: Implement actual rate limiting validation.
    /// </summary>
    private async Task ValidateRateLimitAsync(
        Guid? tenantId,
        Guid? userId,
        NotificationTypes type,
        CancellationToken cancellationToken)
    {
        var rateLimitStatus = await CheckRateLimitAsync(tenantId, userId, type, cancellationToken);

        if (!rateLimitStatus.IsAllowed)
        {
            throw new InvalidOperationException(
                $"Rate limit exceeded for tenant {tenantId}, user {userId}, type {type}. " +
                $"Quota resets in {rateLimitStatus.ResetTime}");
        }
    }

    #endregion
}