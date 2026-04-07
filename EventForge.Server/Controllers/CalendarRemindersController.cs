using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for calendar reminder and task management with multi-tenant support.
/// </summary>
[Route("api/v1/calendar-reminders")]
[Authorize]
[RequireLicenseFeature("BasicEventManagement")]
public class CalendarRemindersController(
    ICalendarReminderService calendarReminderService,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Gets all calendar reminders with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of calendar reminders.</returns>
    /// <response code="200">Successfully retrieved calendar reminders with pagination metadata in headers.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="403">User does not have access to the current tenant.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CalendarReminderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<CalendarReminderDto>>> GetCalendarReminders(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await calendarReminderService.GetCalendarRemindersAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving calendar reminders.", ex);
        }
    }

    /// <summary>
    /// Gets calendar reminders within a date range.
    /// </summary>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of calendar reminders within the date range.</returns>
    /// <response code="200">Successfully retrieved calendar reminders.</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="403">User does not have access to the current tenant.</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(IEnumerable<CalendarReminderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<CalendarReminderDto>>> GetCalendarRemindersByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (endDate < startDate)
            return CreateValidationProblemDetails("End date must be greater than or equal to start date.");

        try
        {
            var result = await calendarReminderService.GetCalendarRemindersByDateRangeAsync(startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving calendar reminders by date range.", ex);
        }
    }

    /// <summary>
    /// Gets all active calendar reminders for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active calendar reminders.</returns>
    /// <response code="200">Successfully retrieved active calendar reminders.</response>
    /// <response code="403">User does not have access to the current tenant.</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<CalendarReminderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<CalendarReminderDto>>> GetActiveReminders(
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await calendarReminderService.GetActiveRemindersAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active calendar reminders.", ex);
        }
    }

    /// <summary>
    /// Gets a calendar reminder by ID.
    /// </summary>
    /// <param name="id">Calendar reminder ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Calendar reminder information.</returns>
    /// <response code="200">Returns the calendar reminder.</response>
    /// <response code="404">Calendar reminder not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CalendarReminderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarReminderDto>> GetCalendarReminderById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reminder = await calendarReminderService.GetCalendarReminderByIdAsync(id, cancellationToken);

            if (reminder is null)
                return CreateNotFoundProblem($"Calendar reminder with ID {id} not found.");

            return Ok(reminder);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the calendar reminder.", ex);
        }
    }

    /// <summary>
    /// Creates a new calendar reminder.
    /// </summary>
    /// <param name="createDto">Calendar reminder creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created calendar reminder.</returns>
    /// <response code="201">Returns the newly created calendar reminder.</response>
    /// <response code="400">Invalid calendar reminder data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CalendarReminderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalendarReminderDto>> CreateCalendarReminder(
        [FromBody] CreateCalendarReminderDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var reminder = await calendarReminderService.CreateCalendarReminderAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetCalendarReminderById),
                new { id = reminder.Id },
                reminder);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the calendar reminder.", ex);
        }
    }

    /// <summary>
    /// Updates an existing calendar reminder.
    /// </summary>
    /// <param name="id">Calendar reminder ID.</param>
    /// <param name="updateDto">Calendar reminder update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated calendar reminder.</returns>
    /// <response code="200">Returns the updated calendar reminder.</response>
    /// <response code="400">Invalid calendar reminder data.</response>
    /// <response code="404">Calendar reminder not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CalendarReminderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarReminderDto>> UpdateCalendarReminder(
        Guid id,
        [FromBody] UpdateCalendarReminderDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var reminder = await calendarReminderService.UpdateCalendarReminderAsync(id, updateDto, currentUser, cancellationToken);

            if (reminder is null)
                return CreateNotFoundProblem($"Calendar reminder with ID {id} not found.");

            return Ok(reminder);
        }
        catch (InvalidOperationException ex) when (ex.InnerException is DbUpdateConcurrencyException)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the calendar reminder.", ex);
        }
    }

    /// <summary>
    /// Marks a calendar reminder as completed.
    /// </summary>
    /// <param name="id">Calendar reminder ID.</param>
    /// <param name="request">Completion request with optional notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated calendar reminder.</returns>
    /// <response code="200">Returns the completed calendar reminder.</response>
    /// <response code="404">Calendar reminder not found.</response>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(CalendarReminderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarReminderDto>> CompleteCalendarReminder(
        Guid id,
        [FromBody] CompleteCalendarReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var reminder = await calendarReminderService.CompleteCalendarReminderAsync(id, request?.CompletionNotes, currentUser, cancellationToken);

            if (reminder is null)
                return CreateNotFoundProblem($"Calendar reminder with ID {id} not found.");

            return Ok(reminder);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while completing the calendar reminder.", ex);
        }
    }

    /// <summary>
    /// Deletes a calendar reminder (soft delete).
    /// </summary>
    /// <param name="id">Calendar reminder ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Calendar reminder successfully deleted.</response>
    /// <response code="404">Calendar reminder not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCalendarReminder(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await calendarReminderService.DeleteCalendarReminderAsync(id, currentUser, cancellationToken);

            if (!result)
                return CreateNotFoundProblem($"Calendar reminder with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the calendar reminder.", ex);
        }
    }
}

/// <summary>
/// Request body for completing a calendar reminder.
/// </summary>
public class CompleteCalendarReminderRequest
{
    public string? CompletionNotes { get; set; }
}
