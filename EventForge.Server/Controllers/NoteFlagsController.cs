using EventForge.DTOs.Sales;
using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Caching;
using EventForge.Server.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for note flag management with multi-tenant support.
/// Provides CRUD operations for note flags used in sales sessions.
/// </summary>
[Route("api/v1/note-flags")]
[Authorize]
[RequireLicenseFeature("SalesManagement")]
public class NoteFlagsController : BaseApiController
{
    private readonly INoteFlagService _noteFlagService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<NoteFlagsController> _logger;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public NoteFlagsController(
        INoteFlagService noteFlagService,
        ITenantContext tenantContext,
        ILogger<NoteFlagsController> logger,
        ICacheInvalidationService cacheInvalidation)
    {
        _noteFlagService = noteFlagService ?? throw new ArgumentNullException(nameof(noteFlagService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    }

    /// <summary>
    /// Gets all note flags with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of note flags</returns>
    /// <response code="200">Successfully retrieved note flags with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [OutputCache(PolicyName = "SemiStaticEntities")]
    [ProducesResponseType(typeof(PagedResult<NoteFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<NoteFlagDto>>> GetAll(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _noteFlagService.GetNoteFlagsAsync(pagination, cancellationToken);

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
            _logger.LogError(ex, "An error occurred while retrieving note flags.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving note flags.", ex);
        }
    }

    /// <summary>
    /// Gets only active note flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active note flags ordered by display order</returns>
    /// <response code="200">Returns the list of active note flags</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<NoteFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<NoteFlagDto>>> GetActive(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var noteFlags = await _noteFlagService.GetActiveAsync(cancellationToken);
            return Ok(noteFlags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving active note flags.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving active note flags.", ex);
        }
    }

    /// <summary>
    /// Gets a specific note flag by ID.
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Note flag details</returns>
    /// <response code="200">Returns the note flag</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the note flag is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NoteFlagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteFlagDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var noteFlag = await _noteFlagService.GetByIdAsync(id, cancellationToken);

            if (noteFlag == null)
                return NotFound(new { message = $"Note flag {id} not found." });

            return Ok(noteFlag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving note flag {NoteFlagId}.", id);
            return CreateInternalServerErrorProblem($"An error occurred while retrieving the note flag.", ex);
        }
    }

    /// <summary>
    /// Creates a new note flag.
    /// </summary>
    /// <param name="createDto">Note flag creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created note flag</returns>
    /// <response code="201">Returns the newly created note flag</response>
    /// <response code="400">If the request is invalid or the code already exists</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(NoteFlagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<NoteFlagDto>> Create(
        [FromBody] CreateNoteFlagDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var noteFlag = await _noteFlagService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = noteFlag.Id },
                noteFlag);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating note flag.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating note flag.");
            return CreateInternalServerErrorProblem("An error occurred while creating the note flag.", ex);
        }
    }

    /// <summary>
    /// Updates a note flag.
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="updateDto">Note flag update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated note flag</returns>
    /// <response code="200">Returns the updated note flag</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the note flag is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(NoteFlagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteFlagDto>> Update(
        Guid id,
        [FromBody] UpdateNoteFlagDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var noteFlag = await _noteFlagService.UpdateAsync(id, updateDto, currentUser, cancellationToken);

            if (noteFlag == null)
                return NotFound(new { message = $"Note flag {id} not found." });

            await _cacheInvalidation.InvalidateSemiStaticEntitiesAsync(cancellationToken);
            return Ok(noteFlag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating note flag {NoteFlagId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while updating the note flag.", ex);
        }
    }

    /// <summary>
    /// Deletes a note flag (soft delete).
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the note flag was successfully deleted</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the note flag is not found</response>
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
            var deleted = await _noteFlagService.DeleteAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Note flag {id} not found." });

            await _cacheInvalidation.InvalidateSemiStaticEntitiesAsync(cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting note flag {NoteFlagId}.", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the note flag.", ex);
        }
    }
}
