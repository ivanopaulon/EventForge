using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services
{
    public interface IBusinessPartyService
    {
        // BusinessParty Management
        Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20);
        Task<BusinessPartyDto?> GetBusinessPartyAsync(Guid id);
        Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(BusinessPartyType partyType);
        Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, BusinessPartyType? partyType = null, int pageSize = 50);
        Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesWithBirthdayAsync();
        Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createDto);
        Task<BusinessPartyDto> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateDto);
        Task DeleteBusinessPartyAsync(Guid id);

        // BusinessPartyAccounting Management
        Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId);

        // Full Detail Aggregated Query
        /// <summary>
        /// Recupera tutti i dettagli completi di un BusinessParty in una singola chiamata.
        /// </summary>
        /// <param name="id">BusinessParty ID</param>
        /// <param name="includeInactive">Include contatti/indirizzi inattivi</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>DTO aggregato con tutti i dati, null se non trovato</returns>
        Task<BusinessPartyFullDetailDto?> GetFullDetailAsync(Guid id, bool includeInactive = false, CancellationToken ct = default);

        // BusinessParty Documents
        Task<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>?> GetBusinessPartyDocumentsAsync(
            Guid businessPartyId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            Guid? documentTypeId = null,
            string? searchNumber = null,
            EventForge.DTOs.Common.ApprovalStatus? approvalStatus = null,
            int page = 1,
            int pageSize = 20);

        // BusinessParty Product Analysis
        Task<PagedResult<BusinessPartyProductAnalysisDto>?> GetBusinessPartyProductAnalysisAsync(
            Guid businessPartyId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? type = null,
            int? topN = null,
            int page = 1,
            int pageSize = 20,
            string? sortBy = null,
            bool sortDescending = true);

        // Supplier Product Bulk Operations
        Task<List<SupplierProductPreview>?> PreviewBulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request);
        Task<BulkUpdateResult?> BulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request);
    }

    public class BusinessPartyService(
        IHttpClientService httpClientService,
        ILogger<BusinessPartyService> logger) : IBusinessPartyService
    {
        private const string BaseUrl = "api/v1/businessparties";

        #region BusinessParty Management

        public async Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20)
        {
            return await httpClientService.GetAsync<PagedResult<BusinessPartyDto>>($"api/v1/businessparties?page={page}&pageSize={pageSize}")
                ?? new PagedResult<BusinessPartyDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }

        public async Task<BusinessPartyDto?> GetBusinessPartyAsync(Guid id)
        {
            return await httpClientService.GetAsync<BusinessPartyDto>($"api/v1/businessparties/{id}");
        }

        public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(BusinessPartyType partyType)
        {
            return await httpClientService.GetAsync<IEnumerable<BusinessPartyDto>>($"api/v1/businessparties/by-type/{partyType}")
                ?? [];
        }

        public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesWithBirthdayAsync()
        {
            return await httpClientService.GetAsync<IEnumerable<BusinessPartyDto>>("api/v1/businessparties/with-birthdays")
                ?? [];
        }

        public async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, BusinessPartyType? partyType = null, int pageSize = 50)
        {
            var query = $"api/v1/businessparties/search?searchTerm={Uri.EscapeDataString(searchTerm)}&pageSize={pageSize}";
            if (partyType.HasValue)
            {
                query += $"&partyType={partyType.Value}";
            }
            return await httpClientService.GetAsync<IEnumerable<BusinessPartyDto>>(query)
                ?? [];
        }

        public async Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createDto)
        {
            var result = await httpClientService.PostAsync<CreateBusinessPartyDto, BusinessPartyDto>("api/v1/businessparties", createDto);
            return result ?? throw new InvalidOperationException("Failed to create business party");
        }

        public async Task<BusinessPartyDto> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateDto)
        {
            return await httpClientService.PutAsync<UpdateBusinessPartyDto, BusinessPartyDto>($"api/v1/businessparties/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update business party");
        }

        public async Task DeleteBusinessPartyAsync(Guid id)
        {
            await httpClientService.DeleteAsync($"api/v1/businessparties/{id}");
        }

        #endregion

        #region BusinessPartyAccounting Management

        public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId)
        {
            return await httpClientService.GetAsync<BusinessPartyAccountingDto>($"api/v1/businessparties/{businessPartyId}/accounting");
        }

        public async Task<BusinessPartyFullDetailDto?> GetFullDetailAsync(
            Guid id,
            bool includeInactive = false,
            CancellationToken ct = default)
        {
            try
            {
                var url = $"api/v1/businessparties/{id}/full-detail?includeInactive={includeInactive}";
                logger.LogInformation("Fetching full detail for BusinessParty {Id} from {Url}", id, url);

                return await httpClientService.GetAsync<BusinessPartyFullDetailDto>(url, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving full detail for BusinessParty {Id}", id);
                throw;
            }
        }

        #endregion

        #region BusinessParty Documents

        public async Task<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>?> GetBusinessPartyDocumentsAsync(
            Guid businessPartyId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            Guid? documentTypeId = null,
            string? searchNumber = null,
            EventForge.DTOs.Common.ApprovalStatus? approvalStatus = null,
            int page = 1,
            int pageSize = 20)
        {
            var query = $"api/v1/businessparties/{businessPartyId}/documents?page={page}&pageSize={pageSize}";

            if (fromDate.HasValue)
            {
                query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            }

            if (toDate.HasValue)
            {
                query += $"&toDate={toDate.Value:yyyy-MM-dd}";
            }

            if (documentTypeId.HasValue)
            {
                query += $"&documentTypeId={documentTypeId.Value}";
            }

            if (!string.IsNullOrWhiteSpace(searchNumber))
            {
                query += $"&searchNumber={Uri.EscapeDataString(searchNumber)}";
            }

            if (approvalStatus.HasValue)
            {
                query += $"&approvalStatus={approvalStatus.Value}";
            }

            return await httpClientService.GetAsync<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>>(query);
        }

        #endregion

        #region BusinessParty Product Analysis

        public async Task<PagedResult<BusinessPartyProductAnalysisDto>?> GetBusinessPartyProductAnalysisAsync(
            Guid businessPartyId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? type = null,
            int? topN = null,
            int page = 1,
            int pageSize = 20,
            string? sortBy = null,
            bool sortDescending = true)
        {
            var query = $"api/v1/businessparties/{businessPartyId}/product-analysis?page={page}&pageSize={pageSize}&sortDescending={sortDescending}";

            if (fromDate.HasValue)
            {
                query += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            }

            if (toDate.HasValue)
            {
                query += $"&toDate={toDate.Value:yyyy-MM-dd}";
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query += $"&type={Uri.EscapeDataString(type)}";
            }

            if (topN.HasValue)
            {
                query += $"&topN={topN.Value}";
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                query += $"&sortBy={Uri.EscapeDataString(sortBy)}";
            }

            return await httpClientService.GetAsync<PagedResult<BusinessPartyProductAnalysisDto>>(query);
        }

        #endregion

        #region Supplier Product Bulk Operations

        public async Task<List<SupplierProductPreview>?> PreviewBulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request)
        {
            return await httpClientService.PostAsync<BulkUpdateSupplierProductsRequest, List<SupplierProductPreview>>(
                $"api/v1/businessparties/{supplierId}/products/bulk-preview",
                request);
        }

        public async Task<BulkUpdateResult?> BulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request)
        {
            return await httpClientService.PostAsync<BulkUpdateSupplierProductsRequest, BulkUpdateResult>(
                $"api/v1/businessparties/{supplierId}/products/bulk-update",
                request);
        }

        #endregion
    }
}
