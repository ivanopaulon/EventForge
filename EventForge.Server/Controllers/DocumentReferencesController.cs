using EventForge.Server.Services.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Teams;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for managing document references for teams and team members.
/// Handles upload, retrieval, and management of documents like medical certificates, photos, etc.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentReferencesController(
    ITeamService teamService,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Gets all documents for a specific owner (Team or TeamMember).
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="ownerType">Owner type ("Team" or "TeamMember")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document references</returns>
    /// <response code="200">Returns the list of documents</response>
    /// <response code="400">If the request parameters are invalid</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentReferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentReferenceDto>>> GetDocumentsByOwner(
        Guid ownerId,
        [FromQuery] string ownerType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            // Validate parameters
            if (string.IsNullOrWhiteSpace(ownerType))
            {
                return CreateValidationProblemDetails("Owner type is required.");
            }

            if (ownerType != "Team" && ownerType != "TeamMember")
            {
                return CreateValidationProblemDetails("Owner type must be 'Team' or 'TeamMember'.");
            }

            var documents = await teamService.GetDocumentsByOwnerAsync(ownerId, ownerType, cancellationToken);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving documents", ex);
        }
    }

    /// <summary>
    /// Gets a specific document reference by ID.
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document reference details</returns>
    /// <response code="200">Returns the document reference</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentReferenceDto>> GetDocumentReference(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var document = await teamService.GetDocumentReferenceByIdAsync(id, cancellationToken);

            if (document is null)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving document reference", ex);
        }
    }

    /// <summary>
    /// Creates a new document reference.
    /// </summary>
    /// <param name="createDocumentDto">Document creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document reference</returns>
    /// <response code="201">Returns the created document reference</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentReferenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentReferenceDto>> CreateDocumentReference(
        [FromBody] CreateDocumentReferenceDto createDocumentDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var document = await teamService.CreateDocumentReferenceAsync(createDocumentDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentReference),
                new { id = document.Id },
                document);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating document reference", ex);
        }
    }

    /// <summary>
    /// Updates an existing document reference.
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="updateDocumentDto">Document update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document reference</returns>
    /// <response code="200">Returns the updated document reference</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentReferenceDto>> UpdateDocumentReference(
        Guid id,
        [FromBody] UpdateDocumentReferenceDto updateDocumentDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var document = await teamService.UpdateDocumentReferenceAsync(id, updateDocumentDto, currentUser, cancellationToken);

            if (document is null)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating document reference", ex);
        }
    }

    /// <summary>
    /// Uploads a file and creates a DocumentReference for it, for a given owner (Team or TeamMember).
    /// Reuses the same local wwwroot storage pattern already used elsewhere in the project
    /// (e.g. product image uploads) rather than introducing a new storage mechanism.
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="ownerId">Owner ID (Team or TeamMember)</param>
    /// <param name="ownerType">Owner type ("Team" or "TeamMember")</param>
    /// <param name="type">Document type</param>
    /// <param name="subType">Document sub-type</param>
    /// <param name="expiry">Optional expiry date (e.g. for certificates)</param>
    /// <param name="title">Optional title/description</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document reference</returns>
    /// <response code="201">Returns the created document reference</response>
    /// <response code="400">If the file or request data is invalid</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentReferenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<DocumentReferenceDto>> UploadDocument(
        IFormFile file,
        [FromForm] Guid ownerId,
        [FromForm] string ownerType,
        [FromForm] DocumentReferenceType type,
        [FromForm] DocumentReferenceSubType subType = DocumentReferenceSubType.None,
        [FromForm] DateTime? expiry = null,
        [FromForm] string? title = null,
        [FromForm] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file is null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(ownerType) || (ownerType != "Team" && ownerType != "TeamMember"))
        {
            return CreateValidationProblemDetails("Owner type must be 'Team' or 'TeamMember'.");
        }

        const long maxFileSize = 20 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB.");
        }

        try
        {
            var extension = Path.GetExtension(file.FileName);
            var storedFileName = $"{ownerType.ToLowerInvariant()}_{ownerId}_{Guid.NewGuid()}{extension}";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", ownerType.ToLowerInvariant());
            _ = Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, storedFileName);
            var storageKey = $"/documents/{ownerType.ToLowerInvariant()}/{storedFileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var createDocumentDto = new CreateDocumentReferenceDto
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                FileName = file.FileName,
                Type = type,
                SubType = subType,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                Expiry = expiry,
                FileSizeBytes = file.Length,
                Title = title,
                Notes = notes
            };

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var document = await teamService.CreateDocumentReferenceAsync(createDocumentDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentReference),
                new { id = document.Id },
                document);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error uploading document", ex);
        }
    }

    /// <summary>
    /// Deletes a document reference (soft delete).
    /// </summary>
    /// <param name="id">Document reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the document was deleted successfully</response>
    /// <response code="404">If the document is not found</response>
    /// <response code="403">If user lacks permissions for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocumentReference(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

            var currentUser = tenantContext.CurrentUserId?.ToString() ?? "System";
            var deleted = await teamService.DeleteDocumentReferenceAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting document reference", ex);
        }
    }
}