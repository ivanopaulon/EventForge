using EventForge.DTOs.Documents;
using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Unified REST API controller for document-related operations with multi-tenant support.
/// Provides aggregated access to document attachments, comments, templates, workflows, and analytics.
/// Delegates business logic to existing specialized services through the DocumentFacade and individual services.
/// </summary>
[Route("api/v1/documents")]
[Authorize]
[RequireLicenseFeature("BasicReporting")]
public class DocumentsController : BaseApiController
{
    private readonly IDocumentFacade _documentFacade;
    private readonly ITenantContext _tenantContext;
    private readonly IDocumentTemplateService _templateService;
    private readonly IDocumentCommentService _commentService;
    private readonly IDocumentWorkflowService _workflowService;
    private readonly IDocumentAnalyticsService _analyticsService;
    private readonly IDocumentAttachmentService _attachmentService;
    private readonly IDocumentHeaderService _documentHeaderService;
    private readonly IDocumentTypeService _documentTypeService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentFacade documentFacade,
        ITenantContext tenantContext,
        IDocumentTemplateService templateService,
        IDocumentCommentService commentService,
        IDocumentWorkflowService workflowService,
        IDocumentAnalyticsService analyticsService,
        IDocumentAttachmentService attachmentService,
        IDocumentHeaderService documentHeaderService,
        IDocumentTypeService documentTypeService,
        ILogger<DocumentsController> logger)
    {
        _documentFacade = documentFacade ?? throw new ArgumentNullException(nameof(documentFacade));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));
        _documentHeaderService = documentHeaderService ?? throw new ArgumentNullException(nameof(documentHeaderService));
        _documentTypeService = documentTypeService ?? throw new ArgumentNullException(nameof(documentTypeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Document Headers

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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _documentHeaderService.GetPagedDocumentHeadersAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document headers.");
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
            _logger.LogError(ex, "An error occurred while retrieving the document header.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var documentHeaders = await _documentHeaderService.GetDocumentHeadersByBusinessPartyAsync(businessPartyId, cancellationToken);
            return Ok(documentHeaders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document headers.");
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

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var documentHeader = await _documentHeaderService.CreateDocumentHeaderAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = documentHeader.Id },
                documentHeader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document header.");
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
            _logger.LogError(ex, "An error occurred while updating the document header.");
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
            _logger.LogError(ex, "An error occurred while deleting the document header.");
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
            _logger.LogError(ex, "An error occurred while calculating document totals.");
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
            _logger.LogError(ex, "An error occurred while approving the document.");
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
            _logger.LogError(ex, "An error occurred while closing the document.");
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
    public async Task<ActionResult<bool>> DocumentExists(
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
            _logger.LogError(ex, "An error occurred while checking document header existence.");
            return CreateInternalServerErrorProblem("An error occurred while checking document header existence.", ex);
        }
    }

    #endregion

    #region Document Rows

    /// <summary>
    /// Adds a row to a document.
    /// </summary>
    /// <param name="createRowDto">Document row creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document row</returns>
    /// <response code="201">Returns the created document row</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("rows")]
    [ProducesResponseType(typeof(DocumentRowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentRowDto>> AddDocumentRow(
        [FromBody] CreateDocumentRowDto createRowDto,
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
            var documentRow = await _documentHeaderService.AddDocumentRowAsync(createRowDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(AddDocumentRow), new { id = documentRow.Id }, documentRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding document row.");
            return CreateInternalServerErrorProblem("An error occurred while adding document row.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="updateRowDto">Document row update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document row</returns>
    /// <response code="200">Returns the updated document row</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the document row is not found</response>
    [HttpPut("rows/{rowId:guid}")]
    [ProducesResponseType(typeof(DocumentRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRowDto>> UpdateDocumentRow(
        Guid rowId,
        [FromBody] UpdateDocumentRowDto updateRowDto,
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
            var documentRow = await _documentHeaderService.UpdateDocumentRowAsync(rowId, updateRowDto, currentUser, cancellationToken);
            
            if (documentRow == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Document row not found",
                    Detail = $"Document row with ID {rowId} was not found."
                });
            }

            return Ok(documentRow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating document row {RowId}.", rowId);
            return CreateInternalServerErrorProblem($"An error occurred while updating document row {rowId}.", ex);
        }
    }

    /// <summary>
    /// Deletes a document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the document row was successfully deleted</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the document row is not found</response>
    [HttpDelete("rows/{rowId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocumentRow(
        Guid rowId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _documentHeaderService.DeleteDocumentRowAsync(rowId, cancellationToken);
            
            if (!success)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Document row not found",
                    Detail = $"Document row with ID {rowId} was not found."
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting document row {RowId}.", rowId);
            return CreateInternalServerErrorProblem($"An error occurred while deleting document row {rowId}.", ex);
        }
    }

    #endregion

    // Attachment endpoints
    /// <summary>
    /// Gets all attachments for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentAttachments(
        Guid documentId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _documentFacade.GetAttachmentsAsync(documentId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document attachments.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets attachments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeHistory">Include all versions or only current</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document attachments</returns>
    /// <response code="200">Returns the document attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetDocumentRowAttachments(
        Guid documentRowId,
        [FromQuery] bool includeHistory = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _attachmentService.GetDocumentRowAttachmentsAsync(documentRowId, includeHistory, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document attachments.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document attachments.", ex);
        }
    }

    /// <summary>
    /// Gets an attachment by ID.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document attachment details</returns>
    /// <response code="200">Returns the document attachment</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> GetAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachment = await _attachmentService.GetAttachmentByIdAsync(attachmentId, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document attachment.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new attachment for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created attachment</returns>
    /// <response code="201">Returns the created attachment</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/attachments")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateDocumentAttachment(
        Guid documentId,
        [FromBody] CreateDocumentAttachmentDto createDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await _documentFacade.CreateAttachmentAsync(documentId, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetAttachment), new { attachmentId = attachment.Id }, attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document attachment.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new attachment using generic creation endpoint.
    /// </summary>
    /// <param name="createDto">Attachment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document attachment</returns>
    /// <response code="201">Returns the created document attachment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateAttachment(
        [FromBody] CreateDocumentAttachmentDto createDto,
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
            var attachment = await _attachmentService.CreateAttachmentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetAttachment),
                new { attachmentId = attachment.Id },
                attachment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document attachment.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document attachment metadata.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="updateDto">Attachment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document attachment</returns>
    /// <response code="200">Returns the updated document attachment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> UpdateAttachment(
        Guid attachmentId,
        [FromBody] UpdateDocumentAttachmentDto updateDto,
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
            var attachment = await _attachmentService.UpdateAttachmentAsync(attachmentId, updateDto, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document attachment.");
            return CreateInternalServerErrorProblem("An error occurred while updating the document attachment.", ex);
        }
    }

    /// <summary>
    /// Creates a new version of an existing attachment.
    /// </summary>
    /// <param name="attachmentId">Original attachment ID</param>
    /// <param name="versionDto">New version data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New version document attachment</returns>
    /// <response code="201">Returns the new version document attachment</response>
    /// <response code="400">If the version data is invalid</response>
    /// <response code="404">If the original attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments/{attachmentId:guid}/versions")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> CreateAttachmentVersion(
        Guid attachmentId,
        [FromBody] AttachmentVersionDto versionDto,
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
            var newVersion = await _attachmentService.CreateAttachmentVersionAsync(attachmentId, versionDto, currentUser, cancellationToken);

            if (newVersion == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return CreatedAtAction(
                nameof(GetAttachment),
                new { attachmentId = newVersion.Id },
                newVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the attachment version.");
            return CreateInternalServerErrorProblem("An error occurred while creating the attachment version.", ex);
        }
    }

    /// <summary>
    /// Gets attachment version history.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachment versions</returns>
    /// <response code="200">Returns the attachment versions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/{attachmentId:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentVersions(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var versions = await _attachmentService.GetAttachmentVersionsAsync(attachmentId, cancellationToken);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving attachment versions.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving attachment versions.", ex);
        }
    }

    /// <summary>
    /// Signs an attachment digitally.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="signatureInfo">Digital signature information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signed document attachment</returns>
    /// <response code="200">Returns the signed document attachment</response>
    /// <response code="400">If the signature data is invalid</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("attachments/{attachmentId:guid}/sign")]
    [ProducesResponseType(typeof(DocumentAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAttachmentDto>> SignAttachment(
        Guid attachmentId,
        [FromBody] string signatureInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(signatureInfo))
        {
            return CreateValidationProblemDetails("Signature information is required.");
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var attachment = await _attachmentService.SignAttachmentAsync(attachmentId, signatureInfo, currentUser, cancellationToken);

            if (attachment == null)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return Ok(attachment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while signing the attachment.");
            return CreateInternalServerErrorProblem("An error occurred while signing the attachment.", ex);
        }
    }

    /// <summary>
    /// Gets attachments by category.
    /// </summary>
    /// <param name="category">Attachment category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of attachments in the category</returns>
    /// <response code="200">Returns the attachments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("attachments/category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachmentsByCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var attachments = await _attachmentService.GetAttachmentsByCategoryAsync(category, cancellationToken);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving attachments by category.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving attachments by category.", ex);
        }
    }

    /// <summary>
    /// Deletes a document attachment (soft delete).
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the attachment was deleted successfully</response>
    /// <response code="404">If the attachment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("attachments/{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAttachment(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _attachmentService.DeleteAttachmentAsync(attachmentId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document attachment with ID {attachmentId} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document attachment.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document attachment.", ex);
        }
    }

    /// <summary>
    /// Checks if a document attachment exists.
    /// </summary>
    /// <param name="attachmentId">Attachment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("attachments/{attachmentId:guid}")]
    [HttpGet("attachments/{attachmentId:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> AttachmentExists(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _attachmentService.AttachmentExistsAsync(attachmentId, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking attachment existence.");
            return CreateInternalServerErrorProblem("An error occurred while checking attachment existence.", ex);
        }
    }

    // Comment endpoints
    /// <summary>
    /// Gets all comments for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/comments")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentComments(
        Guid documentId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await _documentFacade.GetCommentsAsync(documentId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document comments.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets comments for a document row.
    /// </summary>
    /// <param name="documentRowId">Document row ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of document comments</returns>
    /// <response code="200">Returns the document comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/document-row/{documentRowId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetDocumentRowComments(
        Guid documentRowId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comments = await _commentService.GetDocumentRowCommentsAsync(documentRowId, includeReplies, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document comments.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document comments.", ex);
        }
    }

    /// <summary>
    /// Gets a comment by ID.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="includeReplies">Include threaded replies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document comment details</returns>
    /// <response code="200">Returns the document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> GetComment(
        Guid commentId,
        [FromQuery] bool includeReplies = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var comment = await _commentService.GetCommentByIdAsync(commentId, includeReplies, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document comment.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document comment.", ex);
        }
    }

    /// <summary>
    /// Creates a new comment for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created comment</returns>
    /// <response code="201">Returns the created comment</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/comments")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> CreateDocumentComment(
        Guid documentId,
        [FromBody] CreateDocumentCommentDto createDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _documentFacade.CreateCommentAsync(documentId, createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetComment), new { commentId = comment.Id }, comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document comment.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document comment.", ex);
        }
    }

    /// <summary>
    /// Creates a new comment using generic creation endpoint.
    /// </summary>
    /// <param name="createDto">Comment creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document comment</returns>
    /// <response code="201">Returns the created document comment</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> CreateComment(
        [FromBody] CreateDocumentCommentDto createDto,
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
            var comment = await _commentService.CreateCommentAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetComment),
                new { commentId = comment.Id },
                comment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document comment.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document comment.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="updateDto">Comment update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document comment</returns>
    /// <response code="200">Returns the updated document comment</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("comments/{commentId:guid}")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> UpdateComment(
        Guid commentId,
        [FromBody] UpdateDocumentCommentDto updateDto,
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
            var comment = await _commentService.UpdateCommentAsync(commentId, updateDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document comment.");
            return CreateInternalServerErrorProblem("An error occurred while updating the document comment.", ex);
        }
    }

    /// <summary>
    /// Resolves a document comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="resolveDto">Resolution data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved document comment</returns>
    /// <response code="200">Returns the resolved document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments/{commentId:guid}/resolve")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ResolveComment(
        Guid commentId,
        [FromBody] ResolveCommentDto resolveDto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.ResolveCommentAsync(commentId, resolveDto, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resolving the comment.");
            return CreateInternalServerErrorProblem("An error occurred while resolving the comment.", ex);
        }
    }

    /// <summary>
    /// Reopens a resolved comment.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reopened document comment</returns>
    /// <response code="200">Returns the reopened document comment</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("comments/{commentId:guid}/reopen")]
    [ProducesResponseType(typeof(DocumentCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentDto>> ReopenComment(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comment = await _commentService.ReopenCommentAsync(commentId, currentUser, cancellationToken);

            if (comment == null)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reopening the comment.");
            return CreateInternalServerErrorProblem("An error occurred while reopening the comment.", ex);
        }
    }

    /// <summary>
    /// Gets comment statistics for a document header.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comment statistics</returns>
    /// <response code="200">Returns the comment statistics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/comments/stats")]
    [ProducesResponseType(typeof(DocumentCommentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentCommentStatsDto>> GetDocumentCommentStats(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var stats = await _commentService.GetDocumentCommentStatsAsync(documentId, currentUser, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving comment statistics.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving comment statistics.", ex);
        }
    }

    /// <summary>
    /// Gets comments assigned to the current user.
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of assigned comments</returns>
    /// <response code="200">Returns the assigned comments</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("comments/assigned")]
    [ProducesResponseType(typeof(IEnumerable<DocumentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetAssignedComments(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var comments = await _commentService.GetAssignedCommentsAsync(currentUser, status, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving assigned comments.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving assigned comments.", ex);
        }
    }

    /// <summary>
    /// Deletes a document comment (soft delete).
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">If the comment was deleted successfully</response>
    /// <response code="404">If the comment is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _commentService.DeleteCommentAsync(commentId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document comment with ID {commentId} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document comment.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document comment.", ex);
        }
    }

    /// <summary>
    /// Checks if a document comment exists.
    /// </summary>
    /// <param name="commentId">Comment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    /// <response code="200">Returns existence status</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpHead("comments/{commentId:guid}")]
    [HttpGet("comments/{commentId:guid}/exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<bool>> CommentExists(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var exists = await _commentService.CommentExistsAsync(commentId, cancellationToken);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking comment existence.");
            return CreateInternalServerErrorProblem("An error occurred while checking comment existence.", ex);
        }
    }

    // Template endpoints
    /// <summary>
    /// Gets all document templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplates(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await _templateService.GetAllAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document templates.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates.", ex);
        }
    }

    /// <summary>
    /// Gets public document templates available to all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of public document templates</returns>
    /// <response code="200">Returns the public document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/public")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetPublicTemplates(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await _documentFacade.GetPublicTemplatesAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving public document templates.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving public document templates.", ex);
        }
    }

    /// <summary>
    /// Gets document templates by document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates for the specified document type</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates/by-document-type/{documentTypeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplatesByDocumentType(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await _templateService.GetByDocumentTypeAsync(documentTypeId, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document templates by document type.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates by document type.", ex);
        }
    }

    /// <summary>
    /// Gets document templates by category
    /// </summary>
    /// <param name="category">Template category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates for the specified category</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="400">If the category is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates/by-category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplatesByCategory(string category, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (string.IsNullOrWhiteSpace(category))
            return CreateValidationProblemDetails("Category cannot be empty.");

        try
        {
            var templates = await _templateService.GetByCategoryAsync(category, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document templates by category.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates by category.", ex);
        }
    }

    /// <summary>
    /// Gets a document template by ID.
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template</returns>
    /// <response code="200">Returns the document template</response>
    /// <response code="404">If the template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/{templateId:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentTemplateDto>> GetTemplate(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var template = await _documentFacade.GetTemplateByIdAsync(templateId, cancellationToken);

            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {templateId} not found.");

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document template.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document template.", ex);
        }
    }

    /// <summary>
    /// Creates a new document template
    /// </summary>
    /// <param name="createDto">Document template creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document template</returns>
    /// <response code="201">Document template created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> CreateTemplate([FromBody] CreateDocumentTemplateDto createDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await _templateService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { templateId = template.Id },
                template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document template.");
            return CreateInternalServerErrorProblem("An error occurred while creating the document template.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document template
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="updateDto">Document template update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document template</returns>
    /// <response code="200">Document template updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("templates/{templateId:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> UpdateTemplate(Guid templateId, [FromBody] UpdateDocumentTemplateDto updateDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await _templateService.UpdateAsync(templateId, updateDto, currentUser, cancellationToken);

            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document template.");
            return CreateInternalServerErrorProblem("An error occurred while updating the document template.", ex);
        }
    }

    /// <summary>
    /// Deletes a document template (soft delete)
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document template deleted successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("templates/{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTemplate(Guid templateId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _templateService.DeleteAsync(templateId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document template.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document template.", ex);
        }
    }

    /// <summary>
    /// Updates template usage statistics
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Usage updated successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("templates/{templateId:guid}/usage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTemplateUsage(Guid templateId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var updated = await _templateService.UpdateUsageAsync(templateId, currentUser, cancellationToken);

            if (!updated)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating template usage.");
            return CreateInternalServerErrorProblem("An error occurred while updating template usage.", ex);
        }
    }

    // Workflow endpoints
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflows = await _workflowService.GetAllAsync(cancellationToken);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document workflows.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflows = await _documentFacade.GetWorkflowsAsync(documentTypeId, cancellationToken);
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document workflows.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var workflow = await _workflowService.GetByIdAsync(workflowId, cancellationToken);
            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document workflow.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await _workflowService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { workflowId = workflow.Id },
                workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document workflow.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var workflow = await _workflowService.UpdateAsync(workflowId, updateDto, currentUser, cancellationToken);

            if (workflow == null)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document workflow.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _workflowService.DeleteAsync(workflowId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document workflow with ID {workflowId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document workflow.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document workflow.", ex);
        }
    }

    // Analytics endpoints
    /// <summary>
    /// Gets analytics for a specific document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document analytics</returns>
    /// <response code="200">Returns the document analytics</response>
    /// <response code="404">If analytics are not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{documentId:guid}/analytics")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> GetDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var analytics = await _documentFacade.GetAnalyticsAsync(documentId, cancellationToken);

            if (analytics == null)
                return CreateNotFoundProblem($"Analytics for document with ID {documentId} not found.");

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document analytics.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving document analytics.", ex);
        }
    }

    /// <summary>
    /// Gets analytics summary with grouping and filtering.
    /// </summary>
    /// <param name="from">Start date filter (optional)</param>
    /// <param name="to">End date filter (optional)</param>
    /// <param name="groupBy">Group by option: time, documentType, department</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics summary</returns>
    /// <response code="200">Returns the analytics summary</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("analytics/summary")]
    [ProducesResponseType(typeof(DocumentAnalyticsSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsSummaryDto>> GetAnalyticsSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? groupBy = "documentType",
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        // Validate groupBy parameter
        if (!string.IsNullOrEmpty(groupBy) &&
            !new[] { "time", "documentType", "department" }.Contains(groupBy, StringComparer.OrdinalIgnoreCase))
        {
            return CreateValidationProblemDetails("Group by must be one of: time, documentType, department");
        }

        // Validate date range
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return CreateValidationProblemDetails("Start date cannot be after end date");
        }

        try
        {
            var summary = await _analyticsService.GetAnalyticsSummaryAsync(from, to, groupBy, cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving analytics summary.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving analytics summary.", ex);
        }
    }

    /// <summary>
    /// Gets KPI summary for documents in date range.
    /// </summary>
    /// <param name="from">Start date (required)</param>
    /// <param name="to">End date (required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>KPI summary</returns>
    /// <response code="200">Returns the KPI summary</response>
    /// <response code="400">If the date parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("analytics/kpi")]
    [ProducesResponseType(typeof(DocumentKpiSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentKpiSummaryDto>> GetKpiSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        // Validate date range
        if (from > to)
        {
            return CreateValidationProblemDetails("Start date cannot be after end date");
        }

        if (from == default || to == default)
        {
            return CreateValidationProblemDetails("Both start and end dates are required");
        }

        // Limit date range to prevent performance issues
        if ((to - from).TotalDays > 365)
        {
            return CreateValidationProblemDetails("Date range cannot exceed 365 days");
        }

        try
        {
            var kpiSummary = await _analyticsService.CalculateKpiSummaryAsync(from, to, cancellationToken);
            return Ok(kpiSummary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while calculating KPI summary.");
            return CreateInternalServerErrorProblem("An error occurred while calculating KPI summary.", ex);
        }
    }

    /// <summary>
    /// Refreshes (creates or updates) analytics for a document.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document analytics</returns>
    /// <response code="200">Returns the updated document analytics</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/analytics/refresh")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> RefreshDocumentAnalytics(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await _documentFacade.RefreshAnalyticsAsync(documentId, currentUser, cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while refreshing document analytics.");
            return CreateInternalServerErrorProblem("An error occurred while refreshing document analytics.", ex);
        }
    }

    /// <summary>
    /// Handles workflow events for analytics tracking.
    /// </summary>
    /// <param name="documentId">Document header ID</param>
    /// <param name="eventType">Workflow event type</param>
    /// <param name="eventData">Additional event data (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated analytics</returns>
    /// <response code="200">Returns the updated analytics</response>
    /// <response code="400">If the event type is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{documentId:guid}/analytics/events")]
    [ProducesResponseType(typeof(DocumentAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentAnalyticsDto>> HandleWorkflowEvent(
        Guid documentId,
        [FromQuery] string eventType,
        [FromBody] object? eventData = null,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return CreateValidationProblemDetails("Event type is required");
        }

        try
        {
            var currentUser = GetCurrentUser();
            var analytics = await _analyticsService.HandleWorkflowEventAsync(
                documentId, eventType, eventData, currentUser, cancellationToken);

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while handling workflow event.");
            return CreateInternalServerErrorProblem("An error occurred while handling workflow event.", ex);
        }
    }

    #region Document Types

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
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var documentTypes = await _documentTypeService.GetAllAsync(cancellationToken);
            return Ok(documentTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving document types.");
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
            var documentType = await _documentTypeService.GetByIdAsync(id, cancellationToken);

            if (documentType == null)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return Ok(documentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the document type.");
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
            var documentType = await _documentTypeService.CreateAsync(createDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetDocumentType), new { id = documentType.Id }, documentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the document type.");
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
            var documentType = await _documentTypeService.UpdateAsync(id, updateDto, GetCurrentUser(), cancellationToken);

            if (documentType == null)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return Ok(documentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the document type.");
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
            var deleted = await _documentTypeService.DeleteAsync(id, GetCurrentUser(), cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Document type with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the document type.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the document type.", ex);
        }
    }

    #endregion

    #region Document Export

    /// <summary>
    /// Exports documents to various formats (PDF, Excel, HTML, CSV, JSON).
    /// Supports filtering by date range, document type, and status.
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation result with download information</returns>
    /// <response code="200">Export operation initiated successfully</response>
    /// <response code="400">Invalid export parameters</response>
    /// <response code="403">User doesn't have permission to export documents</response>
    [HttpPost("export")]
    [ProducesResponseType(typeof(DocumentExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentExportResultDto>> ExportDocumentsAsync(
        [FromBody] DocumentExportRequestDto request,
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
            var currentUser = User.Identity?.Name ?? "Unknown";
            var exportService = HttpContext.RequestServices.GetRequiredService<IDocumentExportService>();

            var result = await exportService.ExportDocumentsAsync(request, currentUser, cancellationToken);

            _logger.LogInformation(
                "Document export {ExportId} initiated by {User} with format {Format}",
                result.ExportId, currentUser, request.Format);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid export parameters");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while exporting documents");
            return CreateInternalServerErrorProblem("An error occurred while exporting documents.", ex);
        }
    }

    /// <summary>
    /// Gets the status of a document export operation.
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation status</returns>
    /// <response code="200">Export status retrieved successfully</response>
    /// <response code="404">Export operation not found</response>
    [HttpGet("export/{exportId:guid}/status")]
    [ProducesResponseType(typeof(DocumentExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentExportResultDto>> GetExportStatusAsync(
        [FromRoute] Guid exportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exportService = HttpContext.RequestServices.GetRequiredService<IDocumentExportService>();
            var result = await exportService.GetExportStatusAsync(exportId, cancellationToken);

            if (result == null)
            {
                return CreateNotFoundProblem($"Export operation {exportId} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving export status for {ExportId}", exportId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving export status.", ex);
        }
    }

    #endregion
}