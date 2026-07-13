using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{
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
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await documentFacade.GetAttachmentsAsync(documentId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets attachments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentRowAttachments(
        Guid documentRowId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await documentFacade.GetDocumentRowAttachmentsAsync(documentRowId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets an attachment by ID.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document attachment details</returns>
    /// <response code="200">Returns the document attachment</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> GetAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachment = await documentFacade.GetAttachmentByIdAsync(attachmentId, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document attachment.", ex);
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
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            createDto.DocumentHeaderId = documentId;
            var attachment = await documentFacade.CreateAttachmentAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetAttachment), new { attachmentId = attachment.Id }, attachment);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new attachment using generic creation endpoint.
    /// </summary>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document attachment</returns>
    /// <response code="201">Returns the created document attachment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments")]
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

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await documentFacade.CreateAttachmentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetAttachment),
                new { attachmentId = attachment.Id },
                attachment);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
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
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="updateDto">Attachment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document attachment</returns>
    /// <response code="200">Returns the updated document attachment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> UpdateAttachment(
        Guid attachmentId,
        [FromBody] UpdateDocumentAttachmentDto updateDto,
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
            var attachment = await documentFacade.UpdateAttachmentAsync(attachmentId, updateDto, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

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
    /// <param name="attachmentId">Original attachment ID</param>
    /// <param name="versionDto">New version data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New version document attachment</returns>
    /// <response code="201">Returns the new version document attachment</response>
    /// <response code="400">If the version data is invalid</response>
    /// <response code="404">If the original attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments/{attachmentId:guid}/versions")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateAttachmentVersion(
        Guid attachmentId,
        [FromBody] AttachmentVersionDto versionDto,
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
            var newVersion = await documentFacade.CreateAttachmentVersionAsync(attachmentId, versionDto, currentUser, cancellationToken);

            if (newVersion == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return CreatedAtAction(
                nameof(GetAttachment),
                new { attachmentId = newVersion.Id },
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
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachment versions</returns>
    /// <response code="200">Returns the attachment versions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/{attachmentId:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentVersions(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var versions = await documentFacade.GetAttachmentVersionsAsync(attachmentId, cancellationToken);
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
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="signatureInfo">Digital signature information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signed document attachment</returns>
    /// <response code="200">Returns the signed document attachment</response>
    /// <response code="400">If the signature data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments/{attachmentId:guid}/sign")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> SignAttachment(
        Guid attachmentId,
        [FromBody] string signatureInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureInfo))
        {
            return CreateValidationProblemDetails("Signature information is required.");
        }

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await documentFacade.SignAttachmentAsync(attachmentId, signatureInfo, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

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
    [HttpGet("attachments/category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentsByCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await documentFacade.GetAttachmentsByCategoryAsync(category, cancellationToken);
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
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the attachment was deleted successfully</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await documentFacade.DeleteAttachmentAsync(attachmentId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

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
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("attachments/{attachmentId:guid}")]
    [HttpGet("attachments/{attachmentId:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> AttachmentExists(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await documentFacade.AttachmentExistsAsync(attachmentId, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking attachment existence.", ex);
        }
    }

    // Comment endpoints
}
