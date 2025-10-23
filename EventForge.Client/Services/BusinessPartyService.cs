using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Client.Services
{
    public interface IBusinessPartyService
    {
        // BusinessParty Management
        Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20);
        Task<BusinessPartyDto?> GetBusinessPartyAsync(Guid id);
        Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(BusinessPartyType partyType);
        Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createDto);
        Task<BusinessPartyDto> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateDto);
        Task DeleteBusinessPartyAsync(Guid id);
    }

    public class BusinessPartyService : IBusinessPartyService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<BusinessPartyService> _logger;
        private readonly ILoadingDialogService _loadingDialogService; // kept for DI compatibility

        public BusinessPartyService(
            IHttpClientService httpClientService,
            ILogger<BusinessPartyService> logger,
            ILoadingDialogService loadingDialogService)
        {
            _httpClientService = httpClientService;
            _logger = logger;
            _loadingDialogService = loadingDialogService;
        }

        #region BusinessParty Management

        public async Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20)
        {
            return await _httpClientService.GetAsync<PagedResult<BusinessPartyDto>>($"api/v1/businessparties?page={page}&pageSize={pageSize}")
                ?? new PagedResult<BusinessPartyDto> { Items = new List<BusinessPartyDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }

        public async Task<BusinessPartyDto?> GetBusinessPartyAsync(Guid id)
        {
            return await _httpClientService.GetAsync<BusinessPartyDto>($"api/v1/businessparties/{id}");
        }

        public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(BusinessPartyType partyType)
        {
            return await _httpClientService.GetAsync<IEnumerable<BusinessPartyDto>>($"api/v1/businessparties/by-type/{partyType}")
                ?? new List<BusinessPartyDto>();
        }

        public async Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createDto)
        {
            // Keep service free of UI concerns. Pages/components should control loading UI using page-level flags or the global loading dialog service directly if needed.
            var result = await _httpClientService.PostAsync<CreateBusinessPartyDto, BusinessPartyDto>("api/v1/businessparties", createDto);
            return result ?? throw new InvalidOperationException("Failed to create business party");
        }

        public async Task<BusinessPartyDto> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateBusinessPartyDto, BusinessPartyDto>($"api/v1/businessparties/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update business party");
        }

        public async Task DeleteBusinessPartyAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/businessparties/{id}");
        }

        #endregion
    }
}
