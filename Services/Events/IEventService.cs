using EventForge.Models.Events;
using EventForge.Models.Audit;

namespace EventForge.Services.Events;

/// <summary>
/// Service interface for managing events and related operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Gets all events with optional filtering and pagination.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated events with total count</returns>
    Task<PagedResult<EventResponseDto>> GetEventsAsync(
        EventQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="includeTeams">Whether to include teams and members in the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The event details or null if not found</returns>
    Task<EventResponseDto?> GetEventByIdAsync(
        Guid id,
        bool includeTeams = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="eventCreateDto">Event creation data</param>
    /// <param name="createdBy">User creating the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created event details</returns>
    Task<EventResponseDto> CreateEventAsync(
        EventCreateDto eventCreateDto,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">The event ID to update</param>
    /// <param name="eventUpdateDto">Event update data</param>
    /// <param name="modifiedBy">User modifying the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated event details or null if not found</returns>
    Task<EventResponseDto?> UpdateEventAsync(
        Guid id,
        EventUpdateDto eventUpdateDto,
        string modifiedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event (soft delete).
    /// </summary>
    /// <param name="id">The event ID to delete</param>
    /// <param name="deletedBy">User deleting the event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the event was deleted, false if not found</returns>
    Task<bool> DeleteEventAsync(
        Guid id,
        string deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the event exists and is not deleted</returns>
    Task<bool> EventExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events by status.
    /// </summary>
    /// <param name="status">The event status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events with the specified status</returns>
    Task<IEnumerable<EventResponseDto>> GetEventsByStatusAsync(
        int status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    /// <param name="fromDate">Start date for the range</param>
    /// <param name="toDate">End date for the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of events within the date range</returns>
    Task<IEnumerable<EventResponseDto>> GetEventsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}