using EventForge.DTOs.Documents;
using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document header management with multi-tenant support.
/// Provides comprehensive CRUD operations for document headers within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[RequireLicenseFeature("BasicReporting")]
public class DocumentHeadersController : BaseApiController
{
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly ITenantContext _tenantContext;
    private readonly IExportService _exportService;
    private readonly ILogger<DocumentHeadersController> _logger;

    public DocumentHeadersController(
        IDocumentHeaderService documentHeaderService,
        ITenantContext tenantContext,
        IExportService exportService,
        ILogger<DocumentHeadersController> logger)
    {
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
    public async Task<ActionResult<PagedResult<DocumentHeaderDto>>> GetDocumentHeaders(
        [FromQuery] DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
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
    public async Task<ActionResult<DocumentHeaderDto>> GetDocumentHeader(
        Guid id,
        [FromQuery] bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeader = await _documentHeaderService.GetDocumentHeaderByIdAsync(id, includeRows, cancellationToken);

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
    public async Task<ActionResult<IEnumerable<DocumentHeaderDto>>> GetDocumentHeadersByBusinessParty(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeaders = await _documentHeaderService.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
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
    public async Task<ActionResult<DocumentHeaderDto>> CreateDocumentHeader(
        [FromBody] CreateDocumentHeaderDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentHeader),
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
    public async Task<ActionResult<DocumentHeaderDto>> UpdateDocumentHeader(
        Guid id,
        [FromBody] UpdateDocumentHeaderDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.UpdateDocumentHeaderAsync(id, updateDto, currentUser, cancellationToken);

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
    public async Task<IActionResult> DeleteDocumentHeader(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentHeaderService.DeleteDocumentHeaderAsync(id, currentUser, cancellationToken);

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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeader = await _documentHeaderService.CalculateDocumentTotalsAsync(id, cancellationToken);

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
    /// Approves a document header.
    /// </summary>
    /// <param name="id">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approved document header</returns>
    /// <response code="200">Returns the approved document header</response>
    /// <response code="404">If the document header is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> ApproveDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.ApproveDocumentAsync(id, currentUser, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while approving the document.", ex);
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
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentHeaderDto>> CloseDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.CloseDocumentAsync(id, currentUser, cancellationToken);

            if (documentHeader == null)
                return CreateNotFoundProblem($"Document header with ID {id} not found.");

            return Ok(documentHeader);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while closing the document.", ex);
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
    public async Task<ActionResult<bool>> DocumentHeaderExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _documentHeaderService.DocumentHeaderExistsAsync(id, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking document header existence.", ex);
        }
    }

    /// <summary>
    /// Export all documents to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportDocuments(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Export operation started by {User} for Documents (format: {Format})",
            User.Identity?.Name ?? "Unknown", format);

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await _documentHeaderService.GetDocumentsForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await _exportService.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Documents_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await _exportService.ExportToExcelAsync(data, "Documents", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Documents_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        _logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }
}