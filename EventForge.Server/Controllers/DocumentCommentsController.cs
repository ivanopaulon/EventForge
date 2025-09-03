using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document comment management with multi-tenant support.
/// Provides collaboration features for documents including comments, mentions, and task assignments.
/// </summary>
/// <remarks>
/// DEPRECATED: This controller is deprecated in favor of the unified DocumentsController.
/// Use the unified API at /api/v1/documents/comments/* instead of /api/v1/DocumentComments/*.
/// This controller will be removed in a future version.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Obsolete("This controller is deprecated. Use the unified DocumentsController at /api/v1/documents/comments/* instead. This controller will be removed in a future version.")]
public class DocumentCommentsController : BaseApiController
{
    private readonly IDocumentCommentService _commentService;
    private readonly ITenantContext _tenantContext;

    public DocumentCommentsController(IDocumentCommentService commentService, ITenantContext tenantContext)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all comments for a document header.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document-header/{documentHeaderId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentHeaderComments(
        Guid documentHeaderId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await _commentService.GetDocumentHeaderCommentsAsync(documentHeaderId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets all comments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentRowComments(
        Guid documentRowId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await _commentService.GetDocumentRowCommentsAsync(documentRowId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets a document comment by ID.
    /// </summary>
    /// <param name="id">Comment ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document comment details</returns>
    /// <response code="200">Returns the document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> GetComment(
        Guid id,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comment = await _commentService.GetCommentByIdAsync(id, includeReplies, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {id} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document comment.", ex);
        }
    }

    /// <summary>
    /// Creates a new document comment.
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document comment</returns>
    /// <response code="201">Returns the created document comment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.CreateCommentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetComment),
                new { id = comment.Id },
                comment);
        }
        catch (ArgumentException ex)
        {
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
    /// <param name="id">Comment ID</param>
    /// <param name="updateDto">Comment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document comment</returns>
    /// <response code="200">Returns the updated document comment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> UpdateComment(
        Guid id,
        [FromBody] UpdateDocumentCommentDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.UpdateCommentAsync(id, updateDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {id} not found.");

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
    /// <param name="id">Comment ID</param>
    /// <param name="resolveDto">Resolution data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved document comment</returns>
    /// <response code="200">Returns the resolved document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ResolveComment(
        Guid id,
        [FromBody] ResolveCommentDto resolveDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.ResolveCommentAsync(id, resolveDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {id} not found.");

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
    /// <param name="id">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reopened document comment</returns>
    /// <response code="200">Returns the reopened document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/reopen")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ReopenComment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.ReopenCommentAsync(id, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {id} not found.");

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
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comment statistics</returns>
    /// <response code="200">Returns the comment statistics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document-header/{documentHeaderId:guid}/stats")]
    [ProducesResponseType(typeof(DocumentCommentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentStatsDto>> GetDocumentCommentStats(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var stats = await _commentService.GetDocumentCommentStatsAsync(documentHeaderId, currentUser, cancellationToken);
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
    [HttpGet("assigned")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetAssignedComments(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comments = await _commentService.GetAssignedCommentsAsync(currentUser, status, cancellationToken);
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
    /// <param name="id">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the comment was deleted successfully</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _commentService.DeleteCommentAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document comment with ID {id} not found.");

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
    /// <param name="id">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("{id:guid}")]
    [HttpGet("{id:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CommentExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _commentService.CommentExistsAsync(id, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking comment existence.", ex);
        }
    }
}