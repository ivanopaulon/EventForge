using Prym.DTOs.Common;
using Prym.DTOs.Events;

namespace Prym.Web.Services
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

    public class EventService(
        IHttpClientService httpClientService,
        ILogger<EventService> logger) : IEventService
    {

        public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            try
            {
                var url = $"api/v1/events?page={page}&pageSize={pageSize}";
                return await httpClientService.GetAsync<PagedResult<EventDto>>(url, ct) ??
                    new PagedResult<EventDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting events (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        public async Task<IEnumerable<EventDto>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            try
            {
                var url = $"api/v1/events/date-range?startDate={startDate:O}&endDate={endDate:O}&page=1&pageSize=1000";
                var result = await httpClientService.GetAsync<PagedResult<EventDto>>(url, ct);
                return result?.Items ?? Enumerable.Empty<EventDto>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting events by date range ({StartDate} - {EndDate})", startDate, endDate);
                throw;
            }
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<EventDto>($"api/v1/events/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting event {Id}", id);
                throw;
            }
        }

        public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<EventDetailDto>($"api/v1/events/{id}/details", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting event detail {Id}", id);
                throw;
            }
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateEventDto, EventDto>("api/v1/events", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create event");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating event");
                throw;
            }
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateEventDto, EventDto>($"api/v1/events/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update event");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating event {Id}", id);
                throw;
            }
        }

        public async Task DeleteEventAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/events/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting event {Id}", id);
                throw;
            }
        }
    }
}
