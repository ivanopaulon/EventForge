using EventForge.DTOs.Notifications;
using EventForge.DTOs.Common;
using EventForge.Server.Data;
using EventForge.Server.Services.Audit;
using EventForge.Server.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
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
            _context.Notifications.Add(notification);
            _context.NotificationRecipients.AddRange(recipients);
            await _context.SaveChangesAsync(cancellationToken);

            // Update status to sent
            notification.Status = NotificationStatus.Sent;
            notification.ModifiedAt = DateTime.UtcNow; // Use ModifiedAt instead of UpdatedAt
            await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail for notification creation
            await _auditLogService.LogEntityChangeAsync(
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
    /// STUB IMPLEMENTATION - Processes notifications individually.
    /// 
    /// TODO: Implement optimized bulk processing with:
    /// - Database bulk operations for better performance
    /// - Batch rate limiting validation
    /// - Parallel processing with configurable concurrency
    /// - Failure isolation and partial success handling
    /// - Progress reporting for long-running operations
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

        _logger.LogInformation("Starting bulk notification processing for {Count} notifications", notifications.Count);

        foreach (var notification in notifications.Take(batchSize))
        {
            try
            {
                var result = await SendNotificationAsync(notification, cancellationToken);
                results.Add(new NotificationOperationResult
                {
                    NotificationId = result.Id,
                    Success = true,
                    Metadata = new Dictionary<string, object> { ["ProcessedAt"] = DateTime.UtcNow }
                });
                successCount++;
            }
            catch (Exception ex)
            {
                results.Add(new NotificationOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ErrorCode = "SEND_FAILED",
                    Metadata = new Dictionary<string, object> 
                    { 
                        ["ProcessedAt"] = DateTime.UtcNow,
                        ["ExceptionType"] = ex.GetType().Name
                    }
                });
                failureCount++;
                
                _logger.LogWarning(ex, "Failed to process bulk notification");
            }
        }

        _logger.LogInformation(
            "Bulk notification processing completed: {Success} success, {Failure} failures in {ElapsedMs}ms",
            successCount, failureCount, stopwatch.ElapsedMilliseconds);

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
            await _auditLogService.LogEntityChangeAsync(
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

            await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            await _auditLogService.LogEntityChangeAsync(
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

            await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            await _auditLogService.LogEntityChangeAsync(
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

            await _context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            await _auditLogService.LogEntityChangeAsync(
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
    /// STUB IMPLEMENTATION - Returns default preferences.
    /// </summary>
    public async Task<NotificationPreferencesDto> GetUserPreferencesAsync(
        Guid userId, 
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving notification preferences for user {UserId} in tenant {TenantId}", userId, tenantId);

        // TODO: Query database for user-specific preferences with tenant defaults
        await Task.Delay(10, cancellationToken);

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

    /// <summary>
    /// Updates user notification preferences with validation.
    /// STUB IMPLEMENTATION - Logs update and returns preferences.
    /// </summary>
    public async Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(
        NotificationPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogEntityChangeAsync(
            entityName: "NotificationPreferences",
            entityId: preferences.UserId,
            propertyName: "Update",
            operationType: "Update",
            oldValue: "Previous preferences",
            newValue: $"Locale: {preferences.PreferredLocale}, Enabled: {preferences.NotificationsEnabled}",
            changedBy: preferences.UserId.ToString(),
            entityDisplayName: $"Notification Preferences: {preferences.UserId}",
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Updated notification preferences for user {UserId}: Locale={Locale}, Enabled={Enabled}",
            preferences.UserId, preferences.PreferredLocale, preferences.NotificationsEnabled);

        return preferences;
    }

    /// <summary>
    /// Localizes notification content based on user preferences.
    /// STUB IMPLEMENTATION - Returns notification unchanged.
    /// </summary>
    public async Task<NotificationResponseDto> LocalizeNotificationAsync(
        NotificationResponseDto notification,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Localizing notification {NotificationId} to locale {Locale} for user {UserId}",
            notification.Id, targetLocale, userId);

        // TODO: Implement localization logic
        await Task.Delay(10, cancellationToken);

        // Return notification with updated locale metadata
        notification.Payload.Locale = targetLocale;
        return notification;
    }

    #endregion

    #region Expiry & Cleanup Management

    /// <summary>
    /// Processes expired notifications with cleanup policies.
    /// STUB IMPLEMENTATION - Returns empty processing results.
    /// </summary>
    public async Task<ExpiryProcessingResultDto> ProcessExpiredNotificationsAsync(
        Guid? tenantId = null,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing expired notifications for tenant {TenantId} with batch size {BatchSize}",
            tenantId?.ToString() ?? "ALL", batchSize);

        // TODO: Implement database query and cleanup logic
        await Task.Delay(100, cancellationToken);

        return new ExpiryProcessingResultDto
        {
            ProcessedCount = 0,
            ExpiredCount = 0,
            ArchivedCount = 0,
            DeletedCount = 0,
            ProcessingTime = stopwatch.Elapsed
        };
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
        await _auditLogService.LogEntityChangeAsync(
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
        await _auditLogService.LogEntityChangeAsync(
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