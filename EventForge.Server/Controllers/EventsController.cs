using EventForge.Server.Filters;
using EventForge.Server.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for event management with multi-tenant support.
/// Provides comprehensive CRUD operations for events, teams, and event-related entities
/// within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class EventsController : BaseApiController
{
    private readonly IEventService _eventService;
    private readonly ITenantContext _tenantContext;

    public EventsController(IEventService eventService, ITenantContext tenantContext)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Event CRUD Operations

    /// <summary>
    /// Gets all events with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="deleted">Filter for soft-deleted items: 'false' (default), 'true', or 'all'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    /// <response code="200">Returns the paginated list of events</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [SoftDeleteFilter]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string deleted = "false",
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate pagination parameters using helper
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        // Validate tenant access
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _eventService.GetEventsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving events.", ex);
        }
    }

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event information</returns>
    /// <response code="200">Returns the event</response>
    /// <response code="404">If the event is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _eventService.GetEventByIdAsync(id, cancellationToken);

            if (eventEntity == null)
            {
                return CreateNotFoundProblem($"Event with ID {id} not found.");
            }

            return Ok(eventEntity);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the event.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets detailed event information including associated teams and members.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed event information</returns>
    /// <response code="200">Returns the detailed event information</response>
    /// <response code="404">If the event is not found</response>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEventDetail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventDetail = await _eventService.GetEventDetailAsync(id, cancellationToken);

            if (eventDetail == null)
            {
                return CreateNotFoundProblem($"Event with ID {id} not found.");
            }

            return Ok(eventDetail);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the event details.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="createEventDto">Event creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event</returns>
    /// <response code="201">Returns the newly created event</response>
    /// <response code="400">If the event data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> CreateEvent(
        [FromBody] CreateEventDto createEventDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var eventEntity = await _eventService.CreateEventAsync(createEventDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventEntity.Id },
                eventEntity);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while creating the event.", error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="updateEventDto">Event update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event</returns>
    /// <response code="200">Returns the updated event</response>
    /// <response code="400">If the event data is invalid</response>
    /// <response code="404">If the event is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(
        Guid id,
        [FromBody] UpdateEventDto updateEventDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUser = GetCurrentUser();
            var eventEntity = await _eventService.UpdateEventAsync(id, updateEventDto, currentUser, cancellationToken);

            if (eventEntity == null)
            {
                return CreateNotFoundProblem($"Event with ID {id} not found.");
            }

            return Ok(eventEntity);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while updating the event.", error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the event was successfully deleted</response>
    /// <response code="404">If the event is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await _eventService.DeleteEventAsync(id, currentUser, Array.Empty<byte>(), cancellationToken);

            if (!result)
            {
                return CreateNotFoundProblem($"Event with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while deleting the event.", error = ex.Message });
        }
    }

    #endregion
}