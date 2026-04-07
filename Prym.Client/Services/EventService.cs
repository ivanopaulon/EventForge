using Prym.DTOs.Common;
using Prym.DTOs.Events;

namespace Prym.Client.Services
{
    /// <summary>
    /// Service interface for event management operations.
    /// Maps to /api/v1/events endpoints in EventsController.
    /// </summary>
    public interface IEventService
    {
        Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<IEnumerable<EventDto>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default);
        Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken ct = default);
        Task<EventDto> CreateEventAsync(CreateEventDto createDto, CancellationToken ct = default);
        Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto, CancellationToken ct = default);
        Task DeleteEventAsync(Guid id, CancellationToken ct = default);
    }

    public class EventService(IHttpClientService httpClientService) : IEventService
    {

        public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var url = $"api/v1/events?page={page}&pageSize={pageSize}";
            return await httpClientService.GetAsync<PagedResult<EventDto>>(url, ct) ??
                new PagedResult<EventDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }

        public async Task<IEnumerable<EventDto>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            var url = $"api/v1/events/date-range?startDate={startDate:O}&endDate={endDate:O}&page=1&pageSize=1000";
            var result = await httpClientService.GetAsync<PagedResult<EventDto>>(url, ct);
            return result?.Items ?? Enumerable.Empty<EventDto>();
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await httpClientService.GetAsync<EventDto>($"api/v1/events/{id}", ct);
        }

        public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken ct = default)
        {
            return await httpClientService.GetAsync<EventDetailDto>($"api/v1/events/{id}/details", ct);
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createDto, CancellationToken ct = default)
        {
            return await httpClientService.PostAsync<CreateEventDto, EventDto>("api/v1/events", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create event");
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto, CancellationToken ct = default)
        {
            return await httpClientService.PutAsync<UpdateEventDto, EventDto>($"api/v1/events/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update event");
        }

        public async Task DeleteEventAsync(Guid id, CancellationToken ct = default)
        {
            await httpClientService.DeleteAsync($"api/v1/events/{id}", ct);
        }
    }
}
