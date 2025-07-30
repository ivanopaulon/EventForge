using EventForge.DTOs.PriceLists;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for price list management with multi-tenant support.
/// Provides CRUD operations for price lists within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class PriceListsController : BaseApiController
{
    private readonly IPriceListService _priceListService;
    private readonly ITenantContext _tenantContext;

    public PriceListsController(IPriceListService priceListService, ITenantContext tenantContext)
    {
        _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region PriceList CRUD Operations

    /// <summary>
    /// Gets all price lists with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of price lists</returns>
    /// <response code="200">Returns the paginated list of price lists</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PriceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PriceListDto>>> GetPriceLists(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _priceListService.GetPriceListsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price lists.", ex);
        }
    }

    /// <summary>
    /// Gets price lists for a specific event.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of price lists for the event</returns>
    /// <response code="200">Returns the list of price lists for the event</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("by-event/{eventId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PriceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PriceListDto>>> GetPriceListsByEvent(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var priceLists = await _priceListService.GetPriceListsByEventAsync(eventId, cancellationToken);
            return Ok(priceLists);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price lists for the event.", ex);
        }
    }

    /// <summary>
    /// Gets a price list by ID.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price list information</returns>
    /// <response code="200">Returns the price list</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> GetPriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var priceList = await _priceListService.GetPriceListByIdAsync(id, cancellationToken);

            if (priceList == null)
            {
                return CreateNotFoundProblem($"Price list with ID {id} not found.");
            }

            return Ok(priceList);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the price list.", ex);
        }
    }

    /// <summary>
    /// Gets detailed price list information including entries.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed price list information</returns>
    /// <response code="200">Returns the detailed price list information</response>
    /// <response code="404">If the price list is not found</response>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(PriceListDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDetailDto>> GetPriceListDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priceListDetail = await _priceListService.GetPriceListDetailAsync(id, cancellationToken);

            if (priceListDetail == null)
            {
                return NotFound(new { message = $"Price list with ID {id} not found." });
            }

            return Ok(priceListDetail);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the price list details.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new price list.
    /// </summary>
    /// <param name="createPriceListDto">Price list creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created price list</returns>
    /// <response code="201">Returns the newly created price list</response>
    /// <response code="400">If the price list data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> CreatePriceList(
        [FromBody] CreatePriceListDto createPriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await _priceListService.CreatePriceListAsync(createPriceListDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the price list.", ex);
        }
    }

    /// <summary>
    /// Updates an existing price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="updatePriceListDto">Price list update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated price list</returns>
    /// <response code="200">Returns the updated price list</response>
    /// <response code="400">If the price list data is invalid</response>
    /// <response code="404">If the price list is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDto>> UpdatePriceList(
        Guid id,
        [FromBody] UpdatePriceListDto updatePriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await _priceListService.UpdatePriceListAsync(id, updatePriceListDto, currentUser, cancellationToken);

            if (priceList == null)
            {
                return NotFound(new { message = $"Price list with ID {id} not found." });
            }

            return Ok(priceList);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the price list.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a price list (soft delete).
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Price list deleted successfully</response>
    /// <response code="404">If the price list is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _priceListService.DeletePriceListAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Price list with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the price list.", error = ex.Message });
        }
    }

    #endregion

    #region PriceList Entry Management Operations

    /// <summary>
    /// Gets all entries for a price list.
    /// </summary>
    /// <param name="priceListId">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of price list entries</returns>
    /// <response code="200">Returns the list of price list entries</response>
    /// <response code="404">If the price list is not found</response>
    [HttpGet("{priceListId:guid}/entries")]
    [ProducesResponseType(typeof(IEnumerable<PriceListEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PriceListEntryDto>>> GetPriceListEntries(
        Guid priceListId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _priceListService.PriceListExistsAsync(priceListId, cancellationToken))
            {
                return NotFound(new { message = $"Price list with ID {priceListId} not found." });
            }

            var entries = await _priceListService.GetPriceListEntriesAsync(priceListId, cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving price list entries.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a price list entry by ID.
    /// </summary>
    /// <param name="id">Price list entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price list entry information</returns>
    /// <response code="200">Returns the price list entry</response>
    /// <response code="404">If the price list entry is not found</response>
    [HttpGet("entries/{id:guid}")]
    [ProducesResponseType(typeof(PriceListEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListEntryDto>> GetPriceListEntry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _priceListService.GetPriceListEntryByIdAsync(id, cancellationToken);

            if (entry == null)
            {
                return NotFound(new { message = $"Price list entry with ID {id} not found." });
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the price list entry.", error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a new entry to a price list.
    /// </summary>
    /// <param name="createPriceListEntryDto">Price list entry creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created price list entry</returns>
    /// <response code="201">Returns the newly created price list entry</response>
    /// <response code="400">If the price list entry data is invalid</response>
    [HttpPost("entries")]
    [ProducesResponseType(typeof(PriceListEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceListEntryDto>> AddPriceListEntry(
        [FromBody] CreatePriceListEntryDto createPriceListEntryDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var entry = await _priceListService.AddPriceListEntryAsync(createPriceListEntryDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetPriceListEntry), new { id = entry.Id }, entry);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the price list entry.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing price list entry.
    /// </summary>
    /// <param name="id">Price list entry ID</param>
    /// <param name="updatePriceListEntryDto">Price list entry update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated price list entry</returns>
    /// <response code="200">Returns the updated price list entry</response>
    /// <response code="400">If the price list entry data is invalid</response>
    /// <response code="404">If the price list entry is not found</response>
    [HttpPut("entries/{id:guid}")]
    [ProducesResponseType(typeof(PriceListEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListEntryDto>> UpdatePriceListEntry(
        Guid id,
        [FromBody] UpdatePriceListEntryDto updatePriceListEntryDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var entry = await _priceListService.UpdatePriceListEntryAsync(id, updatePriceListEntryDto, currentUser, cancellationToken);

            if (entry == null)
            {
                return NotFound(new { message = $"Price list entry with ID {id} not found." });
            }

            return Ok(entry);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the price list entry.", error = ex.Message });
        }
    }

    /// <summary>
    /// Removes an entry from a price list (soft delete).
    /// </summary>
    /// <param name="id">Price list entry ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion</returns>
    /// <response code="204">Price list entry deleted successfully</response>
    /// <response code="404">If the price list entry is not found</response>
    [HttpDelete("entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePriceListEntry(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _priceListService.RemovePriceListEntryAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = $"Price list entry with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the price list entry.", error = ex.Message });
        }
    }

    #endregion
}