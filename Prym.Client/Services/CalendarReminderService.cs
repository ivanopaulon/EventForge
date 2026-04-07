using Prym.DTOs.Calendar;
using Prym.DTOs.Common;

namespace Prym.Client.Services;

public interface ICalendarReminderService
{
    Task<PagedResult<CalendarReminderDto>> GetCalendarRemindersAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IEnumerable<CalendarReminderDto>> GetCalendarRemindersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IEnumerable<CalendarReminderDto>> GetActiveRemindersAsync(CancellationToken ct = default);
    Task<CalendarReminderDto?> GetCalendarReminderByIdAsync(Guid id, CancellationToken ct = default);
    Task<CalendarReminderDto> CreateCalendarReminderAsync(CreateCalendarReminderDto createDto, CancellationToken ct = default);
    Task<CalendarReminderDto?> UpdateCalendarReminderAsync(Guid id, UpdateCalendarReminderDto updateDto, CancellationToken ct = default);
    Task<CalendarReminderDto?> CompleteCalendarReminderAsync(Guid id, string? completionNotes = null, CancellationToken ct = default);
    Task DeleteCalendarReminderAsync(Guid id, CancellationToken ct = default);
}

public class CalendarReminderService(IHttpClientService httpClientService) : ICalendarReminderService
{

    public async Task<PagedResult<CalendarReminderDto>> GetCalendarRemindersAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var url = $"api/v1/calendar-reminders?page={page}&pageSize={pageSize}";
        return await httpClientService.GetAsync<PagedResult<CalendarReminderDto>>(url, ct)
            ?? new PagedResult<CalendarReminderDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<CalendarReminderDto>> GetCalendarRemindersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var url = $"api/v1/calendar-reminders/date-range?startDate={startDate:O}&endDate={endDate:O}";
        return await httpClientService.GetAsync<IEnumerable<CalendarReminderDto>>(url, ct)
            ?? Enumerable.Empty<CalendarReminderDto>();
    }

    public async Task<IEnumerable<CalendarReminderDto>> GetActiveRemindersAsync(CancellationToken ct = default)
    {
        return await httpClientService.GetAsync<IEnumerable<CalendarReminderDto>>("api/v1/calendar-reminders/active", ct)
            ?? Enumerable.Empty<CalendarReminderDto>();
    }

    public async Task<CalendarReminderDto?> GetCalendarReminderByIdAsync(Guid id, CancellationToken ct = default)
        => await httpClientService.GetAsync<CalendarReminderDto>($"api/v1/calendar-reminders/{id}", ct);

    public async Task<CalendarReminderDto> CreateCalendarReminderAsync(CreateCalendarReminderDto createDto, CancellationToken ct = default)
        => await httpClientService.PostAsync<CreateCalendarReminderDto, CalendarReminderDto>("api/v1/calendar-reminders", createDto, ct)
           ?? throw new InvalidOperationException("Failed to create calendar reminder.");

    public async Task<CalendarReminderDto?> UpdateCalendarReminderAsync(Guid id, UpdateCalendarReminderDto updateDto, CancellationToken ct = default)
        => await httpClientService.PutAsync<UpdateCalendarReminderDto, CalendarReminderDto>($"api/v1/calendar-reminders/{id}", updateDto, ct);

    public async Task<CalendarReminderDto?> CompleteCalendarReminderAsync(Guid id, string? completionNotes = null, CancellationToken ct = default)
    {
        var body = new { CompletionNotes = completionNotes };
        return await httpClientService.PostAsync<object, CalendarReminderDto>($"api/v1/calendar-reminders/{id}/complete", body, ct);
    }

    public async Task DeleteCalendarReminderAsync(Guid id, CancellationToken ct = default)
        => await httpClientService.DeleteAsync($"api/v1/calendar-reminders/{id}", ct);
}
