using Prym.DTOs.Common;

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

    public class EntityManagementService(
        IHttpClientService httpClientService,
        ILogger<EntityManagementService> logger,
        ILoadingDialogService loadingDialogService) : IEntityManagementService
    {

        #region Address Management

        public async Task<IEnumerable<AddressDto>> GetAddressesAsync(CancellationToken ct = default)
        {
            try
            {
                await loadingDialogService.ShowAsync("Caricamento Indirizzi", "Recupero elenco indirizzi...", true);
                await loadingDialogService.UpdateProgressAsync(50);

                var result = await httpClientService.GetAsync<IEnumerable<AddressDto>>("api/v1/entities/addresses", ct) ?? [];

                await loadingDialogService.UpdateOperationAsync("Indirizzi caricati");
                await loadingDialogService.UpdateProgressAsync(100);

                await Task.Delay(500);
                await loadingDialogService.HideAsync();

                return result;
            }
            catch (Exception)
            {
                await loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<AddressDto>>($"api/v1/entities/addresses/owner/{ownerId}", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving addresses for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<AddressDto?> GetAddressAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<AddressDto>($"api/v1/entities/addresses/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving address {Id}", id);
                throw;
            }
        }

        public async Task<AddressDto> CreateAddressAsync(CreateAddressDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateAddressDto, AddressDto>("api/v1/entities/addresses", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create address");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating address");
                throw;
            }
        }

        public async Task<AddressDto> UpdateAddressAsync(Guid id, UpdateAddressDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateAddressDto, AddressDto>($"api/v1/entities/addresses/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update address");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating address {Id}", id);
                throw;
            }
        }

        public async Task DeleteAddressAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/entities/addresses/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting address {Id}", id);
                throw;
            }
        }

        #endregion

        #region Contact Management

        public async Task<IEnumerable<ContactDto>> GetContactsAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ContactDto>>("api/v1/entities/contacts", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving contacts");
                throw;
            }
        }

        public async Task<IEnumerable<ContactDto>> GetContactsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ContactDto>>($"api/v1/entities/contacts/owner/{ownerId}", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving contacts for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<ContactDto?> GetContactAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<ContactDto>($"api/v1/entities/contacts/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving contact {Id}", id);
                throw;
            }
        }

        public async Task<ContactDto> CreateContactAsync(CreateContactDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateContactDto, ContactDto>("api/v1/entities/contacts", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create contact");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating contact");
                throw;
            }
        }

        public async Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateContactDto, ContactDto>($"api/v1/entities/contacts/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update contact");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating contact {Id}", id);
                throw;
            }
        }

        public async Task DeleteContactAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/entities/contacts/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting contact {Id}", id);
                throw;
            }
        }

        #endregion

        #region Reference Management

        public async Task<IEnumerable<ReferenceDto>> GetReferencesAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ReferenceDto>>("api/v1/entities/references", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving references");
                throw;
            }
        }

        public async Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ReferenceDto>>($"api/v1/entities/references/owner/{ownerId}", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving references for owner {OwnerId}", ownerId);
                throw;
            }
        }

        public async Task<ReferenceDto?> GetReferenceAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<ReferenceDto>($"api/v1/entities/references/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving reference {Id}", id);
                throw;
            }
        }

        public async Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateReferenceDto, ReferenceDto>("api/v1/entities/references", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create reference");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating reference");
                throw;
            }
        }

        public async Task<ReferenceDto> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateReferenceDto, ReferenceDto>($"api/v1/entities/references/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update reference");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating reference {Id}", id);
                throw;
            }
        }

        public async Task DeleteReferenceAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/entities/references/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting reference {Id}", id);
                throw;
            }
        }

        #endregion

        #region Classification Node Management

        public async Task<IEnumerable<ClassificationNodeDto>> GetClassificationNodesAsync(CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.GetAsync<PagedResult<ClassificationNodeDto>>("api/v1/entities/classification-nodes?page=1&pageSize=100", ct);
                return result?.Items ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving classification nodes");
                throw;
            }
        }

        public async Task<IEnumerable<ClassificationNodeDto>> GetRootClassificationNodesAsync(CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>("api/v1/entities/classification-nodes/root", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving root classification nodes");
                throw;
            }
        }

        public async Task<IEnumerable<ClassificationNodeDto>> GetChildrenClassificationNodesAsync(Guid parentId, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<IEnumerable<ClassificationNodeDto>>($"api/v1/entities/classification-nodes/{parentId}/children", ct) ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving children classification nodes for parent {ParentId}", parentId);
                throw;
            }
        }

        public async Task<ClassificationNodeDto?> GetClassificationNodeAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving classification node {Id}", id);
                throw;
            }
        }

        public async Task<ClassificationNodeDto> CreateClassificationNodeAsync(CreateClassificationNodeDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateClassificationNodeDto, ClassificationNodeDto>("api/v1/entities/classification-nodes", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create classification node");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating classification node");
                throw;
            }
        }

        public async Task<ClassificationNodeDto> UpdateClassificationNodeAsync(Guid id, UpdateClassificationNodeDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateClassificationNodeDto, ClassificationNodeDto>($"api/v1/entities/classification-nodes/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update classification node");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating classification node {Id}", id);
                throw;
            }
        }

        public async Task DeleteClassificationNodeAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/entities/classification-nodes/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting classification node {Id}", id);
                throw;
            }
        }

        #endregion
    }
}