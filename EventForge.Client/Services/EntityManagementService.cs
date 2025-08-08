using EventForge.DTOs.Common;

namespace EventForge.Client.Services
{
    public interface IEntityManagementService
    {
        // Address Management
        Task<IEnumerable<AddressDto>> GetAddressesAsync();
        Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId);
        Task<AddressDto?> GetAddressAsync(Guid id);
        Task<AddressDto> CreateAddressAsync(CreateAddressDto createDto);
        Task<AddressDto> UpdateAddressAsync(Guid id, UpdateAddressDto updateDto);
        Task DeleteAddressAsync(Guid id);

        // Contact Management
        Task<IEnumerable<ContactDto>> GetContactsAsync();
        Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId);
        Task<ContactDto?> GetContactAsync(Guid id);
        Task<ContactDto> CreateContactAsync(CreateContactDto createDto);
        Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactDto updateDto);
        Task DeleteContactAsync(Guid id);

        // Classification Node Management
        Task<IEnumerable<ClassificationNodeDto>> GetClassificationNodesAsync();
        Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync();
        Task<ClassificationNodeDto?> GetClassificationNodeAsync(Guid id);
        Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto);
        Task<ClassificationNodeDto> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto);
        Task DeleteClassificationNodeAsync(Guid id);
    }

    public class EntityManagementService : IEntityManagementService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<EntityManagementService> _logger;
        private readonly ILoadingDialogService _loadingDialogService;

        public EntityManagementService(IHttpClientService httpClientService, ILogger<EntityManagementService> logger, ILoadingDialogService loadingDialogService)
        {
            _httpClientService = httpClientService;
            _logger = logger;
            _loadingDialogService = loadingDialogService;
        }

        #region Address Management

        public async Task<IEnumerable<AddressDto>> GetAddressesAsync()
        {
            try
            {
                await _loadingDialogService.ShowAsync("Caricamento Indirizzi", "Recupero elenco indirizzi...", true);
                await _loadingDialogService.UpdateProgressAsync(50);
                
                var result = await _httpClientService.GetAsync<IEnumerable<AddressDto>>("api/v1/entities/addresses") ?? new List<AddressDto>();
                
                await _loadingDialogService.UpdateOperationAsync("Indirizzi caricati");
                await _loadingDialogService.UpdateProgressAsync(100);
                
                await Task.Delay(500);
                await _loadingDialogService.HideAsync();
                
                return result;
            }
            catch (Exception)
            {
                await _loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId)
        {
            return await _httpClientService.GetAsync<IEnumerable<AddressDto>>($"api/v1/entities/addresses/owner/{ownerId}") ?? new List<AddressDto>();
        }

        public async Task<AddressDto?> GetAddressAsync(Guid id)
        {
            return await _httpClientService.GetAsync<AddressDto>($"api/v1/entities/addresses/{id}");
        }

        public async Task<AddressDto> CreateAddressAsync(CreateAddressDto createDto)
        {
            return await _httpClientService.PostAsync<CreateAddressDto, AddressDto>("api/v1/entities/addresses", createDto) ??
                   throw new InvalidOperationException("Failed to create address");
        }

        public async Task<AddressDto> UpdateAddressAsync(Guid id, UpdateAddressDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateAddressDto, AddressDto>($"api/v1/entities/addresses/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update address");
        }

        public async Task DeleteAddressAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/addresses/{id}");
        }

        #endregion

        #region Contact Management

        public async Task<IEnumerable<ContactDto>> GetContactsAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<ContactDto>>("api/v1/entities/contacts") ?? new List<ContactDto>();
        }

        public async Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId)
        {
            return await _httpClientService.GetAsync<IEnumerable<ContactDto>>($"api/v1/entities/contacts/owner/{ownerId}") ?? new List<ContactDto>();
        }

        public async Task<ContactDto?> GetContactAsync(Guid id)
        {
            return await _httpClientService.GetAsync<ContactDto>($"api/v1/entities/contacts/{id}");
        }

        public async Task<ContactDto> CreateContactAsync(CreateContactDto createDto)
        {
            return await _httpClientService.PostAsync<CreateContactDto, ContactDto>("api/v1/entities/contacts", createDto) ??
                   throw new InvalidOperationException("Failed to create contact");
        }

        public async Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateContactDto, ContactDto>($"api/v1/entities/contacts/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update contact");
        }

        public async Task DeleteContactAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/contacts/{id}");
        }

        #endregion

        #region Classification Node Management

        public async Task<IEnumerable<ClassificationNodeDto>> GetClassificationNodesAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>("api/v1/entities/classification-nodes") ?? new List<ClassificationNodeDto>();
        }

        public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>("api/v1/entities/classification-nodes/root") ?? new List<ClassificationNodeDto>();
        }

        public async Task<ClassificationNodeDto?> GetClassificationNodeAsync(Guid id)
        {
            return await _httpClientService.GetAsync<ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}");
        }

        public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto)
        {
            return await _httpClientService.PostAsync<CreateClassificationNodeDto, ClassificationNodeDto>("api/v1/entities/classification-nodes", createDto) ??
                   throw new InvalidOperationException("Failed to create classification node");
        }

        public async Task<ClassificationNodeDto> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateClassificationNodeDto, ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update classification node");
        }

        public async Task DeleteClassificationNodeAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/classification-nodes/{id}");
        }

        #endregion
    }
}