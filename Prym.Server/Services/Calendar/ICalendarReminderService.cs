namespace Prym.Server.Services.Calendar;

/// <summary>
/// Service interface for managing calendar reminders and tasks.
/// </summary>
public interface ICalendarReminderService
{
    /// <summary>Gets all calendar reminders for the current tenant with pagination.</summary>
    Task<PagedResult<CalendarReminderDto>> GetCalendarRemindersAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>Gets calendar reminders within a date range for the current tenant.</summary>
    Task<IEnumerable<CalendarReminderDto>> GetCalendarRemindersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>Gets all active (non-completed, non-cancelled) reminders for the current tenant.</summary>
    Task<IEnumerable<CalendarReminderDto>> GetActiveRemindersAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a calendar reminder by ID.</summary>
    Task<CalendarReminderDto?> GetCalendarReminderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new calendar reminder.</summary>
    Task<CalendarReminderDto> CreateCalendarReminderAsync(CreateCalendarReminderDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing calendar reminder.</summary>
    Task<CalendarReminderDto?> UpdateCalendarReminderAsync(Guid id, UpdateCalendarReminderDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>Marks a calendar reminder as completed.</summary>
    Task<CalendarReminderDto?> CompleteCalendarReminderAsync(Guid id, string? completionNotes, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a calendar reminder.</summary>
    Task<bool> DeleteCalendarReminderAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}
