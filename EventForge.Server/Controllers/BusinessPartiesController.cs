using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for business party and business party accounting management with multi-tenant support.
/// Provides comprehensive CRUD operations for business parties within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[RequireLicenseFeature("BasicReporting")]
public class BusinessPartiesController : BaseApiController
{
    private readonly IBusinessPartyService _businessPartyService;
    private readonly ISupplierProductBulkService _supplierProductBulkService;
    private readonly ISupplierProductCsvImportService _csvImportService;
    private readonly ITenantContext _tenantContext;
    private readonly IExportService _exportService;
    private readonly ILogger<BusinessPartiesController> _logger;

    public BusinessPartiesController(
        IBusinessPartyService businessPartyService,
        ISupplierProductBulkService supplierProductBulkService,
        ISupplierProductCsvImportService csvImportService,
        ITenantContext tenantContext,
        IExportService exportService,
        ILogger<BusinessPartiesController> logger)
    {
        _businessPartyService = businessPartyService ?? throw new ArgumentNullException(nameof(businessPartyService));
        _supplierProductBulkService = supplierProductBulkService ?? throw new ArgumentNullException(nameof(supplierProductBulkService));
        _csvImportService = csvImportService ?? throw new ArgumentNullException(nameof(csvImportService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region BusinessParty Endpoints

    /// <summary>
    /// Retrieves all business parties with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business parties</returns>
    /// <response code="200">Successfully retrieved business parties with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyDto>>> GetBusinessParties(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyService.GetBusinessPartiesAsync(pagination, cancellationToken);
            
            // Add pagination metadata headers
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business parties.", ex);
        }
    }

    /// <summary>
    /// Gets a business party by ID.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party details</returns>
    /// <response code="200">Returns the business party</response>
    /// <response code="404">If the business party is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyDto>> GetBusinessParty(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var businessParty = await _businessPartyService.GetBusinessPartyByIdAsync(id, cancellationToken);

            if (businessParty == null)
            {
                return CreateNotFoundProblem($"Business party with ID {id} not found.");
            }

            return Ok(businessParty);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the business party.", ex);
        }
    }

    /// <summary>
    /// Recupera tutti i dettagli completi di un BusinessParty in una singola chiamata ottimizzata.
    /// Endpoint FASE 5: riduce N+1 queries da 6+ a 1 sola chiamata HTTP.
    /// </summary>
    /// <param name="id">BusinessParty ID</param>
    /// <param name="includeInactive">Include contatti/indirizzi inattivi (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DTO aggregato con BusinessParty, contatti, indirizzi, listini, statistiche</returns>
    /// <response code="200">Returns the complete business party details</response>
    /// <response code="404">If the business party is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}/full-detail")]
    [ProducesResponseType(typeof(BusinessPartyFullDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyFullDetailDto>> GetFullDetail(
        Guid id, 
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyService.GetFullDetailAsync(id, includeInactive, cancellationToken);
            
            if (result == null)
            {
                return CreateNotFoundProblem($"Business party with ID {id} not found.");
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business party details.", ex);
        }
    }

    /// <summary>
    /// Gets business parties by type.
    /// </summary>
    /// <param name="partyType">Business party type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business parties of the specified type</returns>
    /// <response code="200">Returns the list of business parties</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("by-type/{partyType}")]
    [ProducesResponseType(typeof(IEnumerable<BusinessPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BusinessPartyDto>>> GetBusinessPartiesByType(DTOs.Common.BusinessPartyType partyType, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var businessParties = await _businessPartyService.GetBusinessPartiesByTypeAsync(partyType, cancellationToken);
            return Ok(businessParties);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business parties by type.", ex);
        }
    }

    /// <summary>
    /// Searches business parties by name or tax code.
    /// </summary>
    /// <param name="searchTerm">Search term to match against name or tax code</param>
    /// <param name="partyType">Optional filter by business party type</param>
    /// <param name="pageSize">Maximum number of results to return (default 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching business parties</returns>
    /// <response code="200">Returns the list of matching business parties</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<BusinessPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BusinessPartyDto>>> SearchBusinessParties(
        [FromQuery] string searchTerm,
        [FromQuery] DTOs.Common.BusinessPartyType? partyType = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var businessParties = await _businessPartyService.SearchBusinessPartiesAsync(searchTerm, partyType, pageSize, cancellationToken);
            return Ok(businessParties);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while searching business parties.", ex);
        }
    }

    /// <summary>
    /// Creates a new business party.
    /// </summary>
    /// <param name="createBusinessPartyDto">Business party creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party</returns>
    /// <response code="201">Returns the newly created business party</response>
    /// <response code="400">If the business party data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyDto>> CreateBusinessParty(CreateBusinessPartyDto createBusinessPartyDto, CancellationToken cancellationToken = default)
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
            var businessParty = await _businessPartyService.CreateBusinessPartyAsync(createBusinessPartyDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetBusinessParty), new { id = businessParty.Id }, businessParty);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the business party.", ex);
        }
    }

    /// <summary>
    /// Updates an existing business party.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="updateBusinessPartyDto">Business party update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party</returns>
    /// <response code="200">Returns the updated business party</response>
    /// <response code="400">If the business party data is invalid</response>
    /// <response code="404">If the business party is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyDto>> UpdateBusinessParty(Guid id, UpdateBusinessPartyDto updateBusinessPartyDto, CancellationToken cancellationToken = default)
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
            var businessParty = await _businessPartyService.UpdateBusinessPartyAsync(id, updateBusinessPartyDto, currentUser, cancellationToken);

            if (businessParty == null)
            {
                return CreateNotFoundProblem($"Business party with ID {id} not found.");
            }

            return Ok(businessParty);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the business party.", ex);
        }
    }

    /// <summary>
    /// Deletes a business party (soft delete).
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Business party deleted successfully</response>
    /// <response code="404">If the business party is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBusinessParty(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _businessPartyService.DeleteBusinessPartyAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Business party with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the business party.", ex);
        }
    }

    #endregion

    #region BusinessPartyAccounting Endpoints

    /// <summary>
    /// Retrieves all business party accounting records with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party accounting records</returns>
    /// <response code="200">Successfully retrieved business party accounting records with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("accounting")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyAccountingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyAccountingDto>>> GetBusinessPartyAccounting(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyService.GetBusinessPartyAccountingAsync(pagination, cancellationToken);
            
            // Add pagination metadata headers
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business party accounting records.", ex);
        }
    }

    /// <summary>
    /// Gets a business party accounting record by ID.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting details</returns>
    /// <response code="200">Returns the business party accounting record</response>
    /// <response code="404">If the business party accounting record is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("accounting/{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> GetBusinessPartyAccounting(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var businessPartyAccounting = await _businessPartyService.GetBusinessPartyAccountingByIdAsync(id, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return CreateNotFoundProblem($"Business party accounting with ID {id} not found.");
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the business party accounting record.", ex);
        }
    }

    /// <summary>
    /// Gets business party accounting by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting details</returns>
    /// <response code="200">Returns the business party accounting record</response>
    /// <response code="404">If the business party accounting record is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{businessPartyId:guid}/accounting")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> GetBusinessPartyAccountingByBusinessPartyId(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var businessPartyAccounting = await _businessPartyService.GetBusinessPartyAccountingByBusinessPartyIdAsync(businessPartyId, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return CreateNotFoundProblem($"Business party accounting for business party {businessPartyId} not found.");
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the business party accounting record.", ex);
        }
    }

    /// <summary>
    /// Creates a new business party accounting record.
    /// </summary>
    /// <param name="createBusinessPartyAccountingDto">Business party accounting creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party accounting record</returns>
    /// <response code="201">Returns the newly created business party accounting record</response>
    /// <response code="400">If the business party accounting data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("accounting")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> CreateBusinessPartyAccounting(CreateBusinessPartyAccountingDto createBusinessPartyAccountingDto, CancellationToken cancellationToken = default)
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
            var businessPartyAccounting = await _businessPartyService.CreateBusinessPartyAccountingAsync(createBusinessPartyAccountingDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetBusinessPartyAccounting), new { id = businessPartyAccounting.Id }, businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the business party accounting record.", ex);
        }
    }

    /// <summary>
    /// Updates an existing business party accounting record.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="updateBusinessPartyAccountingDto">Business party accounting update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party accounting record</returns>
    /// <response code="200">Returns the updated business party accounting record</response>
    /// <response code="400">If the business party accounting data is invalid</response>
    /// <response code="404">If the business party accounting record is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("accounting/{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartyAccountingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BusinessPartyAccountingDto>> UpdateBusinessPartyAccounting(Guid id, UpdateBusinessPartyAccountingDto updateBusinessPartyAccountingDto, CancellationToken cancellationToken = default)
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
            var businessPartyAccounting = await _businessPartyService.UpdateBusinessPartyAccountingAsync(id, updateBusinessPartyAccountingDto, currentUser, cancellationToken);

            if (businessPartyAccounting == null)
            {
                return CreateNotFoundProblem($"Business party accounting with ID {id} not found.");
            }

            return Ok(businessPartyAccounting);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the business party accounting record.", ex);
        }
    }

    /// <summary>
    /// Deletes a business party accounting record (soft delete).
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Business party accounting record deleted successfully</response>
    /// <response code="404">If the business party accounting record is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("accounting/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteBusinessPartyAccounting(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _businessPartyService.DeleteBusinessPartyAccountingAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Business party accounting with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the business party accounting record.", ex);
        }
    }

    #endregion

    #region BusinessParty Documents Endpoints

    /// <summary>
    /// Retrieves documents for a specific business party with pagination
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="documentTypeId">Optional document type filter</param>
    /// <param name="searchNumber">Optional number/series search</param>
    /// <param name="approvalStatus">Optional approval status filter</param>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of document headers</returns>
    /// <response code="200">Successfully retrieved documents with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the business party is not found</response>
    [HttpGet("{businessPartyId:guid}/documents")]
    [ProducesResponseType(typeof(PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>>> GetBusinessPartyDocuments(
        Guid businessPartyId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] Guid? documentTypeId = null,
        [FromQuery] string? searchNumber = null,
        [FromQuery] DTOs.Common.ApprovalStatus? approvalStatus = null,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination = default!,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if business party exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(businessPartyId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Business party with ID {businessPartyId} not found.");
            }

            var result = await _businessPartyService.GetBusinessPartyDocumentsAsync(
                businessPartyId, fromDate, toDate, documentTypeId, searchNumber, approvalStatus, pagination, cancellationToken);

            // Add pagination metadata headers
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business party documents.", ex);
        }
    }

    /// <summary>
    /// Retrieves product analysis data for a specific business party with pagination
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="type">Filter by transaction type: 'purchase', 'sale', or null for both</param>
    /// <param name="topN">Limit results to top N by value</param>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="sortBy">Sort field (default: ValuePurchased)</param>
    /// <param name="sortDescending">Sort direction (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product analysis data</returns>
    /// <response code="200">Successfully retrieved product analysis data with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the business party is not found</response>
    [HttpGet("{businessPartyId:guid}/product-analysis")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyProductAnalysisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<BusinessPartyProductAnalysisDto>>> GetBusinessPartyProductAnalysis(
        Guid businessPartyId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? type = null,
        [FromQuery] int? topN = null,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination = default!,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if business party exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(businessPartyId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Business party with ID {businessPartyId} not found.");
            }

            var result = await _businessPartyService.GetBusinessPartyProductAnalysisAsync(
                businessPartyId, fromDate, toDate, type, topN, pagination, sortBy, sortDescending, cancellationToken);

            // Add pagination metadata headers
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving business party product analysis.", ex);
        }
    }

    #endregion

    #region Supplier Product Bulk Operations

    /// <summary>
    /// Previews bulk updates for supplier products without applying changes.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview of changes showing current and new values</returns>
    /// <response code="200">Returns the preview of changes</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpPost("{supplierId:guid}/products/bulk-preview")]
    [ProducesResponseType(typeof(List<SupplierProductPreview>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SupplierProductPreview>>> PreviewBulkUpdateSupplierProducts(
        Guid supplierId,
        [FromBody] BulkUpdateSupplierProductsRequest request,
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
            // Check if supplier exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(supplierId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Supplier with ID {supplierId} not found.");
            }

            var previews = await _supplierProductBulkService.PreviewBulkUpdateAsync(supplierId, request, cancellationToken);
            return Ok(previews);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing bulk updates.", ex);
        }
    }

    /// <summary>
    /// Performs bulk updates on supplier products with transaction safety.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="request">Bulk update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the bulk update operation</returns>
    /// <response code="200">Returns the result of the bulk update</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpPost("{supplierId:guid}/products/bulk-update")]
    [ProducesResponseType(typeof(BulkUpdateResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkUpdateResult>> BulkUpdateSupplierProducts(
        Guid supplierId,
        [FromBody] BulkUpdateSupplierProductsRequest request,
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
            // Check if supplier exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(supplierId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Supplier with ID {supplierId} not found.");
            }

            var currentUser = GetCurrentUser();
            var result = await _supplierProductBulkService.BulkUpdateSupplierProductsAsync(supplierId, request, currentUser, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while performing bulk updates.", ex);
        }
    }

    /// <summary>
    /// Validates a CSV file for supplier product import.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="file">CSV file to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with preview and suggestions</returns>
    /// <response code="200">Returns the validation result</response>
    /// <response code="400">If the file is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpPost("{supplierId:guid}/products/validate-csv")]
    [ProducesResponseType(typeof(CsvValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CsvValidationResult>> ValidateCsv(
        Guid supplierId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid file",
                Detail = "CSV file is required"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if supplier exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(supplierId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Supplier with ID {supplierId} not found.");
            }

            var result = await _csvImportService.ValidateCsvAsync(supplierId, file, null, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while validating CSV file.", ex);
        }
    }

    /// <summary>
    /// Imports supplier products from a CSV file.
    /// </summary>
    /// <param name="supplierId">Supplier ID</param>
    /// <param name="file">CSV file to import</param>
    /// <param name="options">Import options (as JSON in form data)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics and errors</returns>
    /// <response code="200">Returns the import result</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the supplier is not found</response>
    [HttpPost("{supplierId:guid}/products/import-csv")]
    [ProducesResponseType(typeof(CsvImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CsvImportResult>> ImportCsv(
        Guid supplierId,
        IFormFile file,
        [FromForm] string? options,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid file",
                Detail = "CSV file is required"
            });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            // Check if supplier exists
            var exists = await _businessPartyService.BusinessPartyExistsAsync(supplierId, cancellationToken);
            if (!exists)
            {
                return CreateNotFoundProblem($"Supplier with ID {supplierId} not found.");
            }

            // Parse options from JSON
            var importOptions = string.IsNullOrWhiteSpace(options)
                ? new CsvImportOptions()
                : System.Text.Json.JsonSerializer.Deserialize<CsvImportOptions>(options) ?? new CsvImportOptions();

            var currentUser = GetCurrentUser();
            var result = await _csvImportService.ImportCsvAsync(supplierId, file, importOptions, currentUser, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while importing CSV file.", ex);
        }
    }

    /// <summary>
    /// Export all business parties to Excel or CSV (Admin/SuperAdmin only)
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
    public async Task<IActionResult> ExportBusinessParties(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Export operation started by {User} for BusinessParties (format: {Format})",
            User.Identity?.Name ?? "Unknown", format);
        
        // Use high page size for export (configured in appsettings.json)
        var pagination = new PaginationParameters 
        { 
            Page = 1, 
            PageSize = 50000 // Will be capped to MaxExportPageSize
        };
        
        var data = await _businessPartyService.GetBusinessPartiesForExportAsync(pagination, ct);
        
        byte[] fileBytes;
        string contentType;
        string fileName;
        
        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await _exportService.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"BusinessParties_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;
            
            case "excel":
            default:
                fileBytes = await _exportService.ExportToExcelAsync(data, "Business Parties", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"BusinessParties_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }
        
        _logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());
        
        return File(fileBytes, contentType, fileName);
    }

    #endregion
}