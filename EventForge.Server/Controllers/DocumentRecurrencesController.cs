using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document recurrence management with multi-tenant support.
/// Provides CRUD operations for document recurrences within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentRecurrencesController : BaseApiController
{
    private readonly IDocumentRecurrenceService _documentRecurrenceService;
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes a new instance of the DocumentRecurrencesController
    /// </summary>
    /// <param name="documentRecurrenceService">Document recurrence service</param>
    /// <param name="tenantContext">Tenant context service</param>
    public DocumentRecurrencesController(IDocumentRecurrenceService documentRecurrenceService, ITenantContext tenantContext)
    {
        _documentRecurrenceService = documentRecurrenceService ?? throw new ArgumentNullException(nameof(documentRecurrenceService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all document recurrences
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document recurrences</returns>
    /// <response code="200">Returns the list of document recurrences</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentRecurrenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentRecurrenceDto>>> GetDocumentRecurrences(CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var recurrences = await _documentRecurrenceService.GetAllAsync(cancellationToken);
            return Ok(recurrences);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document recurrences.", ex);
        }
    }

    /// <summary>
    /// Gets active document recurrence schedules
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active document recurrences</returns>
    /// <response code="200">Returns the list of active document recurrences</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<DocumentRecurrenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentRecurrenceDto>>> GetActiveDocumentRecurrences(CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var recurrences = await _documentRecurrenceService.GetActiveSchedulesAsync(cancellationToken);
            return Ok(recurrences);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active document recurrences.", ex);
        }
    }

    /// <summary>
    /// Gets a specific document recurrence by ID
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document recurrence details</returns>
    /// <response code="200">Returns the document recurrence</response>
    /// <response code="404">If the document recurrence is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentRecurrenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentRecurrenceDto>> GetDocumentRecurrence(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var recurrence = await _documentRecurrenceService.GetByIdAsync(id, cancellationToken);
            if (recurrence == null)
                return CreateNotFoundProblem($"Document recurrence with ID {id} was not found.");

            return Ok(recurrence);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document recurrence.", ex);
        }
    }

    /// <summary>
    /// Creates a new document recurrence
    /// </summary>
    /// <param name="createDto">Document recurrence creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document recurrence</returns>
    /// <response code="201">Document recurrence created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentRecurrenceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentRecurrenceDto>> CreateDocumentRecurrence([FromBody] CreateDocumentRecurrenceDto createDto, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var recurrence = await _documentRecurrenceService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentRecurrence),
                new { id = recurrence.Id },
                recurrence);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document recurrence.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document recurrence
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="updateDto">Document recurrence update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document recurrence</returns>
    /// <response code="200">Document recurrence updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document recurrence is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentRecurrenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentRecurrenceDto>> UpdateDocumentRecurrence(Guid id, [FromBody] UpdateDocumentRecurrenceDto updateDto, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var recurrence = await _documentRecurrenceService.UpdateAsync(id, updateDto, currentUser, cancellationToken);

            if (recurrence == null)
                return CreateNotFoundProblem($"Document recurrence with ID {id} was not found.");

            return Ok(recurrence);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document recurrence.", ex);
        }
    }

    /// <summary>
    /// Deletes a document recurrence (soft delete)
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document recurrence deleted successfully</response>
    /// <response code="404">If the document recurrence is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocumentRecurrence(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentRecurrenceService.DeleteAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document recurrence with ID {id} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document recurrence.", ex);
        }
    }

    /// <summary>
    /// Enables or disables a document recurrence schedule
    /// </summary>
    /// <param name="id">Document recurrence ID</param>
    /// <param name="enabled">Whether to enable or disable the schedule</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document recurrence status updated successfully</response>
    /// <response code="404">If the document recurrence is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("{id:guid}/enabled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetRecurrenceEnabledStatus(Guid id, [FromQuery] bool enabled, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var updated = await _documentRecurrenceService.SetEnabledStatusAsync(id, enabled, currentUser, cancellationToken);

            if (!updated)
                return CreateNotFoundProblem($"Document recurrence with ID {id} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating recurrence status.", ex);
        }
    }
}