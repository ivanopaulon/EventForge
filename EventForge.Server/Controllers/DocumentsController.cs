using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Unified REST API controller for document-related operations with multi-tenant support.
/// Provides aggregated access to document attachments, comments, templates, workflows, and analytics.
/// Delegates business logic to existing specialized services through the DocumentFacade.
/// </summary>
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController : BaseApiController
{
    private readonly IDocumentFacade _documentFacade;
    private readonly ITenantContext _tenantContext;

    public DocumentsController(IDocumentFacade documentFacade, ITenantContext tenantContext)
    {
        _documentFacade = documentFacade ?? throw new ArgumentNullException(nameof(documentFacade));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    // Attachment endpoints
    /// <summary>
    /// Gets all attachments for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentAttachments(
        Guid documentId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _documentFacade.GetAttachmentsAsync(documentId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Creates a new attachment for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created attachment</returns>
    /// <response code="201">Returns the created attachment</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateDocumentAttachment(
        Guid documentId,
        [FromBody] CreateDocumentAttachmentDto createDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await _documentFacade.CreateAttachmentAsync(documentId, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetDocumentAttachments), new { documentId = documentId }, attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document attachment.", ex);
        }
    }

    // Comment endpoints
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await _documentFacade.GetCommentsAsync(documentId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _documentFacade.CreateCommentAsync(documentId, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetDocumentComments), new { documentId = documentId }, comment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document comment.", ex);
        }
    }

    // Template endpoints
    /// <summary>
    /// Gets public document templates available to all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of public document templates</returns>
    /// <response code="200">Returns the public document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/public")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetPublicTemplates(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await _documentFacade.GetPublicTemplatesAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving public document templates.", ex);
        }
    }

    /// <summary>
    /// Gets a document template by ID.
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template</returns>
    /// <response code="200">Returns the document template</response>
    /// <response code="404">If the template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/{templateId:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentTemplateDto>> GetTemplate(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var template = await _documentFacade.GetTemplateByIdAsync(templateId, cancellationToken);
            
            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {templateId} not found.");

            return Ok(template);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document template.", ex);
        }
    }

    // Workflow endpoints
    /// <summary>
    /// Gets document workflows, optionally filtered by document type.
    /// </summary>
    /// <param name="documentId">Document header ID (used to determine document type for filtering)</param>
    /// <param name="documentTypeId">Optional document type ID to filter workflows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document workflows</returns>
    /// <response code="200">Returns the document workflows</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/workflows")]
    [ProducesResponseType(typeof(IEnumerable<DocumentWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentWorkflowDto>>> GetDocumentWorkflows(
        Guid documentId,
        [FromQuery] Guid? documentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflows = await _documentFacade.GetWorkflowsAsync(documentTypeId, cancellationToken);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document workflows.", ex);
        }
    }

    // Analytics endpoints
    /// <summary>
    /// Gets analytics for a specific document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document analytics</returns>
    /// <response code="200">Returns the document analytics</response>
    /// <response code="404">If analytics are not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/analytics")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> GetDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var analytics = await _documentFacade.GetAnalyticsAsync(documentId, cancellationToken);
            
            if (analytics == null)
                return CreateNotFoundProblem($"Analytics for document with ID {documentId} not found.");

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document analytics.", ex);
        }
    }

    /// <summary>
    /// Refreshes (creates or updates) analytics for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document analytics</returns>
    /// <response code="200">Returns the updated document analytics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/analytics/refresh")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> RefreshDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await _documentFacade.RefreshAnalyticsAsync(documentId, currentUser, cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while refreshing document analytics.", ex);
        }
    }
}