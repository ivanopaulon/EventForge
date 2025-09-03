using EventForge.DTOs.Business;
using EventForge.Server.Filters;
using EventForge.Server.Services.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ITenantContext _tenantContext;

    public BusinessPartiesController(IBusinessPartyService businessPartyService, ITenantContext tenantContext)
    {
        _businessPartyService = businessPartyService ?? throw new ArgumentNullException(nameof(businessPartyService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region BusinessParty Endpoints

    /// <summary>
    /// Gets all business parties with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business parties</returns>
    /// <response code="200">Returns the paginated list of business parties</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyDto>>> GetBusinessParties(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        // Validate tenant access
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyService.GetBusinessPartiesAsync(page, pageSize, cancellationToken);
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
    /// Gets all business party accounting records with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party accounting records</returns>
    /// <response code="200">Returns the paginated list of business party accounting records</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("accounting")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartyAccountingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BusinessPartyAccountingDto>>> GetBusinessPartyAccounting(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _businessPartyService.GetBusinessPartyAccountingAsync(page, pageSize, cancellationToken);
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
}