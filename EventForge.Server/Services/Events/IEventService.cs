namespace EventForge.Server.Services.Events;

/// <summary>
/// Service interface for managing events.
/// </summary>
public interface IEventService
{
    // Event CRUD operations

    /// <summary>
    /// Gets all events with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of events</returns>
    Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event DTO or null if not found</returns>
    Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed event information including teams and members.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed event DTO or null if not found</returns>
    Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="createEventDto">Event creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created event DTO</returns>
    Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="updateEventDto">Event update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated event DTO or null if not found</returns>
    Task<EventDto?> UpdateEventAsync(Guid id, UpdateEventDto updateEventDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an event (soft delete).
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="rowVersion">Row version for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteEventAsync(Guid id, string currentUser, byte[] rowVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an event exists.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
}