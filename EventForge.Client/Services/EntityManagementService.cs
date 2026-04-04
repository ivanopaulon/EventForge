using EventForge.DTOs.Common;

namespace EventForge.Client.Services
{
    public interface IEntityManagementService
    {
        // Address Management
        Task<IEnumerable<AddressDto>> GetAddressesAsync(CancellationToken ct = default);
        Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken ct = default);
        Task<AddressDto?> GetAddressAsync(Guid id, CancellationToken ct = default);
        Task<AddressDto> CreateAddressAsync(CreateAddressDto createDto, CancellationToken ct = default);
        Task<AddressDto> UpdateAddressAsync(Guid id, UpdateAddressDto updateDto, CancellationToken ct = default);
        Task DeleteAddressAsync(Guid id, CancellationToken ct = default);

        // Contact Management
        Task<IEnumerable<ContactDto>> GetContactsAsync(CancellationToken ct = default);
        Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
        Task<ContactDto?> GetContactAsync(Guid id, CancellationToken ct = default);
        Task<ContactDto> CreateContactAsync(CreateContactDto createDto, CancellationToken ct = default);
        Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactDto updateDto, CancellationToken ct = default);
        Task DeleteContactAsync(Guid id, CancellationToken ct = default);

        // Reference Management
        Task<IEnumerable<ReferenceDto>> GetReferencesAsync(CancellationToken ct = default);
        Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken ct = default);
        Task<ReferenceDto?> GetReferenceAsync(Guid id, CancellationToken ct = default);
        Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createDto, CancellationToken ct = default);
        Task<ReferenceDto> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateDto, CancellationToken ct = default);
        Task DeleteReferenceAsync(Guid id, CancellationToken ct = default);

        // Classification Node Management
        Task<IEnumerable<ClassificationNodeDto>> GetClassificationNodesAsync(CancellationToken ct = default);
        Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken ct = default);
        Task<IEnumerable<ClassificationNodeDto>> GetChildrenClassificationNodesAsync(Guid parentId, CancellationToken ct = default);
        Task<ClassificationNodeDto?> GetClassificationNodeAsync(Guid id, CancellationToken ct = default);
        Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, CancellationToken ct = default);
        Task<ClassificationNodeDto> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, CancellationToken ct = default);
        Task DeleteClassificationNodeAsync(Guid id, CancellationToken ct = default);
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

        public async Task<IEnumerable<AddressDto>> GetAddressesAsync(CancellationToken ct = default)
        {
            try
            {
                await _loadingDialogService.ShowAsync("Caricamento Indirizzi", "Recupero elenco indirizzi...", true);
                await _loadingDialogService.UpdateProgressAsync(50);

                var result = await _httpClientService.GetAsync<IEnumerable<AddressDto>>("api/v1/entities/addresses", ct) ?? new List<AddressDto>();

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

        public async Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<AddressDto>>($"api/v1/entities/addresses/owner/{ownerId}", ct) ?? new List<AddressDto>();
        }

        public async Task<AddressDto?> GetAddressAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<AddressDto>($"api/v1/entities/addresses/{id}", ct);
        }

        public async Task<AddressDto> CreateAddressAsync(CreateAddressDto createDto, CancellationToken ct = default)
        {
            return await _httpClientService.PostAsync<CreateAddressDto, AddressDto>("api/v1/entities/addresses", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create address");
        }

        public async Task<AddressDto> UpdateAddressAsync(Guid id, UpdateAddressDto updateDto, CancellationToken ct = default)
        {
            return await _httpClientService.PutAsync<UpdateAddressDto, AddressDto>($"api/v1/entities/addresses/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update address");
        }

        public async Task DeleteAddressAsync(Guid id, CancellationToken ct = default)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/addresses/{id}", ct);
        }

        #endregion

        #region Contact Management

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ContactDto>>("api/v1/entities/contacts", ct) ?? new List<ContactDto>();
        }

        public async Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ContactDto>>($"api/v1/entities/contacts/owner/{ownerId}", ct) ?? new List<ContactDto>();
        }

        public async Task<ContactDto?> GetContactAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<ContactDto>($"api/v1/entities/contacts/{id}", ct);
        }

        public async Task<ContactDto> CreateContactAsync(CreateContactDto createDto, CancellationToken ct = default)
        {
            return await _httpClientService.PostAsync<CreateContactDto, ContactDto>("api/v1/entities/contacts", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create contact");
        }

        public async Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactDto updateDto, CancellationToken ct = default)
        {
            return await _httpClientService.PutAsync<UpdateContactDto, ContactDto>($"api/v1/entities/contacts/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update contact");
        }

        public async Task DeleteContactAsync(Guid id, CancellationToken ct = default)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/contacts/{id}", ct);
        }

        #endregion

        #region Reference Management

        public async Task<IEnumerable<ReferenceDto>> GetReferencesAsync(CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ReferenceDto>>("api/v1/entities/references", ct) ?? new List<ReferenceDto>();
        }

        public async Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ReferenceDto>>($"api/v1/entities/references/owner/{ownerId}", ct) ?? new List<ReferenceDto>();
        }

        public async Task<ReferenceDto?> GetReferenceAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<ReferenceDto>($"api/v1/entities/references/{id}", ct);
        }

        public async Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createDto, CancellationToken ct = default)
        {
            return await _httpClientService.PostAsync<CreateReferenceDto, ReferenceDto>("api/v1/entities/references", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create reference");
        }

        public async Task<ReferenceDto> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateDto, CancellationToken ct = default)
        {
            return await _httpClientService.PutAsync<UpdateReferenceDto, ReferenceDto>($"api/v1/entities/references/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update reference");
        }

        public async Task DeleteReferenceAsync(Guid id, CancellationToken ct = default)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/references/{id}", ct);
        }

        #endregion

        #region Classification Node Management

        public async Task<IEnumerable<ClassificationNodeDto>> GetClassificationNodesAsync(CancellationToken ct = default)
        {
            var result = await _httpClientService.GetAsync<PagedResult<ClassificationNodeDto>>("api/v1/entities/classification-nodes?page=1&pageSize=100", ct);
            return result?.Items ?? new List<ClassificationNodeDto>();
        }

        public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>("api/v1/entities/classification-nodes/root", ct) ?? new List<ClassificationNodeDto>();
        }

        public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenClassificationNodesAsync(Guid parentId, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>($"api/v1/entities/classification-nodes/{parentId}/children", ct) ?? new List<ClassificationNodeDto>();
        }

        public async Task<ClassificationNodeDto?> GetClassificationNodeAsync(Guid id, CancellationToken ct = default)
        {
            return await _httpClientService.GetAsync<ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}", ct);
        }

        public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, CancellationToken ct = default)
        {
            return await _httpClientService.PostAsync<CreateClassificationNodeDto, ClassificationNodeDto>("api/v1/entities/classification-nodes", createDto, ct) ??
                   throw new InvalidOperationException("Failed to create classification node");
        }

        public async Task<ClassificationNodeDto> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, CancellationToken ct = default)
        {
            return await _httpClientService.PutAsync<UpdateClassificationNodeDto, ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}", updateDto, ct) ??
                   throw new InvalidOperationException("Failed to update classification node");
        }

        public async Task DeleteClassificationNodeAsync(Guid id, CancellationToken ct = default)
        {
            await _httpClientService.DeleteAsync($"api/v1/entities/classification-nodes/{id}", ct);
        }

        #endregion
    }
}