using Microsoft.AspNetCore.Mvc;
using EventForge.Services.Events;
using EventForge.Models.Events;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for event management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    /// <summary>
    /// Gets paginated events with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated events</returns>
    /// <response code="200">Returns the paginated events</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(Models.Audit.PagedResult<EventResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Models.Audit.PagedResult<EventResponseDto>>> GetEvents(
        [FromQuery] EventQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _eventService.GetEventsAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving events.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="includeTeams">Whether to include teams and members in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The event details</returns>
    /// <response code="200">Returns the event details</response>
    /// <response code="404">If the event is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> GetEvent(
        Guid id,
        [FromQuery] bool includeTeams = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventResponse = await _eventService.GetEventByIdAsync(id, includeTeams, cancellationToken);
            
            if (eventResponse == null)
            {
                return NotFound(new { message = $"Event with ID {id} not found." });
            }

            return Ok(eventResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving the event.", error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventCreateDto">Event creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created event details</returns>
    /// <response code="201">Returns the created event</response>
    /// <response code="400">If the event data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponseDto>> CreateEvent(
        [FromBody] EventCreateDto eventCreateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // For now, use a default user. In a real application, this would come from authentication context
            var createdBy = "system"; // TODO: Get from authentication context
            
            var createdEvent = await _eventService.CreateEventAsync(eventCreateDto, createdBy, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetEvent), 
                new { id = createdEvent.Id }, 
                createdEvent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = "Invalid event data.", error = ex.Message });
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
    /// <param name="id">The event ID to update</param>
    /// <param name="eventUpdateDto">Event update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated event details</returns>
    /// <response code="200">Returns the updated event</response>
    /// <response code="400">If the event data is invalid</response>
    /// <response code="404">If the event is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponseDto>> UpdateEvent(
        Guid id,
        [FromBody] EventUpdateDto eventUpdateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // For now, use a default user. In a real application, this would come from authentication context
            var modifiedBy = "system"; // TODO: Get from authentication context
            
            var updatedEvent = await _eventService.UpdateEventAsync(id, eventUpdateDto, modifiedBy, cancellationToken);
            
            if (updatedEvent == null)
            {
                return NotFound(new { message = $"Event with ID {id} not found." });
            }

            return Ok(updatedEvent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = "Invalid event data.", error = ex.Message });
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
    /// <param name="id">The event ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
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
            // For now, use a default user. In a real application, this would come from authentication context
            var deletedBy = "system"; // TODO: Get from authentication context
            
            var deleted = await _eventService.DeleteEventAsync(id, deletedBy, cancellationToken);
            
            if (!deleted)
            {
                return NotFound(new { message = $"Event with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while deleting the event.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets events by status.
    /// </summary>
    /// <param name="status">The event status (0=Planned, 1=Ongoing, 2=Completed, 3=Cancelled)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events with the specified status</returns>
    /// <response code="200">Returns the events for the status</response>
    /// <response code="400">If the status is invalid</response>
    [HttpGet("status/{status:int}")]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEventsByStatus(
        int status,
        CancellationToken cancellationToken = default)
    {
        if (status < 0 || status > 3)
        {
            return BadRequest(new { message = "Invalid status. Valid values are 0=Planned, 1=Ongoing, 2=Completed, 3=Cancelled." });
        }

        try
        {
            var events = await _eventService.GetEventsByStatusAsync(status, cancellationToken);
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving events by status.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range (ISO format)</param>
    /// <param name="toDate">End date for the range (ISO format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events within the date range</returns>
    /// <response code="200">Returns the events within the date range</response>
    /// <response code="400">If the date range is invalid</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEventsInDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        if (fromDate >= toDate)
        {
            return BadRequest(new { message = "From date must be earlier than to date." });
        }

        try
        {
            var events = await _eventService.GetEventsInDateRangeAsync(fromDate, toDate, cancellationToken);
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while retrieving events in date range.", error = ex.Message });
        }
    }

    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existence status</returns>
    /// <response code="200">Returns whether the event exists</response>
    [HttpHead("{id:guid}")]
    [HttpGet("{id:guid}/exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventExists(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _eventService.EventExistsAsync(id, cancellationToken);
            
            if (exists)
            {
                return Ok(new { exists = true });
            }
            else
            {
                return NotFound(new { exists = false });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "An error occurred while checking event existence.", error = ex.Message });
        }
    }
}