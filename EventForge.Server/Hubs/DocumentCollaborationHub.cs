using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventForge.Server.Hubs;

/// <summary>
/// SignalR hub for real-time document collaboration functionality with multi-tenant support.
/// Handles real-time comments, threading, task assignments, mentions, and status updates on documents.
/// </summary>
[Authorize]
public class DocumentCollaborationHub : Hub
{
    private readonly ILogger<DocumentCollaborationHub> _logger;
    private readonly IDocumentCommentService _commentService;
    private readonly IDocumentHeaderService _documentHeaderService;

    public DocumentCollaborationHub(
        ILogger<DocumentCollaborationHub> logger,
        IDocumentCommentService commentService,
        IDocumentHeaderService documentHeaderService)
    {
        _logger = logger;
        _commentService = commentService;
        _documentHeaderService = documentHeaderService;
    }

    #region Connection Management

    /// <summary>
    /// Called when a client connects to the hub.
    /// Automatically joins user to their tenant group for document collaboration.
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

            _logger.LogInformation("User {UserId} connected to document collaboration hub for tenant {TenantId}",
                userId.Value, tenantId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Auto-releases any locks held by this connection.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();

        if (userId.HasValue)
        {
            // Release all locks for this connection
            await _documentHeaderService.ReleaseAllLocksForConnectionAsync(Context.ConnectionId);

            _logger.LogInformation(
                "User {UserId} disconnected from document collaboration hub. Locks released for connection {ConnectionId}.",
                userId.Value, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Document Lock Management

    /// <summary>
    /// Request exclusive edit lock for a document.
    /// </summary>
    /// <param name="documentId">ID of document to lock</param>
    /// <returns>True if lock acquired successfully</returns>
    public async Task<bool> RequestEditLock(Guid documentId)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        _logger.LogDebug(
            "RequestEditLock called: DocumentId={DocumentId}, UserName={UserName}, UserId={UserId}, TenantId={TenantId}, ConnectionId={ConnectionId}",
            documentId, userName, userId, tenantId, Context.ConnectionId);

        if (!userId.HasValue)
        {
            _logger.LogWarning(
                "RequestEditLock FAILED: UserId is null. DocumentId={DocumentId}, UserName={UserName}",
                documentId, userName);
            throw new HubException("User not authenticated");
        }
        
        // Add tenant validation
        if (!tenantId.HasValue)
        {
            _logger.LogWarning(
                "RequestEditLock FAILED: TenantId is null. DocumentId={DocumentId}, UserName={UserName}, UserId={UserId}",
                documentId, userName, userId.Value);
            throw new HubException("Autenticazione non valida: Tenant ID non trovato. Effettua nuovamente il login.");
        }

        try
        {
            _logger.LogDebug(
                "Calling AcquireLockAsync: DocumentId={DocumentId}, UserName={UserName}, ConnectionId={ConnectionId}",
                documentId, userName, Context.ConnectionId);

            var lockAcquired = await _documentHeaderService.AcquireLockAsync(
                documentId,
                userName,
                Context.ConnectionId);

            if (lockAcquired)
            {
                _logger.LogInformation(
                    "✅ Lock acquired in Hub for document {DocumentId} by {UserName}",
                    documentId, userName);

                // Notify other users in the document group
                await Clients.GroupExcept($"document_{documentId}", Context.ConnectionId)
                    .SendAsync("DocumentLocked", new
                    {
                        DocumentId = documentId,
                        LockedBy = userName,
                        LockedAt = DateTime.UtcNow
                    });

                return true;
            }
            else
            {
                _logger.LogWarning(
                    "❌ AcquireLockAsync returned FALSE for document {DocumentId}, user {UserName}",
                    documentId, userName);

                var lockInfo = await _documentHeaderService.GetLockInfoAsync(documentId);
                
                if (lockInfo != null)
                {
                    throw new HubException(
                        $"Documento in modifica da {lockInfo.LockedBy} dal {lockInfo.LockedAt:HH:mm}");
                }
                else
                {
                    throw new HubException(
                        "Documento attualmente non disponibile per la modifica. Riprova tra qualche istante.");
                }
            }
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Exception in RequestEditLock for document {DocumentId}, user {UserName}. Exception type: {ExceptionType}",
                documentId, userName, ex.GetType().Name);
            throw new HubException("Errore acquisizione lock documento");
        }
    }

    /// <summary>
    /// Release edit lock for a document.
    /// </summary>
    /// <param name="documentId">ID of document to unlock</param>
    public async Task ReleaseEditLock(Guid documentId)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";

        try
        {
            var released = await _documentHeaderService.ReleaseLockAsync(documentId, userName);

            if (released)
            {
                _logger.LogInformation(
                    "User {UserName} released lock on document {DocumentId}",
                    userName,
                    documentId);

                // Notify all users in the document group
                await Clients.Group($"document_{documentId}")
                    .SendAsync("DocumentUnlocked", new { DocumentId = documentId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lock for document {DocumentId}", documentId);
            throw new HubException("Errore rilascio lock documento");
        }
    }

    #endregion

    #region Document Subscription

    /// <summary>
    /// Joins a document collaboration room to receive real-time updates.
    /// </summary>
    /// <param name="documentId">ID of the document header to subscribe to</param>
    public async Task JoinDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            _logger.LogWarning(
                "JoinDocument failed: UserId claim not found. ConnectionId: {ConnectionId}, User claims: {@Claims}",
                Context.ConnectionId,
                Context.User?.Claims.Select(c => new { c.Type, c.Value }));
            throw new HubException("Autenticazione non valida: User ID non trovato. Effettua nuovamente il login.");
        }

        if (!tenantId.HasValue)
        {
            _logger.LogWarning(
                "JoinDocument failed: TenantId claim not found. ConnectionId: {ConnectionId}, UserId: {UserId}",
                Context.ConnectionId,
                userId.Value);
            throw new HubException("Autenticazione non valida: Tenant ID non trovato. Effettua nuovamente il login.");
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"document_{documentId}");

            _logger.LogInformation(
                "User {UserName} (ID: {UserId}) joined document {DocumentId} collaboration room",
                userName, userId.Value, documentId);

            // Notify other users in the document that someone joined
            await Clients.GroupExcept($"document_{documentId}", Context.ConnectionId)
                .SendAsync("UserJoinedDocument", new
                {
                    DocumentId = documentId,
                    UserName = userName,
                    JoinedAt = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to join document {DocumentId} for user {UserId}", 
                documentId, userId.Value);
            throw new HubException("Errore durante la connessione al documento. Riprova.");
        }
    }

    /// <summary>
    /// Leaves a document collaboration room.
    /// </summary>
    /// <param name="documentId">ID of the document header to unsubscribe from</param>
    public async Task LeaveDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"document_{documentId}");

