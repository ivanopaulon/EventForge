using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated document headers</returns>
    /// <response code="200">Returns the paginated document headers</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<DocumentHeaderDto>>> GetDocuments(
        [FromQuery] DocumentHeaderQueryParameters queryParameters,
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
            var result = await documentFacade.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document headers.", ex);
        }
    }

    /// <summary>
    /// Gets a document header by ID.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="includeRows">Include document rows in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header details</returns>
    /// <response code="200">Returns the document header</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> GetDocument(
        Guid id,
        [FromQuery] bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeader = await documentFacade.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document header.", ex);
        }
    }

    /// <summary>
    /// Gets document headers by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document headers for the business party</returns>
    /// <response code="200">Returns the document headers</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("business-party/{businessPartyId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentHeaderDto>>> GetDocumentsByBusinessParty(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeaders = await documentFacade.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
            return Ok(documentHeaders);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document headers.", ex);
        }
    }

    /// <summary>
    /// Creates a new document header.
    /// </summary>
    /// <param name="createDto">Document header creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document header</returns>
    /// <response code="201">Returns the created document header</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> CreateDocument(
        [FromBody] CreateDocumentHeaderDto createDto,
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
            var documentHeader = await documentFacade.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = documentHeader.Id },
                documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document header.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="updateDto">Document header update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header</returns>
    /// <response code="200">Returns the updated document header</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> UpdateDocument(
        Guid id,
        [FromBody] UpdateDocumentHeaderDto updateDto,
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
            var documentHeader = await documentFacade.UpdateDocumentHeaderAsync(id, updateDto, currentUser, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document header.", ex);
        }
    }

    /// <summary>
    /// Deletes a document header (soft delete).
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the document header was deleted successfully</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await documentFacade.DeleteDocumentHeaderAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document header.", ex);
        }
    }

    /// <summary>
    /// Calculates document totals for a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document header with updated totals</returns>
    /// <response code="200">Returns the document header with calculated totals</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/calculate-totals")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> CalculateDocumentTotals(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeader = await documentFacade.CalculateDocumentTotalsAsync(id, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating document totals.", ex);
        }
    }

    /// <summary>
    /// Archives a document header (Active → Archived).
    /// Archived documents are excluded from default list views but their stock movements remain unchanged.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document header DTO</returns>
    /// <response code="200">Returns the archived document header</response>
    /// <response code="400">If the document is not in Active status</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> ArchiveDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await documentFacade.ArchiveDocumentAsync(id, currentUser, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while archiving the document.", ex);
        }
    }

    /// <summary>
    /// Checks if a document header exists.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("{id:guid}")]
    [HttpGet("{id:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> DocumentExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await documentFacade.DocumentHeaderExistsAsync(id, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking document header existence.", ex);
        }
    }

}
