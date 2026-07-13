using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{
    /// <summary>
    /// Gets all document workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document workflows</returns>
    /// <response code="200">Returns the list of document workflows</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("workflows")]
    [ProducesResponseType(typeof(IEnumerable<DocumentWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentWorkflowDto>>> GetWorkflows(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflows = await documentFacade.GetWorkflowsAsync(null, cancellationToken);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document workflows.", ex);
        }
    }

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
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflows = await documentFacade.GetWorkflowsAsync(documentTypeId, cancellationToken);
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
    /// <param name="workflowId">Document workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document workflow details</returns>
    /// <response code="200">Returns the document workflow</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("workflows/{workflowId:guid}")]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> GetWorkflow(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflow = await documentFacade.GetWorkflowByIdAsync(workflowId, cancellationToken);
            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

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
    [HttpPost("workflows")]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> CreateWorkflow([FromBody] CreateDocumentWorkflowDto createDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await documentFacade.CreateWorkflowAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { workflowId = workflow.Id },
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
    /// <param name="workflowId">Document workflow ID</param>
    /// <param name="updateDto">Document workflow update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document workflow</returns>
    /// <response code="200">Document workflow updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("workflows/{workflowId:guid}")]
    [ProducesResponseType(typeof(DocumentWorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentWorkflowDto>> UpdateWorkflow(Guid workflowId, [FromBody] UpdateDocumentWorkflowDto updateDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await documentFacade.UpdateWorkflowAsync(workflowId, updateDto, currentUser, cancellationToken);

            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

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
    /// <param name="workflowId">Document workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document workflow deleted successfully</response>
    /// <response code="404">If the document workflow is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("workflows/{workflowId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteWorkflow(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await documentFacade.DeleteWorkflowAsync(workflowId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document workflow.", ex);
        }
    }

    // Analytics endpoints
}
