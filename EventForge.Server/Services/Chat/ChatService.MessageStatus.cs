using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Updates message delivery status in the database with real-time notification.
    /// </summary>
    public async Task<MessageStatusUpdateResultDto> UpdateMessageStatusAsync(
        Guid messageId,
        MessageStatus status,
        Guid userId,
        Dictionary<string, object>? metadata = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await context.ChatMessages
                .Where(m => m.Id == messageId && (tenantId == null || m.TenantId == tenantId.Value) && !m.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Message {messageId} not found.");

            var oldStatus = message.Status;
            var now = DateTime.UtcNow;
            message.Status = status;
            message.ModifiedAt = now;
            message.ModifiedBy = userId.ToString();

            if (status == MessageStatus.Delivered) message.DeliveredAt ??= now;
            if (status == MessageStatus.Read) message.ReadAt ??= now;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "ChatMessage",
                entityId: messageId,
                propertyName: "Status",
                operationType: "Update",
                oldValue: oldStatus.ToString(),
                newValue: status.ToString(),
                changedBy: userId.ToString(),
                entityDisplayName: $"Message {messageId} Status",
                cancellationToken: cancellationToken);

            logger.LogInformation("ChatMessage {MessageId} status updated to {Status} by user {UserId}.", messageId, status, userId);

            await hubContext.Clients.Group($"chat_{message.ChatThreadId}")
                .SendAsync("MessageStatusUpdated", new { MessageId = messageId, Status = status, UpdatedBy = userId }, cancellationToken);

            return new MessageStatusUpdateResultDto
            {
                MessageId = messageId,
                Status = status,
                UserId = userId,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Marks a message as read by a user — upserts MessageReadReceipt in the database.
    /// </summary>
    public async Task<MessageReadReceiptDto> MarkMessageAsReadAsync(
        Guid messageId,
        Guid userId,
        DateTime? readAt = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var readTimestamp = readAt ?? DateTime.UtcNow;

            var existing = await context.MessageReadReceipts
                .Where(r => r.MessageId == messageId && r.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                var message = await context.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.Id == messageId)
                    .Select(m => new { m.TenantId })
                    .FirstOrDefaultAsync(cancellationToken);

                context.MessageReadReceipts.Add(new Data.Entities.Chat.MessageReadReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = readTimestamp,
                    TenantId = message?.TenantId ?? Guid.Empty,
                    CreatedAt = readTimestamp,
                    ModifiedAt = readTimestamp,
                    IsActive = true
                });
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            var user = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Username })
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogDebug("User {UserId} marked message {MessageId} as read.", userId, messageId);

            return new MessageReadReceiptDto
            {
                UserId = userId,
                Username = user?.Username ?? userId.ToString("N"),
                ReadAt = existing?.ReadAt ?? readTimestamp
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark message {MessageId} as read for user {UserId}.", messageId, userId);
            throw;
        }
    }

    /// <summary>
    /// Gets read receipts for a message from the database with user join.
    /// </summary>
    public async Task<List<MessageReadReceiptDto>> GetMessageReadReceiptsAsync(
        Guid messageId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var receipts = await context.MessageReadReceipts
                .AsNoTracking()
                .Where(r => r.MessageId == messageId)
                .Join(context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => new MessageReadReceiptDto
                    {
                        UserId = r.UserId,
                        Username = u.Username,
                        ReadAt = r.ReadAt
                    })
                .ToListAsync(cancellationToken);

            return receipts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get read receipts for message {MessageId}.", messageId);
            throw;
        }
    }

    /// <summary>
    /// Bulk marks multiple messages as read — single SaveChanges for efficiency.
    /// </summary>
    public async Task<BulkReadResultDto> BulkMarkAsReadAsync(
        List<Guid> messageIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("User {UserId} bulk marking {Count} messages as read.", userId, messageIds.Count);

            // Load messages needing receipts in one query
            var existingReceiptIds = await context.MessageReadReceipts
                .AsNoTracking()
                .Where(r => messageIds.Contains(r.MessageId) && r.UserId == userId)
                .Select(r => r.MessageId)
                .ToListAsync(cancellationToken);

            var missing = messageIds.Except(existingReceiptIds).ToList();

            if (missing.Count > 0)
            {
                var tenantMap = await context.ChatMessages
                    .AsNoTracking()
                    .Where(m => missing.Contains(m.Id))
                    .Select(m => new { m.Id, m.TenantId })
                    .ToDictionaryAsync(m => m.Id, m => m.TenantId, cancellationToken);

                var now = DateTime.UtcNow;
                var newReceipts = missing.Select(mid => new Data.Entities.Chat.MessageReadReceipt
                {
                    Id = Guid.NewGuid(),
                    MessageId = mid,
                    UserId = userId,
                    ReadAt = now,
                    TenantId = tenantMap.GetValueOrDefault(mid),
                    CreatedAt = now,
                    ModifiedAt = now,
                    IsActive = true
                }).ToList();

                context.MessageReadReceipts.AddRange(newReceipts);
                _ = await context.SaveChangesAsync(cancellationToken);
            }

            return new BulkReadResultDto
            {
                TotalCount = messageIds.Count,
                SuccessCount = messageIds.Count,
                FailureCount = 0,
                ProcessedMessageIds = messageIds,
                Errors = []
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to bulk mark messages as read for user {UserId}.", userId);
            throw;
        }
    }

}
