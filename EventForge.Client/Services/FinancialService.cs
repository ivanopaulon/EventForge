using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Caching.Memory;
using EventForge.Client.Helpers;
using System.Net;

namespace EventForge.Client.Services
{
    public interface IFinancialService
    {
        // Bank Management
        Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 100);
        Task<BankDto?> GetBankAsync(Guid id);
        Task<BankDto> CreateBankAsync(CreateBankDto createDto);
        Task<BankDto> UpdateBankAsync(Guid id, UpdateBankDto updateDto);
        Task DeleteBankAsync(Guid id);

        // VAT Rate Management
        Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 100);
        Task<VatRateDto?> GetVatRateAsync(Guid id);
        Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto);
        Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto);
        Task DeleteVatRateAsync(Guid id);

        // VAT Nature Management
        Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100);
        Task<VatNatureDto?> GetVatNatureAsync(Guid id);
        Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createDto);
        Task<VatNatureDto> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateDto);
        Task DeleteVatNatureAsync(Guid id);

        // Payment Term Management
        Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(int page = 1, int pageSize = 100);
        Task<PaymentTermDto?> GetPaymentTermAsync(Guid id);
        Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createDto);
        Task<PaymentTermDto> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updateDto);
        Task DeletePaymentTermAsync(Guid id);
    }

    public class FinancialService : IFinancialService
    {
        private const string BaseUrl = "api/v1/financial";
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<FinancialService> _logger;
        private readonly ILoadingDialogService _loadingDialogService;
        private readonly IMemoryCache _cache;

        public FinancialService(IHttpClientService httpClientService, ILogger<FinancialService> logger, ILoadingDialogService loadingDialogService, IMemoryCache cache)
        {
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadingDialogService = loadingDialogService ?? throw new ArgumentNullException(nameof(loadingDialogService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #region Bank Management

        public async Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 100)
        {
            var result = await _httpClientService.GetAsync<PagedResult<BankDto>>(
                $"api/v1/financial/banks?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<BankDto>
            {
                Items = new List<BankDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        public async Task<BankDto?> GetBankAsync(Guid id)
        {
            return await _httpClientService.GetAsync<BankDto>($"api/v1/financial/banks/{id}");
        }

        public async Task<BankDto> CreateBankAsync(CreateBankDto createDto)
        {
            try
            {
                await _loadingDialogService.ShowAsync("Creazione Banca", "Configurazione nuovo istituto bancario...", true);
                await _loadingDialogService.UpdateProgressAsync(30);

                await _loadingDialogService.UpdateOperationAsync("Validazione dati bancari...");
                await _loadingDialogService.UpdateProgressAsync(60);

                var result = await _httpClientService.PostAsync<CreateBankDto, BankDto>("api/v1/financial/banks", createDto) ??
                       throw new InvalidOperationException("Failed to create bank");

                await _loadingDialogService.UpdateOperationAsync("Banca creata con successo");
                await _loadingDialogService.UpdateProgressAsync(100);

                await Task.Delay(1000);
                await _loadingDialogService.HideAsync();

                return result;
            }
            catch (Exception)
            {
                await _loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<BankDto> UpdateBankAsync(Guid id, UpdateBankDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateBankDto, BankDto>($"api/v1/financial/banks/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update bank");
        }

        public async Task DeleteBankAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/financial/banks/{id}");
        }

        #endregion

        #region VAT Rate Management

        public async Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 100)
        {
            // Cache ONLY if full list request (page=1, pageSize>=100)
            var isFullListRequest = page == 1 && pageSize >= 100;
            
            if (isFullListRequest)
            {
                // Try cache first
                if (_cache.TryGetValue(CacheHelper.VAT_RATES, out PagedResult<VatRateDto>? cached) && cached != null)
                {
                    _logger.LogDebug("Cache HIT: VAT rates ({Count} items)", cached.TotalCount);
                    return cached;
                }
            }
            
            // Cache miss or paginated request
            _logger.LogDebug("Cache MISS or paginated request: Loading VAT rates from API (page={Page}, size={PageSize})", 
                page, pageSize);
            
            var result = await _httpClientService.GetAsync<PagedResult<VatRateDto>>(
                $"api/v1/financial/vat-rates?page={page}&pageSize={pageSize}");
            
            // Store in cache only for full list
            if (isFullListRequest && result != null)
            {
                _cache.Set(
                    CacheHelper.VAT_RATES, 
                    result, 
                    CacheHelper.GetExtraLongCacheOptions() // 24 hours
                );
                
                _logger.LogInformation(
                    "Cached {Count} VAT rates for {Hours} hours", 
                    result.TotalCount, 
                    CacheHelper.ExtraLongCache.TotalHours
                );
            }
            
            return result ?? new PagedResult<VatRateDto>
            {
                Items = new List<VatRateDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        public async Task<VatRateDto?> GetVatRateAsync(Guid id)
        {
            return await _httpClientService.GetAsync<VatRateDto>($"api/v1/financial/vat-rates/{id}");
        }

        public async Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto)
        {
            var result = await _httpClientService.PostAsync<CreateVatRateDto, VatRateDto>("api/v1/financial/vat-rates", createDto) ??
                   throw new InvalidOperationException("Failed to create VAT rate");
            
            // Invalidate cache
            _cache.Remove(CacheHelper.VAT_RATES);
            _logger.LogDebug("Invalidated VAT rates cache after create");
            
            return result;
        }

        public async Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto)
        {
            var result = await _httpClientService.PutAsync<UpdateVatRateDto, VatRateDto>($"api/v1/financial/vat-rates/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update VAT rate");
            
            // Invalidate cache
            _cache.Remove(CacheHelper.VAT_RATES);
            _logger.LogDebug("Invalidated VAT rates cache after update (ID: {Id})", id);
            
            return result;
        }

        public async Task DeleteVatRateAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/financial/vat-rates/{id}");
            
            // Invalidate cache
            _cache.Remove(CacheHelper.VAT_RATES);
            _logger.LogDebug("Invalidated VAT rates cache after delete (ID: {Id})", id);
        }

        #endregion

        #region VAT Nature Management

        public async Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100)
        {
            var result = await _httpClientService.GetAsync<PagedResult<VatNatureDto>>(
                $"api/v1/financial/vat-natures?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<VatNatureDto>
            {
                Items = new List<VatNatureDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        public async Task<VatNatureDto?> GetVatNatureAsync(Guid id)
        {
            return await _httpClientService.GetAsync<VatNatureDto>($"api/v1/financial/vat-natures/{id}");
        }

        public async Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createDto)
        {
            return await _httpClientService.PostAsync<CreateVatNatureDto, VatNatureDto>("api/v1/financial/vat-natures", createDto) ??
                   throw new InvalidOperationException("Failed to create VAT nature");
        }

        public async Task<VatNatureDto> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateVatNatureDto, VatNatureDto>($"api/v1/financial/vat-natures/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update VAT nature");
        }

        public async Task DeleteVatNatureAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/financial/vat-natures/{id}");
        }

        #endregion

        #region Payment Term Management

        public async Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(int page = 1, int pageSize = 100)
        {
            var result = await _httpClientService.GetAsync<PagedResult<PaymentTermDto>>(
                $"api/v1/financial/payment-terms?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<PaymentTermDto>
            {
                Items = new List<PaymentTermDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        public async Task<PaymentTermDto?> GetPaymentTermAsync(Guid id)
        {
            return await _httpClientService.GetAsync<PaymentTermDto>($"api/v1/financial/payment-terms/{id}");
        }

        public async Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createDto)
        {
            return await _httpClientService.PostAsync<CreatePaymentTermDto, PaymentTermDto>("api/v1/financial/payment-terms", createDto) ??
                   throw new InvalidOperationException("Failed to create payment term");
        }

        public async Task<PaymentTermDto> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdatePaymentTermDto, PaymentTermDto>($"api/v1/financial/payment-terms/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update payment term");
        }

        public async Task DeletePaymentTermAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/financial/payment-terms/{id}");
        }

        #endregion
    }
}