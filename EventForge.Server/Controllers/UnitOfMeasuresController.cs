using EventForge.DTOs.UnitOfMeasures;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for unit of measure management with multi-tenant support.
/// Provides CRUD operations for units of measure within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class UnitOfMeasuresController : BaseApiController
{
    private readonly IUMService _umService;
    private readonly ITenantContext _tenantContext;

    public UnitOfMeasuresController(IUMService umService, ITenantContext tenantContext)
    {
        _umService = umService ?? throw new ArgumentNullException(nameof(umService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all units of measure with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of units of measure</returns>
    /// <response code="200">Returns the paginated list of units of measure</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UMDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UMDto>>> GetUnitOfMeasures(
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
            var result = await _umService.GetUMsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving units of measure.", ex);
        }
    }

    /// <summary>
    /// Gets a unit of measure by ID.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unit of measure information</returns>
    /// <response code="200">Returns the unit of measure</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UMDto>> GetUnitOfMeasure(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var um = await _umService.GetUMByIdAsync(id, cancellationToken);

            if (um == null)
            {
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");
            }

            return Ok(um);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the unit of measure.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="createUMDto">Unit of measure creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created unit of measure information</returns>
    /// <response code="201">Returns the created unit of measure</response>
    /// <response code="400">If the unit of measure data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UMDto>> CreateUnitOfMeasure(
        [FromBody] CreateUMDto createUMDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var um = await _umService.CreateUMAsync(createUMDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetUnitOfMeasure),
                new { id = um.Id },
                um);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the unit of measure.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing unit of measure.
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="updateUMDto">Unit of measure update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated unit of measure information</returns>
    /// <response code="200">Returns the updated unit of measure</response>
    /// <response code="400">If the unit of measure data is invalid</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UMDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UMDto>> UpdateUnitOfMeasure(
        Guid id,
        [FromBody] UpdateUMDto updateUMDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var um = await _umService.UpdateUMAsync(id, updateUMDto, currentUser, cancellationToken);

            if (um == null)
            {
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");
            }

            return Ok(um);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the unit of measure.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a unit of measure (soft delete).
    /// </summary>
    /// <param name="id">Unit of measure ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the unit of measure was successfully deleted</response>
    /// <response code="404">If the unit of measure is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUnitOfMeasure(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = User?.Identity?.Name ?? "System";
            var deleted = await _umService.DeleteUMAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Unit of measure with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the unit of measure.", error = ex.Message });
        }
    }
}