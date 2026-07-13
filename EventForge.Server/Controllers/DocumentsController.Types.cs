using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Gets all document types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document types</returns>
    /// <response code="200">Returns the list of document types</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentTypes(CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var documentTypes = await documentFacade.GetAllDocumentTypesAsync(cancellationToken);
            return Ok(documentTypes);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document types.", ex);
        }
    }

    /// <summary>
    /// Gets a document type by ID
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document type information</returns>
    /// <response code="200">Returns the document type</response>
    /// <response code="404">If the document type is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("types/{id:guid}")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTypeDto>> GetDocumentType(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentType = await documentFacade.GetDocumentTypeByIdAsync(id, cancellationToken);

            if (documentType == null)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return Ok(documentType);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document type.", ex);
        }
    }

    /// <summary>
    /// Creates a new document type
    /// </summary>
    /// <param name="createDto">Document type creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document type information</returns>
    /// <response code="201">Returns the created document type</response>
    /// <response code="400">If the document type data is invalid</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("types")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTypeDto>> CreateDocumentType(
        [FromBody] CreateDocumentTypeDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var documentType = await documentFacade.CreateDocumentTypeAsync(createDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetDocumentType), new { id = documentType.Id }, documentType);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document type.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document type
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="updateDto">Document type update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document type information</returns>
    /// <response code="200">Returns the updated document type</response>
    /// <response code="400">If the document type data is invalid</response>
    /// <response code="404">If the document type is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("types/{id:guid}")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTypeDto>> UpdateDocumentType(
        Guid id,
        [FromBody] UpdateDocumentTypeDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var documentType = await documentFacade.UpdateDocumentTypeAsync(id, updateDto, GetCurrentUser(), cancellationToken);

            if (documentType == null)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return Ok(documentType);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document type.", ex);
        }
    }

    /// <summary>
    /// Deletes a document type (soft delete)
    /// </summary>
    /// <param name="id">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Document type deleted successfully</response>
    /// <response code="404">If the document type is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteDocumentType(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await documentFacade.DeleteDocumentTypeAsync(id, GetCurrentUser(), cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document type.", ex);
        }
    }

}
