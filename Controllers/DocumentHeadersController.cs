using EventForge.DTOs.Audit;
using Microsoft.AspNetCore.Authorization;
using EventForge.DTOs.Documents;
using EventForge.Services.Documents;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for document header management.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentHeadersController : BaseApiController
{
    private readonly IDocumentHeaderService _documentHeaderService;

    public DocumentHeadersController(IDocumentHeaderService documentHeaderService)
    {
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
    }

    /// <summary>
    /// Gets paginated document headers with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated document headers</returns>
    /// <response code="200">Returns the paginated document headers</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DocumentHeaderDto>>> GetDocumentHeaders(
        [FromQuery] DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving document headers.", error = ex.Message });
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
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentHeaderDto>> GetDocumentHeader(
        Guid id,
        [FromQuery] bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);
            
            if (documentHeader == null)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the document header.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets document headers by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document headers for the business party</returns>
    /// <response code="200">Returns the document headers</response>
    [HttpGet("business-party/{businessPartyId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentHeaderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentHeaderDto>>> GetDocumentHeadersByBusinessParty(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeaders = await _documentHeaderService.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
            return Ok(documentHeaders);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving document headers.", error = ex.Message });
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
    [HttpPost]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentHeaderDto>> CreateDocumentHeader(
        [FromBody] CreateDocumentHeaderDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser(); // Assuming BaseApiController has this method
            var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetDocumentHeader),
                new { id = documentHeader.Id },
                documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the document header.", error = ex.Message });
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
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentHeaderDto>> UpdateDocumentHeader(
        Guid id,
        [FromBody] UpdateDocumentHeaderDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.UpdateDocumentHeaderAsync(id, updateDto, currentUser, cancellationToken);
            
            if (documentHeader == null)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the document header.", error = ex.Message });
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocumentHeader(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentHeaderService.DeleteDocumentHeaderAsync(id, currentUser, cancellationToken);
            
            if (!deleted)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the document header.", error = ex.Message });
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
    [HttpPost("{id:guid}/calculate-totals")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentHeaderDto>> CalculateDocumentTotals(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documentHeader = await _documentHeaderService.CalculateDocumentTotalsAsync(id, cancellationToken);
            
            if (documentHeader == null)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while calculating document totals.", error = ex.Message });
        }
    }

    /// <summary>
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approved document header</returns>
    /// <response code="200">Returns the approved document header</response>
    /// <response code="404">If the document header is not found</response>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentHeaderDto>> ApproveDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.ApproveDocumentAsync(id, currentUser, cancellationToken);
            
            if (documentHeader == null)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while approving the document.", error = ex.Message });
        }
    }

    /// <summary>
    /// Closes a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Closed document header</returns>
    /// <response code="200">Returns the closed document header</response>
    /// <response code="404">If the document header is not found</response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentHeaderDto>> CloseDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);
            
            if (documentHeader == null)
                return NotFound(new { message = $"Document header with ID {id} not found." });

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while closing the document.", error = ex.Message });
        }
    }

    /// <summary>
    /// Checks if a document header exists.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    [HttpHead("{id:guid}")]
    [HttpGet("{id:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> DocumentHeaderExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _documentHeaderService.DocumentHeaderExistsAsync(id, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while checking document header existence.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current user from the request context.
    /// This overrides the base implementation with specific logic for document headers.
    /// </summary>
    /// <returns>Current user identifier</returns>
    private new string GetCurrentUser()
    {
        // TODO: Implement proper user context retrieval from authentication
        return User?.Identity?.Name ?? "system";
    }
}