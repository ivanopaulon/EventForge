using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document attachment management with multi-tenant support.
/// Provides comprehensive CRUD operations for document attachments within the authenticated user's tenant context.
/// </summary>
/// <remarks>
/// DEPRECATED: This controller is deprecated in favor of the unified DocumentsController.
/// Use the unified API at /api/v1/documents/attachments/* instead of /api/v1/DocumentAttachments/*.
/// This controller will be removed in a future version.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Obsolete("This controller is deprecated. Use the unified DocumentsController at /api/v1/documents/attachments/* instead. This controller will be removed in a future version.")]
public class DocumentAttachmentsController : BaseApiController
{
    private readonly IDocumentAttachmentService _attachmentService;
    private readonly ITenantContext _tenantContext;

    public DocumentAttachmentsController(IDocumentAttachmentService attachmentService, ITenantContext tenantContext)
    {
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all attachments for a document header.
    /// </summary>
    /// <param name="documentHeaderId">Document header ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document-header/{documentHeaderId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentHeaderAttachments(
        Guid documentHeaderId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _attachmentService.GetDocumentHeaderAttachmentsAsync(documentHeaderId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets all attachments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentRowAttachments(
        Guid documentRowId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _attachmentService.GetDocumentRowAttachmentsAsync(documentRowId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets a document attachment by ID.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document attachment details</returns>
    /// <response code="200">Returns the document attachment</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> GetAttachment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachment = await _attachmentService.GetAttachmentByIdAsync(id, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {id} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new document attachment.
    /// </summary>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document attachment</returns>
    /// <response code="201">Returns the created document attachment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateAttachment(
        [FromBody] CreateDocumentAttachmentDto createDto,
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
            var attachment = await _attachmentService.CreateAttachmentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetAttachment),
                new { id = attachment.Id },
                attachment);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document attachment metadata.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="updateDto">Attachment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document attachment</returns>
    /// <response code="200">Returns the updated document attachment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> UpdateAttachment(
        Guid id,
        [FromBody] UpdateDocumentAttachmentDto updateDto,
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
            var attachment = await _attachmentService.UpdateAttachmentAsync(id, updateDto, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {id} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new version of an existing attachment.
    /// </summary>
    /// <param name="id">Original attachment ID</param>
    /// <param name="versionDto">New version data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New version document attachment</returns>
    /// <response code="201">Returns the new version document attachment</response>
    /// <response code="400">If the version data is invalid</response>
    /// <response code="404">If the original attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateAttachmentVersion(
        Guid id,
        [FromBody] AttachmentVersionDto versionDto,
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
            var newVersion = await _attachmentService.CreateAttachmentVersionAsync(id, versionDto, currentUser, cancellationToken);

            if (newVersion == null)
                return CreateNotFoundProblem($"Document attachment with ID {id} not found.");

            return CreatedAtAction(
                nameof(GetAttachment),
                new { id = newVersion.Id },
                newVersion);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the attachment version.", ex);
        }
    }

    /// <summary>
    /// Gets attachment version history.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachment versions</returns>
    /// <response code="200">Returns the attachment versions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentVersions(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var versions = await _attachmentService.GetAttachmentVersionsAsync(id, cancellationToken);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving attachment versions.", ex);
        }
    }

    /// <summary>
    /// Signs an attachment digitally.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="signatureInfo">Digital signature information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signed document attachment</returns>
    /// <response code="200">Returns the signed document attachment</response>
    /// <response code="400">If the signature data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/sign")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> SignAttachment(
        Guid id,
        [FromBody] string signatureInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureInfo))
        {
            return CreateValidationProblemDetails("Signature information is required.");
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await _attachmentService.SignAttachmentAsync(id, signatureInfo, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {id} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while signing the attachment.", ex);
        }
    }

    /// <summary>
    /// Gets attachments by category.
    /// </summary>
    /// <param name="category">Attachment category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachments in the category</returns>
    /// <response code="200">Returns the attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentsByCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _attachmentService.GetAttachmentsByCategoryAsync(category, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving attachments by category.", ex);
        }
    }

    /// <summary>
    /// Deletes a document attachment (soft delete).
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the attachment was deleted successfully</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAttachment(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _attachmentService.DeleteAttachmentAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document attachment with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document attachment.", ex);
        }
    }

    /// <summary>
    /// Checks if a document attachment exists.
    /// </summary>
    /// <param name="id">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("{id:guid}")]
    [HttpGet("{id:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> AttachmentExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _attachmentService.AttachmentExistsAsync(id, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking attachment existence.", ex);
        }
    }
}