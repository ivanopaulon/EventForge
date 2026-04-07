using Prym.Server.Filters;
using Prym.Server.ModelBinders;
using Prym.Server.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Prym.Server.Controllers;

/// <summary>
/// REST API controller for event management with multi-tenant support.
/// Provides comprehensive CRUD operations for events, teams, and event-related entities
/// within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
[RequireLicenseFeature("BasicEventManagement")]
public class EventsController(
    IEventService eventService,
    ITenantContext tenantContext,
    ILogger<EventsController> logger) : BaseApiController
{

    #region Event CRUD Operations

    /// <summary>
    /// Gets all events with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    /// <response code="200">Successfully retrieved events with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await eventService.GetEventsAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving events.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving events.", ex);
        }
    }

    /// <summary>
    /// Retrieves events within date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date (optional, defaults to 1 year from start)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events within the specified date range</returns>
    /// <response code="200">Successfully retrieved events with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEventsByDate(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await eventService.GetEventsByDateAsync(startDate, endDate, pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving events by date.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving events by date.", ex);
        }
    }

    /// <summary>
    /// Retrieves upcoming events (from now onwards)
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of upcoming events</returns>
    /// <response code="200">Successfully retrieved upcoming events with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetUpcomingEvents(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await eventService.GetUpcomingEventsAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving upcoming events.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving upcoming events.", ex);
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
            var eventEntity = await eventService.GetEventByIdAsync(id, cancellationToken);

            if (eventEntity is null)
                return CreateNotFoundProblem($"Event with ID {id} not found.");

            return Ok(eventEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving the event.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the event.", ex);
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
            var eventDetail = await eventService.GetEventDetailAsync(id, cancellationToken);

            if (eventDetail is null)
                return CreateNotFoundProblem($"Event with ID {id} not found.");

            return Ok(eventDetail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving the event details.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the event details.", ex);
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
            var eventEntity = await eventService.CreateEventAsync(createEventDto, currentUser, cancellationToken);

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
            logger.LogError(ex, "An error occurred while creating the event.");
            return CreateInternalServerErrorProblem("An error occurred while creating the event.", ex);
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
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var eventEntity = await eventService.UpdateEventAsync(id, updateEventDto, currentUser, cancellationToken);

            if (eventEntity is null)
                return CreateNotFoundProblem($"Event with ID {id} not found.");

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
            logger.LogError(ex, "An error occurred while updating the event.");
            return CreateInternalServerErrorProblem("An error occurred while updating the event.", ex);
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
            var result = await eventService.DeleteEventAsync(id, currentUser, [], cancellationToken);

            if (!result)
                return CreateNotFoundProblem($"Event with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting the event.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the event.", ex);
        }
    }

    #endregion
}