            _logger.LogInformation("User {UserId} left document {DocumentId} collaboration room",
                userId.Value, documentId);

            // Notify other users in the document that someone left
            await Clients.Group($"document_{documentId}")
                .SendAsync("UserLeftDocument", new { DocumentId = documentId, UserId = userId.Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave document {DocumentId} for user {UserId}", documentId, userId.Value);
            throw new HubException("Failed to leave document collaboration room");
        }
    }

    #endregion

    #region Comment Management

    /// <summary>
    /// Creates a new comment on a document and broadcasts it to all subscribers.
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    public async Task CreateComment(CreateDocumentCommentDto createDto)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var tenantId = GetCurrentTenantId();

        if (!userId.HasValue || !tenantId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var comment = await _commentService.CreateCommentAsync(createDto, userName, CancellationToken.None);

            var documentId = createDto.DocumentHeaderId ?? Guid.Empty;

            // Broadcast new comment to all users watching this document
            await Clients.Group($"document_{documentId}")
                .SendAsync("CommentCreated", comment);

            // If there are mentions, notify mentioned users
            if (!string.IsNullOrEmpty(createDto.MentionedUsers))
            {
                await NotifyMentionedUsers(createDto.MentionedUsers, comment, documentId);
            }

            // If this is an assignment, notify the assigned user
            if (!string.IsNullOrEmpty(createDto.AssignedTo))
            {
                await Clients.Group($"user_{createDto.AssignedTo}")
                    .SendAsync("TaskAssigned", new
                    {
                        Comment = comment,
                        DocumentId = documentId,
                        AssignedBy = userName
                    });
            }

            _logger.LogInformation("User {UserId} created comment {CommentId} on document {DocumentId}",
                userId.Value, comment.Id, documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment for user {UserId}", userId.Value);
            throw new HubException("Failed to create comment");
        }
    }

