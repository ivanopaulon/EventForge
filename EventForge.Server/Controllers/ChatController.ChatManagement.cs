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

        createChatDto.TenantId = tenantId;
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

            var result = await chatService.UpdateChatAsync(id, updateDto, userId, tenantContext.CurrentTenantId, cancellationToken);
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
                id, userId, deleteDto?.Reason, deleteDto?.SoftDelete ?? true, tenantContext.CurrentTenantId, cancellationToken);
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

}
