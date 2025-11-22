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
        Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createDto);
        Task<BusinessPartyDto> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateDto);
        Task DeleteBusinessPartyAsync(Guid id);

        // BusinessPartyAccounting Management
        Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId);

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

    public class BusinessPartyService : IBusinessPartyService
    {
        private const string BaseUrl = "api/v1/business-parties";
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

        public async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, BusinessPartyType? partyType = null, int pageSize = 50)
        {
            var query = $"api/v1/businessparties/search?searchTerm={Uri.EscapeDataString(searchTerm)}&pageSize={pageSize}";
            if (partyType.HasValue)
            {
                query += $"&partyType={partyType.Value}";
            }
            return await _httpClientService.GetAsync<IEnumerable<BusinessPartyDto>>(query)
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

        #region BusinessPartyAccounting Management

        public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId)
        {
            return await _httpClientService.GetAsync<BusinessPartyAccountingDto>($"api/v1/businessparties/{businessPartyId}/accounting");
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

            return await _httpClientService.GetAsync<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>>(query);
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

            return await _httpClientService.GetAsync<PagedResult<BusinessPartyProductAnalysisDto>>(query);
        }

        #endregion

        #region Supplier Product Bulk Operations

        public async Task<List<SupplierProductPreview>?> PreviewBulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request)
        {
            return await _httpClientService.PostAsync<BulkUpdateSupplierProductsRequest, List<SupplierProductPreview>>(
                $"api/v1/businessparties/{supplierId}/products/bulk-preview",
                request);
        }

        public async Task<BulkUpdateResult?> BulkUpdateSupplierProductsAsync(Guid supplierId, BulkUpdateSupplierProductsRequest request)
        {
            return await _httpClientService.PostAsync<BulkUpdateSupplierProductsRequest, BulkUpdateResult>(
                $"api/v1/businessparties/{supplierId}/products/bulk-update",
                request);
        }

        #endregion
    }
}