    /// <summary>
    /// Updates an existing comment and broadcasts the update.
    /// </summary>
    /// <param name="commentId">ID of the comment to update</param>
    /// <param name="updateDto">Comment update data</param>
    public async Task UpdateComment(Guid commentId, UpdateDocumentCommentDto updateDto)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var updatedComment = await _commentService.UpdateCommentAsync(commentId, updateDto, userName, CancellationToken.None);

            if (updatedComment == null)
            {
                throw new HubException("Comment not found");
            }

            var documentId = updatedComment.DocumentHeaderId ?? Guid.Empty;

            // Broadcast comment update to all users watching this document
            await Clients.Group($"document_{documentId}")
                .SendAsync("CommentUpdated", updatedComment);

            // If assignment changed, notify the newly assigned user
            if (updateDto.AssignedTo != null && updatedComment.AssignedTo != null)
            {
                await Clients.Group($"user_{updatedComment.AssignedTo}")
                    .SendAsync("TaskAssigned", new
                    {
                        Comment = updatedComment,
                        DocumentId = documentId,
                        AssignedBy = userName
                    });
            }

            _logger.LogInformation("User {UserId} updated comment {CommentId}", userId.Value, commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update comment {CommentId} for user {UserId}", commentId, userId.Value);
            throw new HubException("Failed to update comment");
        }
    }

    /// <summary>
    /// Deletes a comment and broadcasts the deletion.
    /// </summary>
    /// <param name="commentId">ID of the comment to delete</param>
    public async Task DeleteComment(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // Get comment info before deletion to know which document to notify
            var comment = await _commentService.GetCommentByIdAsync(commentId, false, CancellationToken.None);

            if (comment == null)
            {
                throw new HubException("Comment not found");
            }

            var deleted = await _commentService.DeleteCommentAsync(commentId, userName, CancellationToken.None);

            if (!deleted)
            {
                throw new HubException("Failed to delete comment");
            }

            var documentId = comment.DocumentHeaderId ?? Guid.Empty;

            // Broadcast comment deletion to all users watching this document
            await Clients.Group($"document_{documentId}")
                .SendAsync("CommentDeleted", new { CommentId = commentId, DeletedBy = userName });

            _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId.Value, commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete comment {CommentId} for user {UserId}", commentId, userId.Value);
            throw new HubException("Failed to delete comment");
        }
    }

    /// <summary>
    /// Resolves a comment and broadcasts the resolution.
    /// </summary>
    /// <param name="commentId">ID of the comment to resolve</param>
    /// <param name="resolveDto">Resolution data</param>
    public async Task ResolveComment(Guid commentId, ResolveCommentDto resolveDto)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var resolvedComment = await _commentService.ResolveCommentAsync(commentId, resolveDto, userName, CancellationToken.None);

            if (resolvedComment == null)
            {
                throw new HubException("Comment not found");
            }

            var documentId = resolvedComment.DocumentHeaderId ?? Guid.Empty;

            // Broadcast comment resolution to all users watching this document
            await Clients.Group($"document_{documentId}")
                .SendAsync("CommentResolved", new
                {
                    Comment = resolvedComment,
                    ResolvedBy = userName,
                    ResolutionNotes = resolveDto.ResolutionNotes
                });

            _logger.LogInformation("User {UserId} resolved comment {CommentId}", userId.Value, commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve comment {CommentId} for user {UserId}", commentId, userId.Value);
            throw new HubException("Failed to resolve comment");
        }
    }

    /// <summary>
    /// Reopens a resolved comment and broadcasts the action.
    /// </summary>
    /// <param name="commentId">ID of the comment to reopen</param>
    public async Task ReopenComment(Guid commentId)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            var reopenedComment = await _commentService.ReopenCommentAsync(commentId, userName, CancellationToken.None);

            if (reopenedComment == null)
            {
                throw new HubException("Comment not found");
            }

            var documentId = reopenedComment.DocumentHeaderId ?? Guid.Empty;

            // Broadcast comment reopening to all users watching this document
            await Clients.Group($"document_{documentId}")
                .SendAsync("CommentReopened", new
                {
                    Comment = reopenedComment,
                    ReopenedBy = userName
                });

            _logger.LogInformation("User {UserId} reopened comment {CommentId}", userId.Value, commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reopen comment {CommentId} for user {UserId}", commentId, userId.Value);
            throw new HubException("Failed to reopen comment");
        }
    }

    #endregion

    #region Typing Indicators

    /// <summary>
    /// Sends typing indicator to document collaborators.
    /// </summary>
    /// <param name="documentId">ID of the document where user is typing</param>
    /// <param name="isTyping">Whether user is currently typing</param>
    public async Task SendTypingIndicator(Guid documentId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();

        if (!userId.HasValue)
        {
            throw new HubException("User not authenticated");
        }

        try
        {
            // Send typing indicator to other document collaborators (excluding sender)
            await Clients.GroupExcept($"document_{documentId}", Context.ConnectionId)
                .SendAsync("TypingIndicator", new
                {
                    DocumentId = documentId,
                    UserId = userId.Value,
                    UserName = userName,
                    IsTyping = isTyping,
                    Timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing indicator for user {UserId} in document {DocumentId}",
                userId.Value, documentId);
            // Don't throw exception for typing indicators as they're not critical
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Notifies mentioned users about a new mention in a comment.
    /// </summary>
    private async Task NotifyMentionedUsers(string mentionedUsers, DocumentCommentDto comment, Guid documentId)
    {
        if (string.IsNullOrWhiteSpace(mentionedUsers))
            return;

        try
        {
            var users = mentionedUsers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var user in users)
            {
                await Clients.Group($"user_{user}")
                    .SendAsync("UserMentioned", new
                    {
                        Comment = comment,
                        DocumentId = documentId,
                        MentionedBy = comment.CreatedBy
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify mentioned users for comment {CommentId}", comment.Id);
        }
    }

    /// <summary>
    /// Gets the current user ID from the connection context.
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user name from the connection context.
    /// </summary>
    private string GetCurrentUserName()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value
            ?? Context.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? "Unknown User";
    }

    /// <summary>
    /// Gets the current tenant ID from the connection context.
    /// </summary>
    private Guid? GetCurrentTenantId()
    {
        // Primary: try snake_case (matches JWT token format)
        var tenantIdClaim = Context.User?.FindFirst("tenant_id")?.Value;
        
        // Fallback: try PascalCase for backward compatibility
        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        }
        
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return tenantId;
        }
        
        // Additional logging for debugging (only log claim types, not values)
        _logger.LogWarning(
            "TenantId claim not found in user context. Available claim types: {ClaimTypes}",
            string.Join(", ", Context.User?.Claims.Select(c => c.Type) ?? Enumerable.Empty<string>()));
        
        return null;
    }

    #endregion
}
