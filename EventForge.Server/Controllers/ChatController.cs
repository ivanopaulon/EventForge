using Prym.DTOs.Chat;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for chat management and message history export.
/// Provides comprehensive endpoints for chat operations, message management,
/// file handling, and data export capabilities with multi-tenant support.
/// 
/// This controller implements stub endpoints for Step 3 requirements while
/// preparing for future full implementation with advanced features.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class ChatController(
    IChatService chatService,
    IMemoryCache memoryCache,
    ITenantContext tenantContext,
    ILogger<ChatController> logger) : BaseApiController
{

    #region Chat Management

    /// <summary>
    /// Creates a new chat thread (direct message or group chat).
    /// Supports automatic member addition, permission setup, and real-time notifications.
    /// </summary>
    /// <param name="createChatDto">Chat creation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created chat with member details and configuration</returns>
    /// <response code="201">Chat created successfully</response>
    /// <response code="400">Invalid chat parameters or validation errors</response>
    /// <response code="429">Rate limit exceeded for tenant or user</response>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ChatResponseDto>> CreateChatAsync(
        [FromBody] CreateChatDto createChatDto,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        // Derive identity fields from the authenticated server-side context;
        // both must be present — reject early if the HTTP context is incomplete.
        if (tenantContext.CurrentTenantId is not { } tenantId)
            return CreateValidationProblemDetails("Unable to resolve tenant from current context");
        if (tenantContext.CurrentUserId is not { } currentUserId)
            return CreateValidationProblemDetails("Unable to resolve user from current context");

        createChatDto.TenantId  = tenantId;
        createChatDto.CreatedBy = currentUserId;

        try
        {
            logger.LogInformation(
                "Creating {ChatType} chat with {ParticipantCount} participants for tenant {TenantId}",
                createChatDto.Type, createChatDto.ParticipantIds.Count, createChatDto.TenantId);

            var result = await chatService.CreateChatAsync(createChatDto, cancellationToken);

            return Created($"api/v1/chat/{result.Id}", result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return CreateConflictProblem("Rate limit exceeded: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the chat", ex);
        }
    }

    /// <summary>
    /// Gets detailed chat information including members and recent activity.
    /// Includes permission-based filtering and real-time status updates.
    /// </summary>
    /// <param name="id">Chat identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat details or 404 if not found/accessible</returns>
    /// <response code="200">Chat retrieved successfully</response>
    /// <response code="404">Chat not found or not accessible</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatResponseDto>> GetChatByIdAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;
            var tenantId = tenantContext.CurrentTenantId;

            var chat = await chatService.GetChatByIdAsync(id, userId, tenantId, cancellationToken);

            if (chat is null)
            {
                return CreateNotFoundProblem($"Chat with ID {id} was not found or is not accessible");
            }

            return Ok(chat);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the chat", ex);
        }
    }

    /// <summary>
    /// Searches and filters user's chats with advanced criteria and pagination.
    /// Supports full-text search, activity-based sorting, and smart categorization.
    /// </summary>
    /// <param name="searchDto">Search and filtering criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated chat results with activity metadata</returns>
    /// <response code="200">Chats retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ChatResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ChatResponseDto>>> SearchChatsAsync(
        [FromQuery] ChatSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        try
        {
            // Always scope the search to the authenticated user and their tenant
            searchDto.UserId = tenantContext.CurrentUserId;
            searchDto.TenantId = tenantContext.CurrentTenantId;

            logger.LogDebug(
                "Searching chats for user {UserId} in tenant {TenantId} - Page {Page}",
                searchDto.UserId, searchDto.TenantId, searchDto.PageNumber);

            var result = await chatService.SearchChatsAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while searching chats", ex);
        }
    }

    /// <summary>
    /// Returns all active users in the current tenant available for starting a chat.
    /// Includes real-time online status for each user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available users with online indicators</returns>
    /// <response code="200">Users retrieved successfully</response>
    [HttpGet("available-users")]
    [ProducesResponseType(typeof(List<ChatAvailableUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatAvailableUserDto>>> GetAvailableUsersAsync(
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        try
        {
            var tenantId = tenantContext.CurrentTenantId!.Value;
            var users = await chatService.GetAvailableUsersAsync(tenantId, cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get available users for chat");
            return CreateValidationProblemDetails("An error occurred while retrieving available users");
        }
    }

    /// <summary>
    /// Updates chat properties including name, description, and settings.
    /// Includes permission validation and member notification.
    /// </summary>
    /// <param name="id">Chat identifier</param>
    /// <param name="updateDto">Update parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated chat information</returns>
    /// <response code="200">Chat updated successfully</response>
    /// <response code="404">Chat not found or not accessible</response>
    /// <response code="403">Insufficient permissions to update chat</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatResponseDto>> UpdateChatAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateChatDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var result = await chatService.UpdateChatAsync(id, updateDto, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the chat", ex);
        }
    }

    /// <summary>
    /// Archives or deletes a chat with comprehensive cleanup.
    /// Supports soft deletion, data retention policies, and member notification.
    /// </summary>
    /// <param name="id">Chat identifier</param>
    /// <param name="deleteDto">Deletion parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion operation result</returns>
    /// <response code="200">Chat deleted successfully</response>
    /// <response code="404">Chat not found or not accessible</response>
    /// <response code="403">Insufficient permissions to delete chat</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ChatOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatOperationResultDto>> DeleteChatAsync(
        [FromRoute] Guid id,
        [FromBody] DeleteChatDto? deleteDto = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var result = await chatService.DeleteChatAsync(
                id, userId, deleteDto?.Reason, deleteDto?.SoftDelete ?? true, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the chat", ex);
        }
    }

    #endregion

    #region Message Management

    /// <summary>
    /// Sends a message in a chat with comprehensive validation and delivery tracking.
    /// Supports rich content, attachments, threading, and real-time delivery.
    /// </summary>
    /// <param name="messageDto">Message content and metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sent message with delivery status and metadata</returns>
    /// <response code="201">Message sent successfully</response>
    /// <response code="400">Invalid message parameters</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ChatMessageDto>> SendMessageAsync(
        [FromBody] SendMessageDto messageDto,
        CancellationToken cancellationToken = default)
    {
        if (tenantContext.CurrentUserId is { } currentUserId)
            messageDto.SenderId = currentUserId;

        try
        {
            logger.LogInformation(
                "Sending message in chat {ChatId} by user {UserId} with {AttachmentCount} attachments",
                messageDto.ChatId, messageDto.SenderId, messageDto.Attachments?.Count ?? 0);

            var result = await chatService.SendMessageAsync(messageDto, cancellationToken);

            return Created($"api/v1/chat/messages/{result.Id}", result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while sending the message", ex);
        }
    }

    /// <summary>
    /// Retrieves messages for a specific chat thread with pagination.
    /// This is the primary endpoint used by the chat UI when selecting a conversation.
    /// Validates that the requesting user is a member of the chat before returning messages.
    /// </summary>
    /// <param name="chatId">Chat thread identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of messages per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated message results for the chat</returns>
    /// <response code="200">Messages retrieved successfully</response>
    /// <response code="404">Chat not found or user is not a member</response>
    [HttpGet("{chatId:guid}/messages")]
    [ProducesResponseType(typeof(PagedResult<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetChatMessagesAsync(
        [FromRoute] Guid chatId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        try
        {
            if (tenantContext.CurrentUserId is not { } userId)
                return Unauthorized();

            var tenantId = tenantContext.CurrentTenantId;

            // Verify the user has access to this chat (must be a member within their tenant)
            var chat = await chatService.GetChatByIdAsync(chatId, userId, tenantId, cancellationToken);
            if (chat is null)
                return CreateNotFoundProblem($"Chat with ID {chatId} was not found or is not accessible");

            var searchDto = new MessageSearchDto
            {
                ChatId = chatId,
                TenantId = tenantId,
                PageNumber = page,
                PageSize = pageSize
            };

            var result = await chatService.GetMessagesAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving messages for chat {chatId}", ex);
        }
    }

    /// <summary>
    /// Retrieves chat messages with filtering, pagination, and permission validation.
    /// Supports thread navigation, search within conversations, and content filtering.
    /// </summary>
    /// <param name="searchDto">Message search and filtering criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated message results with context information</returns>
    /// <response code="200">Messages retrieved successfully</response>
    [HttpGet("messages")]
    [ProducesResponseType(typeof(PagedResult<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetMessagesAsync(
        [FromQuery] MessageSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug(
                "Retrieving messages for chat {ChatId} from {FromDate} to {ToDate} - Page {Page}",
                searchDto.ChatId, searchDto.FromDate, searchDto.ToDate, searchDto.PageNumber);

            var result = await chatService.GetMessagesAsync(searchDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving messages", ex);
        }
    }

    /// <summary>
    /// Retrieves all chat messages with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of chat messages</returns>
    /// <response code="200">Successfully retrieved messages with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("messages/all")]
    [ProducesResponseType(typeof(PagedResult<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetMessages(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.GetMessagesAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving messages.", ex);
        }
    }

    /// <summary>
    /// Retrieves messages for specific conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of messages for the conversation</returns>
    /// <response code="200">Successfully retrieved messages with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("messages/conversation/{conversationId}")]
    [ProducesResponseType(typeof(PagedResult<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetMessagesByConversation(
        Guid conversationId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.GetMessagesByConversationAsync(conversationId, pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving messages.", ex);
        }
    }

    /// <summary>
    /// Retrieves unread messages for current user
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of unread messages</returns>
    /// <response code="200">Successfully retrieved unread messages with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("messages/unread")]
    [ProducesResponseType(typeof(PagedResult<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetUnreadMessages(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.GetUnreadMessagesAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving unread messages.", ex);
        }
    }

    /// <summary>
    /// Gets a specific message by ID with access validation and context.
    /// Includes thread context, attachments, and read receipt information.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message details or 404 if not found/accessible</returns>
    /// <response code="200">Message retrieved successfully</response>
    /// <response code="404">Message not found or not accessible</response>
    [HttpGet("messages/{messageId:guid}")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessageDto>> GetMessageByIdAsync(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;
            var tenantId = tenantContext.CurrentTenantId;

            var message = await chatService.GetMessageByIdAsync(messageId, userId, tenantId, cancellationToken);

            if (message is null)
            {
                return CreateNotFoundProblem($"Message with ID {messageId} was not found or is not accessible");
            }

            return Ok(message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the message", ex);
        }
    }

    /// <summary>
    /// Edits an existing message with validation and change tracking.
    /// Supports edit history, permission validation, and member notification.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="editDto">Message edit parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated message with edit metadata</returns>
    /// <response code="200">Message edited successfully</response>
    /// <response code="404">Message not found or not accessible</response>
    /// <response code="403">Insufficient permissions to edit message</response>
    [HttpPut("messages/{messageId:guid}")]
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ChatMessageDto>> EditMessageAsync(
        [FromRoute] Guid messageId,
        [FromBody] EditMessageRequestDto editDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var editMessageDto = new EditMessageDto
            {
                MessageId = messageId,
                UserId = userId,
                Content = editDto.Content,
                EditReason = editDto.EditReason
            };

            var result = await chatService.EditMessageAsync(editMessageDto, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while editing the message", ex);
        }
    }

    /// <summary>
    /// Deletes a message with soft/hard delete options.
    /// Supports cascade deletion of attachments and notification cleanup.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="deleteDto">Deletion parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion operation result</returns>
    /// <response code="200">Message deleted successfully</response>
    /// <response code="404">Message not found or not accessible</response>
    /// <response code="403">Insufficient permissions to delete message</response>
    [HttpDelete("messages/{messageId:guid}")]
    [ProducesResponseType(typeof(MessageOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MessageOperationResultDto>> DeleteMessageAsync(
        [FromRoute] Guid messageId,
        [FromBody] DeleteMessageDto? deleteDto = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var result = await chatService.DeleteMessageAsync(
                messageId, userId, deleteDto?.Reason, deleteDto?.SoftDelete ?? true, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the message", ex);
        }
    }

    /// <summary>
    /// Marks a message as read by the current user.
    /// Updates read receipts and triggers delivery confirmations.
    /// </summary>
    /// <param name="messageId">Message identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated read receipt information</returns>
    /// <response code="200">Message marked as read successfully</response>
    /// <response code="404">Message not found or not accessible</response>
    [HttpPost("messages/{messageId:guid}/read")]
    [ProducesResponseType(typeof(MessageReadReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageReadReceiptDto>> MarkMessageAsReadAsync(
        [FromRoute] Guid messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var result = await chatService.MarkMessageAsReadAsync(messageId, userId, null, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateNotFoundProblem(ex.Message
            );
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while marking the message as read", ex);
        }
    }

    /// <summary>
    /// Marks all unread messages in a chat as read for the current user.
    /// </summary>
    /// <param name="chatId">Chat identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk read operation results</returns>
    /// <response code="200">All messages marked as read</response>
    /// <response code="404">Chat not found</response>
    [HttpPost("{chatId:guid}/messages/read-all")]
    [ProducesResponseType(typeof(BulkReadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkReadResultDto>> MarkAllMessagesAsReadAsync(
        [FromRoute] Guid chatId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;
            var allMessages = await chatService.GetMessagesByConversationAsync(
                chatId,
                new PaginationParameters { Page = 1, PageSize = 1000 },
                cancellationToken);

            var messageIds = allMessages.Items.Select(m => m.Id).ToList();
            if (messageIds.Count == 0)
                return Ok(new BulkReadResultDto { TotalCount = 0, SuccessCount = 0, FailureCount = 0, ProcessedMessageIds = [], Errors = [] });

            var result = await chatService.BulkMarkAsReadAsync(messageIds, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while marking all messages as read", ex);
        }
    }

    /// <summary>
    /// Bulk marks multiple messages as read for efficient batch processing.
    /// Supports conversation-level read status updates with optimization.
    /// </summary>
    /// <param name="messageIds">List of message IDs to mark as read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk read operation results</returns>
    /// <response code="200">Bulk read operation completed</response>
    /// <response code="400">Invalid message IDs or request parameters</response>
    [HttpPost("messages/bulk-read")]
    [ProducesResponseType(typeof(BulkReadResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkReadResultDto>> BulkMarkAsReadAsync(
        [FromBody] List<Guid> messageIds,
        CancellationToken cancellationToken = default)
    {
        if (messageIds is null || !messageIds.Any())
        {
            return CreateValidationProblemDetails("Message IDs list cannot be empty"
            );
        }

        if (messageIds.Count > 100)
        {
            return CreateValidationProblemDetails("Maximum 100 messages allowed per bulk read operation"
            );
        }

        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var result = await chatService.BulkMarkAsReadAsync(messageIds, userId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while marking messages as read", ex);
        }
    }

    #endregion

    #region File & Media Management

    /// <summary>
    /// Uploads a file attachment to a chat with comprehensive validation.
    /// Supports multiple file types, virus scanning, and thumbnail generation.
    /// </summary>
    /// <param name="uploadRequest">File upload request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file metadata and access information</returns>
    /// <response code="201">File uploaded successfully</response>
    /// <response code="400">Invalid file or request parameters</response>
    /// <response code="413">File too large</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResultDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FileUploadResultDto>> UploadFileAsync(
        [FromForm] ChatFileUploadRequestDto uploadRequest,
        CancellationToken cancellationToken = default
    )
    {
        if (uploadRequest.File is null || uploadRequest.File.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty"
            );
        }

        // Check file size limit (example: 50MB)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (uploadRequest.File.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var uploadDto = new FileUploadDto
            {
                ChatId = Guid.Parse(uploadRequest.ChatId),
                UploadedBy = userId,
                FileName = uploadRequest.File.FileName,
                ContentType = uploadRequest.File.ContentType,
                FileSize = uploadRequest.File.Length,
                FileStream = uploadRequest.File.OpenReadStream()
            };

            var result = await chatService.UploadFileAsync(uploadDto, cancellationToken);

            if (result.Success)
            {
                return Created($"api/v1/chat/files/{result.AttachmentId}/download", result);
            }
            else
            {
                return CreateValidationProblemDetails(result.ErrorMessage ?? "An error occurred during file upload"
                );
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the file", ex);
        }
    }

    /// <summary>
    /// Uploads an image inserted via the RichTextEditor to prevent base64 inline data URIs
    /// from bloating the message payload. Returns a URL the RTE can use as the image src.
    /// Only image content types are accepted (jpeg, png, gif, webp).
    /// </summary>
    /// <param name="file">Image file posted by the Syncfusion RTE upload handler.</param>
    /// <param name="env">Web host environment (injected from DI).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON with the public URL under which the uploaded image is accessible.</returns>
    /// <response code="200">Image uploaded and URL returned.</response>
    /// <response code="400">File missing, not an image, or exceeds the size limit.</response>
    [HttpPost("upload-image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadRteImageAsync(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return CreateValidationProblemDetails("Image file cannot be empty.");

        const long maxImageSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxImageSize)
            return CreateValidationProblemDetails("Image must not exceed 5 MB.");

        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        if (!allowedMimeTypes.Contains(file.ContentType))
            return CreateValidationProblemDetails($"Unsupported image type '{file.ContentType}'. Allowed: jpeg, png, gif, webp.");

        try
        {
            var uploadId = Guid.NewGuid();
            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = "bin";

            var year  = DateTime.UtcNow.Year.ToString();
            var month = DateTime.UtcNow.Month.ToString("D2");

            var fullPath = Path.Combine(env.ContentRootPath, "Uploads", "chat-images",
                year, month, $"{uploadId}.{extension}");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            // Embed year/month in URL so the GET endpoint can reconstruct the exact path
            // without scanning the directory tree on every request.
            var fileUrl = $"{baseUrl}/api/v1/chat/image-uploads/{year}/{month}/{uploadId}.{extension}";

            return Ok(new { url = fileUrl });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the image.", ex);
        }
    }

    /// <summary>
    /// Serves a previously uploaded RTE inline image.
    /// </summary>
    [HttpGet("image-uploads/{year:int}/{month:int}/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetRteImage(
        [FromRoute] int year,
        [FromRoute] int month,
        [FromRoute] string fileName,
        [FromServices] IWebHostEnvironment env)
    {
        // Validate year and month ranges to prevent path traversal via route segments.
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest();

        // fileName must match exactly the GUID.extension pattern generated by the upload endpoint.
        if (string.IsNullOrEmpty(fileName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                fileName,
                @"^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\.[a-z]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return BadRequest();
        }

        var fullPath = Path.Combine(env.ContentRootPath, "Uploads", "chat-images",
            year.ToString(), month.ToString("D2"), fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var mimeType = ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png"           => "image/png",
            "gif"           => "image/gif",
            "webp"          => "image/webp",
            _               => "application/octet-stream"
        };
        return PhysicalFile(fullPath, mimeType);
    }


    /// Provides time-limited URLs, access logging, and download tracking.
    /// </summary>
    /// <param name="attachmentId">File attachment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secure download information or 404 if not accessible</returns>
    /// <response code="200">Download information retrieved successfully</response>
    /// <response code="404">File not found or not accessible</response>
    [HttpGet("files/{attachmentId:guid}/info")]
    [ProducesResponseType(typeof(FileDownloadInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDownloadInfoDto>> GetFileDownloadInfoAsync(
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;
            var tenantId = tenantContext.CurrentTenantId;

            var downloadInfo = await chatService.GetFileDownloadInfoAsync(attachmentId, userId, tenantId, cancellationToken);

            if (downloadInfo is null)
            {
                return CreateNotFoundProblem($"File with ID {attachmentId} was not found or is not accessible");
            }

            return Ok(downloadInfo);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving file information", ex);
        }
    }

    /// <summary>
    /// Downloads a file attachment with secure access validation.
    /// </summary>
    /// <param name="attachmentId">File attachment identifier</param>
    /// <param name="token">Security token for download access (reserved for future use)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content stream</returns>
    /// <response code="200">File downloaded successfully</response>
    /// <response code="404">File not found or token invalid</response>
    [HttpGet("files/{attachmentId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFileAsync(
        [FromRoute] Guid attachmentId,
        [FromQuery] string? token = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Downloading file {AttachmentId}", attachmentId);

            var result = await chatService.GetAttachmentForDownloadAsync(attachmentId, cancellationToken);
            if (result is null)
                return CreateNotFoundProblem($"File with ID {attachmentId} was not found or has been deleted.");

            var (physicalPath, contentType, fileName) = result.Value;
            return PhysicalFile(physicalPath, contentType, fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while downloading the file", ex);
        }
    }

    #endregion

    #region Chat Statistics & Analytics

    /// <summary>
    /// Gets comprehensive chat statistics and analytics.
    /// Supports real-time metrics, historical analysis, and tenant-specific insights.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter for statistics</param>
    /// <param name="fromDate">Optional start date for statistics range</param>
    /// <param name="toDate">Optional end date for statistics range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive chat analytics and metrics</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ChatStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatStatsDto>> GetChatStatisticsAsync(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dateRange = fromDate.HasValue && toDate.HasValue
                ? new DateRange { StartDate = fromDate.Value, EndDate = toDate.Value }
                : null;

            var result = await chatService.GetChatStatisticsAsync(tenantId, dateRange, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving statistics", ex);
        }
    }

    #endregion

    #region Data Export & History

    private record ChatExportCacheEntry(byte[] Bytes, string Format, int RecordCount, DateTime CreatedAt);
    private static string ChatExportCacheKey(Guid exportId) => $"chat_export_{exportId}";

    /// <summary>
    /// Exports chat history and messages in JSON or CSV format.
    /// Queries the DB synchronously (up to MaxRecords), caches the result for 24h.
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(ChatExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatExportResultDto>> ExportChatHistoryAsync(
        [FromBody] ChatExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Starting chat export for tenant {TenantId} from {FromDate} to {ToDate} in {Format} format",
                exportRequest.TenantId, exportRequest.FromDate, exportRequest.ToDate, exportRequest.Format);

            var maxRecords = Math.Min(exportRequest.MaxRecords ?? 10_000, 100_000);
            var search = new MessageSearchDto
            {
                ChatId = exportRequest.ChatId,
                TenantId = exportRequest.TenantId,
                SenderId = exportRequest.UserId,
                FromDate = exportRequest.FromDate,
                ToDate = exportRequest.ToDate,
                SearchTerm = exportRequest.SearchTerm,
                IncludeDeleted = exportRequest.IncludeDeleted,
                PageNumber = 1,
                PageSize = maxRecords
            };

            var paged = await chatService.GetMessagesAsync(search, cancellationToken);
            var messages = paged.Items.ToList();

            var exportId = Guid.NewGuid();
            byte[] bytes;
            string format = exportRequest.Format.ToUpperInvariant();

            if (format == "CSV")
                bytes = BuildChatCsvExport(messages);
            else
            {
                bytes = BuildChatJsonExport(exportId, messages);
                format = "JSON";
            }

            memoryCache.Set(
                ChatExportCacheKey(exportId),
                new ChatExportCacheEntry(bytes, format, messages.Count, DateTime.UtcNow),
                new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                    Size = 1
                });

            var result = new ChatExportResultDto
            {
                ExportId = exportId,
                Status = "Completed",
                Format = format,
                ProgressPercentage = 100,
                RecordCount = messages.Count,
                FileSizeBytes = bytes.Length,
                StatusUrl = Url.Action(nameof(GetChatExportStatusAsync), new { exportId }),
                DownloadUrl = Url.Action(nameof(DownloadChatExportAsync), new { exportId }),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            logger.LogInformation("Chat export {ExportId} completed: {RecordCount} records, {Format}, {Bytes} bytes",
                exportId, messages.Count, format, bytes.Length);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while generating the chat export", ex);
        }
    }

    /// <summary>
    /// Gets the status of a chat export operation (cached for 24h).
    /// </summary>
    [HttpGet("export/{exportId:guid}/status")]
    [ProducesResponseType(typeof(ChatExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<ChatExportResultDto> GetChatExportStatusAsync([FromRoute] Guid exportId)
    {
        if (!memoryCache.TryGetValue(ChatExportCacheKey(exportId), out ChatExportCacheEntry? entry) || entry is null)
            return CreateNotFoundProblem("Export not found or has expired.");

        var result = new ChatExportResultDto
        {
            ExportId = exportId,
            Status = "Completed",
            Format = entry.Format,
            ProgressPercentage = 100,
            RecordCount = entry.RecordCount,
            FileSizeBytes = entry.Bytes.Length,
            DownloadUrl = Url.Action(nameof(DownloadChatExportAsync), new { exportId }),
            ExpiresAt = entry.CreatedAt.AddHours(24),
            CompletedAt = entry.CreatedAt,
            CreatedAt = entry.CreatedAt
        };
        return Ok(result);
    }

    /// <summary>
    /// Downloads a previously generated chat export file (cached for 24h).
    /// </summary>
    [HttpGet("export/{exportId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult DownloadChatExportAsync([FromRoute] Guid exportId)
    {
        if (!memoryCache.TryGetValue(ChatExportCacheKey(exportId), out ChatExportCacheEntry? entry) || entry is null)
            return CreateNotFoundProblem("Export not found or has expired.");

        var (contentType, fileExt) = entry.Format == "CSV"
            ? ("text/csv", "csv")
            : ("application/json", "json");

        logger.LogInformation("Serving chat export file for {ExportId} ({Format}, {Bytes} bytes)", exportId, entry.Format, entry.Bytes.Length);
        return File(entry.Bytes, contentType, $"chat-export-{exportId}.{fileExt}");
    }

    private static byte[] BuildChatJsonExport(Guid exportId, IReadOnlyList<ChatMessageDto> messages)
    {
        var payload = new
        {
            exportId,
            generatedAt = DateTime.UtcNow,
            format = "JSON",
            recordCount = messages.Count,
            messages
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private static byte[] BuildChatCsvExport(IReadOnlyList<ChatMessageDto> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ChatId,SenderId,SenderName,Content,SentAt,Status,IsEdited,IsDeleted");
        foreach (var m in messages)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(m.Id.ToString()),
                CsvEscape(m.ChatId.ToString()),
                CsvEscape(m.SenderId.ToString()),
                CsvEscape(m.SenderName),
                CsvEscape(m.Content),
                CsvEscape(m.SentAt.ToString("O")),
                CsvEscape(m.Status.ToString()),
                m.IsEdited.ToString().ToLowerInvariant(),
                m.IsDeleted.ToString().ToLowerInvariant()));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    #endregion

    #region System Health & Monitoring

    /// <summary>
    /// Gets chat system health status and metrics.
    /// Provides real-time system monitoring and alerting information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat system health status and metrics</returns>
    /// <response code="200">System health retrieved successfully</response>
    [HttpGet("system/health")]
    [ProducesResponseType(typeof(ChatSystemHealthDto), StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,SuperAdmin")] // Restrict to administrators
    public async Task<ActionResult<ChatSystemHealthDto>> GetChatSystemHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await chatService.GetChatSystemHealthAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving system health", ex);
        }
    }

    #endregion

    /// <summary>
    /// Automatically finds and merges all duplicate DirectMessage chats for the current user.
    /// When two DM threads exist between the same pair of users, their messages are consolidated
    /// into the most-recently-updated thread and the duplicate is soft-deleted.
    /// </summary>
    /// <returns>Merge summary: how many duplicate threads were removed and messages re-parented.</returns>
    /// <response code="200">Merge completed (zero or more threads merged).</response>
    [HttpPost("merge-duplicates")]
    [ProducesResponseType(typeof(DmMergeResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DmMergeResultDto>> MergeDirectMessageDuplicatesAsync(
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantValidation) return tenantValidation;

        if (tenantContext.CurrentUserId is not { } userId)
            return CreateValidationProblemDetails("Unable to resolve user from current context");

        var tenantId = tenantContext.CurrentTenantId;

        try
        {
            var result = await chatService.MergeDirectMessageDuplicatesAsync(userId, tenantId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while merging duplicate chats", ex);
        }
    }

    /// <summary>
    /// Reports (flags) a chat message as inappropriate.
    /// The flag is idempotent: a message already flagged is returned as successful without modification.
    /// </summary>
    /// <param name="messageId">The ID of the message to report.</param>
    /// <param name="dto">Optional reason for reporting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation result indicating success.</returns>
    /// <response code="200">Message flagged successfully (or already flagged).</response>
    /// <response code="404">Message not found.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost("messages/{messageId:guid}/report")]
    [ProducesResponseType(typeof(MessageOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageOperationResultDto>> ReportMessageAsync(
        [FromRoute] Guid messageId,
        [FromBody] ReportMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var reporterUserId = tenantContext.CurrentUserId?.ToString() ?? "unknown";

        try
        {
            var result = await chatService.ReportMessageAsync(messageId, dto, reporterUserId, cancellationToken);

            if (!result.Success)
                return NotFound(new ProblemDetails { Title = result.ErrorMessage ?? "Message not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while reporting the message", ex);
        }
    }
}

/// <summary>
/// DTO for file upload request parameters.
/// Provides Swagger-compatible structure for chat file uploads.
/// </summary>
public class ChatFileUploadRequestDto
{
    /// <summary>
    /// File to upload to the chat.
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Chat identifier where the file will be uploaded.
    /// </summary>
    [Required]
    [MaxLength(36)] // GUID string length
    public string ChatId { get; set; } = string.Empty;
}