using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{
    /// <summary>
    /// Retrieves notifications with advanced filtering and pagination.
    /// Implements database query with multi-tenant security and filtering.
    /// </summary>
    public async Task<PagedResult<NotificationResponseDto>> GetNotificationsAsync(
        NotificationSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {

        try
        {
            var query = context.NotificationRecipients
                .AsNoTracking()
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

            // Pre-fetch sender names
            var senderIds = items.Select(i => i.Notification.SenderId ?? Guid.Empty).ToList();
            var batchNames = await ResolveUserNamesAsync(senderIds, cancellationToken);

            // Map to DTOs with deserialization
            var notificationDtos = items.Select(item => new NotificationResponseDto
            {
                Id = item.Notification.Id,
                TenantId = item.Recipient.TenantId,
                SenderId = item.Notification.SenderId,
                SenderName = item.Notification.SenderId.HasValue
                    ? batchNames.GetValueOrDefault(item.Notification.SenderId.Value, "System")
                    : "System",
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
            logger.LogError(ex, "Failed to retrieve notifications for user {UserId}", searchDto.UserId);
            throw new InvalidOperationException("Failed to retrieve notifications", ex);
        }
    }

    /// <summary>
    /// Retrieves all notifications for the current user with pagination.
    /// NOTE: Requires user and tenant context to be determined from authentication.
    /// </summary>
    public async Task<PagedResult<NotificationResponseDto>> GetNotificationsAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        // NOTE: This is a simplified implementation that returns all notifications
        // In a full implementation, you would extract userId and tenantId from authentication context
        try
        {
            var query = context.Notifications
                .AsNoTracking()
                .Where(n => !n.IsDeleted)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var notificationDtos = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                TenantId = n.TenantId,
                SenderId = n.SenderId,
                SenderName = "System",
                Type = n.Type,
                Priority = n.Priority,
                Status = n.Status,
                Payload = new NotificationPayloadDto
                {
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    IconUrl = n.IconUrl
                },
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                AcknowledgedAt = n.AcknowledgedAt
            }).ToList();

            return new PagedResult<NotificationResponseDto>
            {
                Items = notificationDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notifications");
            throw new InvalidOperationException("Failed to retrieve notifications", ex);
        }
    }

    /// <summary>
    /// Retrieves unread notifications for the current user with pagination.
    /// </summary>
    public async Task<PagedResult<NotificationResponseDto>> GetUnreadNotificationsAsync(
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {

        try
        {
            var query = context.Notifications
                .AsNoTracking()
                .Where(n => !n.IsDeleted && !n.ReadAt.HasValue)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var notificationDtos = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                TenantId = n.TenantId,
                SenderId = n.SenderId,
                SenderName = "System",
                Type = n.Type,
                Priority = n.Priority,
                Status = n.Status,
                Payload = new NotificationPayloadDto
                {
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    IconUrl = n.IconUrl
                },
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                AcknowledgedAt = n.AcknowledgedAt
            }).ToList();

            return new PagedResult<NotificationResponseDto>
            {
                Items = notificationDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve unread notifications");
            throw new InvalidOperationException("Failed to retrieve unread notifications", ex);
        }
    }

    /// <summary>
    /// Retrieves notifications by type with pagination.
    /// </summary>
    public async Task<PagedResult<NotificationResponseDto>> GetNotificationsByTypeAsync(
        string type,
        PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {

        try
        {
            // Parse the type string to NotificationTypes enum
            if (!Enum.TryParse<NotificationTypes>(type, true, out var notificationType))
            {
                throw new ArgumentException($"Invalid notification type: {type}", nameof(type));
            }

            var query = context.Notifications
                .AsNoTracking()
                .Where(n => !n.IsDeleted && n.Type == notificationType)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var notificationDtos = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                TenantId = n.TenantId,
                SenderId = n.SenderId,
                SenderName = "System",
                Type = n.Type,
                Priority = n.Priority,
                Status = n.Status,
                Payload = new NotificationPayloadDto
                {
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    IconUrl = n.IconUrl
                },
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                AcknowledgedAt = n.AcknowledgedAt
            }).ToList();

            return new PagedResult<NotificationResponseDto>
            {
                Items = notificationDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notifications of type {Type}", type);
            throw new InvalidOperationException($"Failed to retrieve notifications of type {type}", ex);
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
        logger.LogInformation(
            "Retrieving notification {NotificationId} for user {UserId} in tenant {TenantId}",
            notificationId, userId, tenantId);

        try
        {
            var notificationRecipient = await context.NotificationRecipients
                .AsNoTracking()
                .Include(nr => nr.Notification)
                .Where(nr => nr.NotificationId == notificationId && nr.UserId == userId)
                .Where(nr => !tenantId.HasValue || nr.TenantId == tenantId.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationRecipient is null)
            {
                return null;
            }

            var notification = notificationRecipient.Notification;

            // Log audit trail for notification access
            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationRecipient",
                entityId: notificationId,
                propertyName: "Access",
                operationType: "Read",
                oldValue: null,
                newValue: "Viewed",
                changedBy: userId.ToString(),
                entityDisplayName: $"Notification Access: {notification.Title}",
                cancellationToken: cancellationToken);

            var senderName2 = await ResolveUserNameAsync(notification.SenderId, cancellationToken);
            return new NotificationResponseDto
            {
                Id = notification.Id,
                TenantId = notificationRecipient.TenantId,
                SenderId = notification.SenderId,
                SenderName = senderName2,
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
            logger.LogError(ex, "Failed to retrieve notification {NotificationId} for user {UserId}", notificationId, userId);
            throw new InvalidOperationException("Failed to retrieve notification", ex);
        }
    }

}
