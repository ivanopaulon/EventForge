using EventForge.DTOs.Events;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Service interface for event management operations.
    /// Maps to /api/v1/events endpoints in EventsController.
    /// </summary>
    public interface IEventService
    {
        Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20);
        Task<EventDto?> GetEventByIdAsync(Guid id);
        Task<EventDetailDto?> GetEventDetailAsync(Guid id);
        Task<EventDto> CreateEventAsync(CreateEventDto createDto);
        Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto);
        Task DeleteEventAsync(Guid id);
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

        public async Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var url = $"api/v1/events?page={page}&pageSize={pageSize}";
                return await _httpClientService.GetAsync<PagedResult<EventDto>>(url) ?? 
                    new PagedResult<EventDto> { Items = new List<EventDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                throw;
            }
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid id)
        {
            try
            {
                return await _httpClientService.GetAsync<EventDto>($"api/v1/events/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event {EventId}", id);
                throw;
            }
        }

        public async Task<EventDetailDto?> GetEventDetailAsync(Guid id)
        {
            try
            {
                return await _httpClientService.GetAsync<EventDetailDto>($"api/v1/events/{id}/details");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event details for {EventId}", id);
                throw;
            }
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto createDto)
        {
            try
            {
                return await _httpClientService.PostAsync<CreateEventDto, EventDto>("api/v1/events", createDto) ??
                       throw new InvalidOperationException("Failed to create event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }

        public async Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto)
        {
            try
            {
                return await _httpClientService.PutAsync<UpdateEventDto, EventDto>($"api/v1/events/{id}", updateDto) ??
                       throw new InvalidOperationException("Failed to update event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                throw;
            }
        }

        public async Task DeleteEventAsync(Guid id)
        {
            try
            {
                await _httpClientService.DeleteAsync($"api/v1/events/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                throw;
            }
        }
    }
}
