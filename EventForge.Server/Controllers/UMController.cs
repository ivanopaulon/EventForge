using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for unit of measure management with multi-tenant support.
/// Provides CRUD operations for units of measure.
/// </summary>
[Route("api/v1/unit-of-measures")]
[Authorize(Policy = "RequireManager")]
[RequireLicenseFeature("ProductManagement")]
public class UMController : BaseApiController
{
    private readonly IUMService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UMController> _logger;

    public UMController(
        IUMService service,
        ITenantContext tenantContext,
        ILogger<UMController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all unit of measures with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of unit of measures</returns>
    /// <response code="200">Successfully retrieved unit of measures with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UMDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UMDto>>> GetUnitOfMeasures(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _service.GetUMsAsync(pagination, cancellationToken);
            
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
            _logger.LogError(ex, "An error occurred while retrieving unit of measures.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving unit of measures.", ex);
        }
    }

    /// <summary>
    /// Gets a specific unit of measure by ID.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unit of measure details</returns>
    /// <response code="200">Returns the unit of measure</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UMDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var um = await _service.GetUMByIdAsync(id, cancellationToken);

            if (um == null)
                return NotFound(new { message = $"Unit of measure {id} not found." });

            return Ok(um);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving unit of measure {UMId}.", id);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="createDto">Unit of measure creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created unit of measure</returns>
    /// <response code="201">Returns the newly created unit of measure</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UMDto>> Create(
        [FromBody] CreateUMDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var um = await _service.CreateUMAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = um.Id },
                um);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating unit of measure.");
            return CreateInternalServerErrorProblem("An error occurred while creating the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Updates a unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="updateDto">Unit of measure update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated unit of measure</returns>
    /// <response code="200">Returns the updated unit of measure</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UMDto>> Update(
        Guid id,
        [FromBody] UpdateUMDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var um = await _service.UpdateUMAsync(id, updateDto, currentUser, cancellationToken);

            if (um == null)
                return NotFound(new { message = $"Unit of measure {id} not found." });

            return Ok(um);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating unit of measure {UMId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while updating the unit of measure.", ex);
        }
    }

    /// <summary>
    /// Deletes a unit of measure (soft delete).
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the unit of measure was successfully deleted</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var deleted = await _service.DeleteUMAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Unit of measure {id} not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting unit of measure {UMId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the unit of measure.", ex);
        }
    }
}
