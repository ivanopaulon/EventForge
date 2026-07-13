using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{
    /// <summary>
    /// Gets all comments for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/comments")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentComments(
        Guid documentId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await documentFacade.GetCommentsAsync(documentId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets comments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentRowComments(
        Guid documentRowId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await documentFacade.GetDocumentRowCommentsAsync(documentRowId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document comment details</returns>
    /// <response code="200">Returns the document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> GetComment(
        Guid commentId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comment = await documentFacade.GetCommentByIdAsync(commentId, includeReplies, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document comment.", ex);
        }
    }

    /// <summary>
    /// Creates a new comment for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created comment</returns>
    /// <response code="201">Returns the created comment</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/comments")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> CreateDocumentComment(
        Guid documentId,
        [FromBody] CreateDocumentCommentDto createDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            createDto.DocumentHeaderId = documentId;
            var comment = await documentFacade.CreateCommentAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetComment), new { commentId = comment.Id }, comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document comment.", ex);
        }
    }

    /// <summary>
    /// Creates a new comment using generic creation endpoint.
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document comment</returns>
    /// <response code="201">Returns the created document comment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> CreateComment(
        [FromBody] CreateDocumentCommentDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await documentFacade.CreateCommentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetComment),
                new { commentId = comment.Id },
                comment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document comment.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="updateDto">Comment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document comment</returns>
    /// <response code="200">Returns the updated document comment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> UpdateComment(
        Guid commentId,
        [FromBody] UpdateDocumentCommentDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await documentFacade.UpdateCommentAsync(commentId, updateDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document comment.", ex);
        }
    }

    /// <summary>
    /// Resolves a document comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="resolveDto">Resolution data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved document comment</returns>
    /// <response code="200">Returns the resolved document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments/{commentId:guid}/resolve")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ResolveComment(
        Guid commentId,
        [FromBody] ResolveCommentDto resolveDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await documentFacade.ResolveCommentAsync(commentId, resolveDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while resolving the comment.", ex);
        }
    }

    /// <summary>
    /// Reopens a resolved comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reopened document comment</returns>
    /// <response code="200">Returns the reopened document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments/{commentId:guid}/reopen")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ReopenComment(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await documentFacade.ReopenCommentAsync(commentId, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while reopening the comment.", ex);
        }
    }

    /// <summary>
    /// Gets comment statistics for a document header.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comment statistics</returns>
    /// <response code="200">Returns the comment statistics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/comments/stats")]
    [ProducesResponseType(typeof(DocumentCommentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentStatsDto>> GetDocumentCommentStats(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var stats = await documentFacade.GetDocumentCommentStatsAsync(documentId, currentUser, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving comment statistics.", ex);
        }
    }

    /// <summary>
    /// Gets comments assigned to the current user.
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of assigned comments</returns>
    /// <response code="200">Returns the assigned comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/assigned")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetAssignedComments(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comments = await documentFacade.GetAssignedCommentsAsync(currentUser, status, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving assigned comments.", ex);
        }
    }

    /// <summary>
    /// Deletes a document comment (soft delete).
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the comment was deleted successfully</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await documentFacade.DeleteCommentAsync(commentId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document comment.", ex);
        }
    }

    /// <summary>
    /// Checks if a document comment exists.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("comments/{commentId:guid}")]
    [HttpGet("comments/{commentId:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CommentExists(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await documentFacade.CommentExistsAsync(commentId, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking comment existence.", ex);
        }
    }

    // Template endpoints
}
