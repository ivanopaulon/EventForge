using EventForge.DTOs.Common;
using EventForge.DTOs.Events;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Service interface for event management operations.
    /// Maps to /api/v1/events endpoints in EventsController.
    /// </summary>
    public interface IEventService
    {
        Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default);
        Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken ct = default);
        Task<EventDto> CreateEventAsync(CreateEventDto createDto, CancellationToken ct = default);
        Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto, CancellationToken ct = default);
        Task DeleteEventAsync(Guid id, CancellationToken ct = default);
    }

    public class EventService : IEventService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<EventService> _logger;

        public EventService(IHttpClientService httpClientService, ILogger<EventService> logger)
        {
            _httpClientService = httpClientService;
            _logger = logger;
        }

        public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var url = $"api/v1/events?page={page}&pageSize={pageSize}";
            return await _httpClientService.GetAsync<PagedResult<EventDto>>(url, ct) ??
                new PagedResult<EventDto> { Items = new List<EventDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<EventDto>($"api/v1/events/{id}", ct);
        }

        public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<EventDetailDto>($"api/v1/events/{id}/details", ct);
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createDto, CancellationToken ct = default)
        {
            return await _httpClientService.PostAsync<CreateEventDto, EventDto>("api/v1/events", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create event");
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto, CancellationToken ct = default)
        {
            return await _httpClientService.PutAsync<UpdateEventDto, EventDto>($"api/v1/events/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update event");
        }

        public async Task DeleteEventAsync(Guid id, CancellationToken ct = default)
        {
            await _httpClientService.DeleteAsync($"api/v1/events/{id}", ct);
        }
    }
}
