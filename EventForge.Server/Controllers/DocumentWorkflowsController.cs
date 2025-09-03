using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document workflow management with multi-tenant support.
/// Provides CRUD operations for document workflows within the authenticated user's tenant context.
/// </summary>
/// <remarks>
/// DEPRECATED: This controller is deprecated in favor of the unified DocumentsController.
/// Use the unified API at /api/v1/documents/workflows/* instead of /api/v1/DocumentWorkflows/*.
/// This controller will be removed in a future version.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Obsolete("This controller is deprecated. Use the unified DocumentsController at /api/v1/documents/workflows/* instead. This controller will be removed in a future version.")]
public class DocumentWorkflowsController : BaseApiController
{
    private readonly IDocumentWorkflowService _documentWorkflowService;
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes a new instance of the DocumentWorkflowsController
    /// </summary>
    /// <param name="documentWorkflowService">Document workflow service</param>
    /// <param name="tenantContext">Tenant context service</param>
    public DocumentWorkflowsController(IDocumentWorkflowService documentWorkflowService, ITenantContext tenantContext)
    {
        _documentWorkflowService = documentWorkflowService ?? throw new ArgumentNullException(nameof(documentWorkflowService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all document workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflows</returns>
    /// <response code="200">Returns the list of document workflows</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentWorkflowDto>>> GetDocumentWorkflows(CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var workflows = await _documentWorkflowService.GetAllAsync(cancellationToken);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document workflows.", ex);
        }
    }

    /// <summary>
    /// Gets a specific document workflow by ID
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document workflow details</returns>
    /// <response code="200">Returns the document workflow</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> GetDocumentWorkflow(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var workflow = await _documentWorkflowService.GetByIdAsync(id, cancellationToken);
            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {id} was not found.");

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document workflow.", ex);
        }
    }

    /// <summary>
    /// Creates a new document workflow
    /// </summary>
    /// <param name="createDto">Document workflow creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document workflow</returns>
    /// <response code="201">Document workflow created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> CreateDocumentWorkflow([FromBody] CreateDocumentWorkflowDto createDto, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await _documentWorkflowService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentWorkflow),
                new { id = workflow.Id },
                workflow);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document workflow.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document workflow
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="updateDto">Document workflow update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document workflow</returns>
    /// <response code="200">Document workflow updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> UpdateDocumentWorkflow(Guid id, [FromBody] UpdateDocumentWorkflowDto updateDto, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await _documentWorkflowService.UpdateAsync(id, updateDto, currentUser, cancellationToken);

            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {id} was not found.");

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document workflow.", ex);
        }
    }

    /// <summary>
    /// Deletes a document workflow (soft delete)
    /// </summary>
    /// <param name="id">Document workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document workflow deleted successfully</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocumentWorkflow(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentWorkflowService.DeleteAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document workflow with ID {id} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document workflow.", ex);
        }
    }
}