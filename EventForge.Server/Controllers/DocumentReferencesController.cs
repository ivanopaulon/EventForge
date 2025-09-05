using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Teams;
using EventForge.Server.Services.Teams;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for managing document references for teams and team members.
/// Handles upload, retrieval, and management of documents like medical certificates, photos, etc.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentReferencesController : BaseApiController
{
    private readonly ITeamService _teamService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentReferencesController> _logger;

    public DocumentReferencesController(
        ITeamService teamService,
        ITenantContext tenantContext,
        ILogger<DocumentReferencesController> logger)
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            // Validate parameters
            if (string.IsNullOrWhiteSpace(ownerType))
            {
                return CreateValidationProblemDetails("Owner type is required.");
            }

            if (ownerType != "Team" && ownerType != "TeamMember")
            {
                return CreateValidationProblemDetails("Owner type must be 'Team' or 'TeamMember'.");
            }

            var documents = await _teamService.GetDocumentsByOwnerAsync(ownerId, ownerType, cancellationToken);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for owner {OwnerId} of type {OwnerType}", ownerId, ownerType);
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
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            var document = await _teamService.GetDocumentReferenceByIdAsync(id, cancellationToken);

            if (document == null)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document reference {DocumentId}", id);
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
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var document = await _teamService.CreateDocumentReferenceAsync(createDocumentDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentReference),
                new { id = document.Id },
                document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document reference");
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
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            // Validate model
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var document = await _teamService.UpdateDocumentReferenceAsync(id, updateDocumentDto, currentUser, cancellationToken);

            if (document == null)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document reference {DocumentId}", id);
            return CreateInternalServerErrorProblem("Error updating document reference", ex);
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
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            var currentUser = _tenantContext.CurrentUserId?.ToString() ?? "System";
            var deleted = await _teamService.DeleteDocumentReferenceAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document reference {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document reference {DocumentId}", id);
            return CreateInternalServerErrorProblem("Error deleting document reference", ex);
        }
    }
}