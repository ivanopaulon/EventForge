using EventForge.DTOs.Chat;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time chat functionality with multi-tenant support.
/// Handles intra-tenant chat (1:1, groups), file/media attachments, message status tracking,
/// SuperAdmin moderation, group creation, localization, and accessibility.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IChatService _chatService;

    public ChatHub(
        ILogger<ChatHub> logger,
        IChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
    }

    #region Connection Management

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically joins user to their active chat groups within their tenant.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (userId.HasValue && tenantId.HasValue)
        {
            // Join user-specific group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

            // Join tenant-wide group for tenant isolation
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId.Value}");

            // STUB: Load user's active chats and join those groups (deferred implementation)

            _logger.LogInformation("User {UserId} connected to chat hub for tenant {TenantId}", userId.Value, tenantId.Value);
        }
        else if (IsInRole("SuperAdmin"))
        {
            // SuperAdmin can monitor all tenants
            await Groups.AddToGroupAsync(Context.ConnectionId, "superadmin_chat_monitoring");
            _logger.LogInformation("SuperAdmin {UserId} connected to chat monitoring", GetCurrentUserId());
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Updates user's last seen status and removes from groups.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (userId.HasValue)
        {
            // STUB: Update user's last seen timestamp in active chats (deferred implementation)

            _logger.LogInformation("User {UserId} disconnected from chat hub", userId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Chat Management

    /// <summary>
    /// Creates a new chat (direct message or group).
    /// </summary>
    /// <param name="createChatDto">Chat creation parameters</param>
    public async Task CreateChat(CreateChatDto createChatDto)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
        {
            throw new HubException("User not authenticated or tenant not specified");
        }

        // Ensure chat is created within user's tenant
        if (createChatDto.TenantId != tenantId.Value)
        {
            throw new HubException("Cannot create chat in different tenant");
        }

        createChatDto.CreatedBy = userId.Value;

        try
        {
            // STUB: Chat service call not yet implemented
            var chatId = Guid.NewGuid();
            var chatResponse = new ChatResponseDto
            {
                Id = chatId,
                TenantId = createChatDto.TenantId,
                Type = createChatDto.Type,
                Name = createChatDto.Name,
                Description = createChatDto.Description,
                IsPrivate = createChatDto.IsPrivate,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.Value
            };

            // Add all participants to the chat group
            foreach (var participantId in createChatDto.ParticipantIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");

                // Notify participant about new chat
                await Clients.Group($"user_{participantId}").SendAsync("ChatCreated", chatResponse);
            }

            _logger.LogInformation("User {UserId} created {ChatType} chat {ChatId} with {ParticipantCount} participants",
                userId.Value, createChatDto.Type, chatId, createChatDto.ParticipantIds.Count);

            await Clients.Caller.SendAsync("ChatCreated", chatResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat for user {UserId}", userId.Value);
            throw new HubException("Failed to create chat");
        }
    }

    /// <summary>
    /// Joins an existing chat group.
    /// </summary>
    /// <param name="chatId">ID of the chat to join</param>
    public async Task JoinChat(Guid chatId)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // STUB: User access verification not yet implemented

            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");

            // Notify other chat members that user joined
            await Clients.Group($"chat_{chatId}").SendAsync("UserJoinedChat", new { ChatId = chatId, UserId = userId.Value });

            _logger.LogInformation("User {UserId} joined chat {ChatId}", userId.Value, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join chat {ChatId} for user {UserId}", chatId, userId.Value);
            throw new HubException("Failed to join chat");
        }
    }

    /// <summary>
    /// Leaves a chat group.
    /// </summary>
    /// <param name="chatId">ID of the chat to leave</param>
    public async Task LeaveChat(Guid chatId)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");

            // Notify other chat members that user left
            await Clients.Group($"chat_{chatId}").SendAsync("UserLeftChat", new { ChatId = chatId, UserId = userId.Value });

            _logger.LogInformation("User {UserId} left chat {ChatId}", userId.Value, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave chat {ChatId} for user {UserId}", chatId, userId.Value);
            throw new HubException("Failed to leave chat");
        }
    }

    #endregion

    #region Message Management

    /// <summary>
    /// Sends a message in a chat.
    /// </summary>
    /// <param name="messageDto">Message to send</param>
    public async Task SendMessage(SendMessageDto messageDto)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        messageDto.SenderId = userId.Value;

        try
        {
            // Call the chat service to save and send the message
            var savedMessage = await _chatService.SendMessageAsync(messageDto);

            // Send message to all chat participants
            await Clients.Group($"chat_{messageDto.ChatId}").SendAsync("MessageReceived", savedMessage);

            // Update chat's last activity
            await Clients.Group($"chat_{messageDto.ChatId}").SendAsync("ChatUpdated", new { ChatId = messageDto.ChatId, LastActivity = DateTime.UtcNow });

            _logger.LogInformation("User {UserId} sent message {MessageId} in chat {ChatId}",
                userId.Value, savedMessage.Id, messageDto.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message for user {UserId} in chat {ChatId}", userId.Value, messageDto.ChatId);
            throw new HubException("Failed to send message");
        }
    }

    /// <summary>
    /// Edits an existing message.
    /// </summary>
    /// <param name="editDto">Message edit parameters</param>
    public async Task EditMessage(EditMessageDto editDto)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        editDto.UserId = userId.Value;

        try
        {
            // STUB: Chat service call not yet implemented - notify chat participants of message edit
            await Clients.Group($"chat_{Guid.Empty}").SendAsync("MessageEdited", editDto);

            _logger.LogInformation("User {UserId} edited message {MessageId}", userId.Value, editDto.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit message {MessageId} for user {UserId}", editDto.MessageId, userId.Value);
            throw new HubException("Failed to edit message");
        }
    }

    /// <summary>
    /// Deletes a message (soft delete).
    /// </summary>
    /// <param name="messageId">ID of the message to delete</param>
    /// <param name="reason">Optional reason for deletion</param>
    public async Task DeleteMessage(Guid messageId, string? reason = null)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // STUB: Chat service call not yet implemented - notify chat participants of message deletion
            await Clients.Group($"chat_{Guid.Empty}").SendAsync("MessageDeleted", new { MessageId = messageId, DeletedBy = userId.Value, Reason = reason });

            _logger.LogInformation("User {UserId} deleted message {MessageId} with reason: {Reason}",
                userId.Value, messageId, reason ?? "No reason provided");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId} for user {UserId}", messageId, userId.Value);
            throw new HubException("Failed to delete message");
        }
    }

    /// <summary>
    /// Marks a message as read.
    /// </summary>
    /// <param name="messageId">ID of the message to mark as read</param>
    public async Task MarkMessageAsRead(Guid messageId)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // STUB: Chat service call not yet implemented
            var readReceipt = new MessageReadReceiptDto
            {
                UserId = userId.Value,
                ReadAt = DateTime.UtcNow
            };

            // Notify chat participants of read status (excluding the reader)
            await Clients.GroupExcept($"chat_{Guid.Empty}", Context.ConnectionId).SendAsync("MessageRead", new { MessageId = messageId, ReadReceipt = readReceipt });

            _logger.LogInformation("User {UserId} marked message {MessageId} as read", userId.Value, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark message {MessageId} as read for user {UserId}", messageId, userId.Value);
            throw new HubException("Failed to mark message as read");
        }
    }

    #endregion

    #region Typing Indicators

    /// <summary>
    /// Sends typing indicator to chat participants.
    /// </summary>
    /// <param name="chatId">ID of the chat where user is typing</param>
    /// <param name="isTyping">Whether user is currently typing</param>
    public async Task SendTypingIndicator(Guid chatId, bool isTyping)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var typingIndicator = new TypingIndicatorDto
            {
                ChatId = chatId,
                UserId = userId.Value,
                IsTyping = isTyping,
                Timestamp = DateTime.UtcNow
            };

            // Send typing indicator to other chat participants (excluding sender)
            await Clients.GroupExcept($"chat_{chatId}", Context.ConnectionId).SendAsync("TypingIndicator", typingIndicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing indicator for user {UserId} in chat {ChatId}", userId.Value, chatId);
            // Don't throw exception for typing indicators as they're not critical
        }
    }

    #endregion

    #region Chat Member Management

    /// <summary>
    /// Adds or removes members from a chat group.
    /// </summary>
    /// <param name="updateMembersDto">Member update parameters</param>
    public async Task UpdateChatMembers(UpdateChatMembersDto updateMembersDto)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        updateMembersDto.ActionBy = userId.Value;

        try
        {
            // STUB: User permission verification and chat service call not yet implemented

            // Add new members to the chat group
            if (updateMembersDto.UsersToAdd?.Any() == true)
            {
                foreach (var newUserId in updateMembersDto.UsersToAdd)
                {
                    // Notify new member about being added to chat
                    await Clients.Group($"user_{newUserId}").SendAsync("AddedToChat", new { ChatId = updateMembersDto.ChatId, AddedBy = userId.Value });
                }
            }

            // Remove members from the chat group
            if (updateMembersDto.UsersToRemove?.Any() == true)
            {
                foreach (var removedUserId in updateMembersDto.UsersToRemove)
                {
                    // Notify removed member
                    await Clients.Group($"user_{removedUserId}").SendAsync("RemovedFromChat", new { ChatId = updateMembersDto.ChatId, RemovedBy = userId.Value, Reason = updateMembersDto.Reason });
                }
            }

            // Notify all chat participants about member changes
            await Clients.Group($"chat_{updateMembersDto.ChatId}").SendAsync("ChatMembersUpdated", updateMembersDto);

            _logger.LogInformation("User {UserId} updated members for chat {ChatId}", userId.Value, updateMembersDto.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat members for user {UserId} in chat {ChatId}", userId.Value, updateMembersDto.ChatId);
            throw new HubException("Failed to update chat members");
        }
    }

    #endregion

    #region SuperAdmin Moderation

    /// <summary>
    /// Performs moderation actions on a chat (SuperAdmin only).
    /// </summary>
    /// <param name="moderationAction">Moderation action to perform</param>
    public async Task ModerateChat(ChatModerationActionDto moderationAction)
    {
        if (!IsInRole("SuperAdmin"))
        {
            throw new HubException("Access denied. SuperAdmin role required.");
        }

        moderationAction.ModeratorId = GetCurrentUserId() ?? Guid.Empty;

        try
        {
            // STUB: Chat service call not yet implemented

            // Notify affected users based on action type
            switch (moderationAction.Action.ToLower())
            {
                case "mute":
                case "disable":
                case "warn":
                    if (moderationAction.NotifyMembers)
                    {
                        await Clients.Group($"chat_{moderationAction.ChatId}").SendAsync("ChatModerated", moderationAction);
                    }
                    break;
                case "delete":
                    await Clients.Group($"chat_{moderationAction.ChatId}").SendAsync("ChatDeleted", moderationAction);
                    break;
            }

            _logger.LogInformation("SuperAdmin {UserId} performed moderation action '{Action}' on chat {ChatId} with reason: {Reason}",
                moderationAction.ModeratorId, moderationAction.Action, moderationAction.ChatId, moderationAction.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to moderate chat {ChatId}", moderationAction.ChatId);
            throw new HubException("Failed to perform moderation action");
        }
    }

    /// <summary>
    /// Gets chat statistics for monitoring (SuperAdmin only).
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to filter statistics</param>
    public async Task GetChatStats(Guid? tenantId = null)
    {
        if (!IsInRole("SuperAdmin") && !IsInRole("Admin"))
        {
            throw new HubException("Access denied. Admin role required.");
        }

        try
        {
            // STUB: Chat service call not yet implemented
            var stats = new ChatStatsDto
            {
                TenantId = tenantId,
                LastCalculated = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("ChatStatsReceived", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat stats");
            throw new HubException("Failed to retrieve chat statistics");
        }
    }

    #endregion

    #region Localization Support

    /// <summary>
    /// Updates the preferred locale for chat interactions.
    /// </summary>
    /// <param name="locale">Preferred locale (e.g., "en-US", "it-IT")</param>
    public async Task UpdateChatLocale(string locale)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // STUB: User preferences service call not yet implemented
            _logger.LogInformation("User {UserId} updated chat locale to {Locale}", userId.Value, locale);

            await Clients.Caller.SendAsync("ChatLocaleUpdated", locale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chat locale for user {UserId}", userId.Value);
            throw new HubException("Failed to update chat locale");
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