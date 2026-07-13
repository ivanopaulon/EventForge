using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;


namespace EventForge.Server.Controllers;

public partial class ChatController
{

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
                messageId, userId, deleteDto?.Reason, deleteDto?.SoftDelete ?? true, tenantContext.CurrentTenantId, cancellationToken);
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

}
