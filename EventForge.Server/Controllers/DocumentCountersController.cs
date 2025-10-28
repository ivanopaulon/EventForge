using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document counter management with multi-tenant support.
/// Provides CRUD operations for document counters within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentCountersController : BaseApiController
{
    private readonly IDocumentCounterService _documentCounterService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentCountersController> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentCountersController
    /// </summary>
    /// <param name="documentCounterService">Document counter service</param>
    /// <param name="tenantContext">Tenant context service</param>
    /// <param name="logger">Logger instance</param>
    public DocumentCountersController(
        IDocumentCounterService documentCounterService,
        ITenantContext tenantContext,
        ILogger<DocumentCountersController> logger)
    {
        _documentCounterService = documentCounterService ?? throw new ArgumentNullException(nameof(documentCounterService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all document counters
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document counters</returns>
    /// <response code="200">Returns the list of document counters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentCounterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentCounterDto>>> GetDocumentCounters(CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var counters = await _documentCounterService.GetAllAsync(cancellationToken);
            return Ok(counters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document counters.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document counters.", ex);
        }
    }

    /// <summary>
    /// Gets all document counters for a specific document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document counters for the specified type</returns>
    /// <response code="200">Returns the list of document counters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("by-type/{documentTypeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCounterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentCounterDto>>> GetDocumentCountersByType(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var counters = await _documentCounterService.GetByDocumentTypeAsync(documentTypeId, cancellationToken);
            return Ok(counters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document counters for type {DocumentTypeId}.", documentTypeId);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving document counters for type {documentTypeId}.", ex);
        }
    }

    /// <summary>
    /// Gets a document counter by ID
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document counter information</returns>
    /// <response code="200">Returns the document counter</response>
    /// <response code="404">If the document counter is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentCounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentCounterDto>> GetDocumentCounter(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var counter = await _documentCounterService.GetByIdAsync(id, cancellationToken);

            if (counter == null)
            {
                return CreateNotFoundProblem($"Document counter with ID {id} not found.");
            }

            return Ok(counter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document counter.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document counter.", ex);
        }
    }

    /// <summary>
    /// Creates a new document counter
    /// </summary>
    /// <param name="createDto">Document counter creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document counter</returns>
    /// <response code="201">Returns the newly created document counter</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentCounterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentCounterDto>> CreateDocumentCounter(
        [FromBody] CreateDocumentCounterDto createDto,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var counter = await _documentCounterService.CreateAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetDocumentCounter), new { id = counter.Id }, counter);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating document counter.");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document counter.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document counter.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document counter
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="updateDto">Document counter update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document counter</returns>
    /// <response code="200">Returns the updated document counter</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the document counter is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentCounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentCounterDto>> UpdateDocumentCounter(
        Guid id,
        [FromBody] UpdateDocumentCounterDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var counter = await _documentCounterService.UpdateAsync(id, updateDto, currentUser, cancellationToken);

            if (counter == null)
            {
                return CreateNotFoundProblem($"Document counter with ID {id} not found.");
            }

            return Ok(counter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document counter.");
            return CreateInternalServerErrorProblem("An error occurred while updating the document counter.", ex);
        }
    }

    /// <summary>
    /// Deletes a document counter
    /// </summary>
    /// <param name="id">Document counter ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">If the document counter was successfully deleted</response>
    /// <response code="404">If the document counter is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocumentCounter(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentCounterService.DeleteAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document counter with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document counter.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document counter.", ex);
        }
    }
}
