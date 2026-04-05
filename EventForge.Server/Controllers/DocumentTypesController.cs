using EventForge.DTOs.Documents;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document type management with multi-tenant support.
/// Provides CRUD operations for document types within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentTypesController(
    IDocumentTypeService documentTypeService,
    ITenantContext tenantContext,
    ICacheInvalidationService cacheInvalidation) : BaseApiController
{
    /// <summary>
    /// Gets all document types
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document types</returns>
    /// <response code="200">Returns the list of document types</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [OutputCache(PolicyName = "StaticEntities")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentTypes(CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var documentTypes = await documentTypeService.GetAllAsync(cancellationToken);
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
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTypeDto>> GetDocumentType(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentType = await documentTypeService.GetByIdAsync(id, cancellationToken);

            if (documentType is null)
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
    [HttpPost]
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
            var documentType = await documentTypeService.CreateAsync(createDto, GetCurrentUser(), cancellationToken);
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
    [HttpPut("{id:guid}")]
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
            var documentType = await documentTypeService.UpdateAsync(id, updateDto, GetCurrentUser(), cancellationToken);

            if (documentType is null)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            await cacheInvalidation.InvalidateStaticEntitiesAsync(cancellationToken);
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteDocumentType(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await documentTypeService.DeleteAsync(id, GetCurrentUser(), cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            await cacheInvalidation.InvalidateStaticEntitiesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document type.", ex);
        }
    }
}