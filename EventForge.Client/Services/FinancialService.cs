using EventForge.Client.Helpers;
using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Client.Services
{
    public interface IFinancialService
    {
        // Bank Management
        Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
        Task<BankDto?> GetBankAsync(Guid id, CancellationToken ct = default);
        Task<BankDto> CreateBankAsync(CreateBankDto createDto, CancellationToken ct = default);
        Task<BankDto> UpdateBankAsync(Guid id, UpdateBankDto updateDto, CancellationToken ct = default);
        Task DeleteBankAsync(Guid id, CancellationToken ct = default);

        // VAT Rate Management
        Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
        Task<VatRateDto?> GetVatRateAsync(Guid id, CancellationToken ct = default);
        Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto, CancellationToken ct = default);
        Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto, CancellationToken ct = default);
        Task DeleteVatRateAsync(Guid id, CancellationToken ct = default);

        // VAT Nature Management
        Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
        Task<VatNatureDto?> GetVatNatureAsync(Guid id, CancellationToken ct = default);
        Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createDto, CancellationToken ct = default);
        Task<VatNatureDto> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateDto, CancellationToken ct = default);
        Task DeleteVatNatureAsync(Guid id, CancellationToken ct = default);

        // Payment Term Management
        Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default);
        Task<PaymentTermDto?> GetPaymentTermAsync(Guid id, CancellationToken ct = default);
        Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createDto, CancellationToken ct = default);
        Task<PaymentTermDto> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updateDto, CancellationToken ct = default);
        Task DeletePaymentTermAsync(Guid id, CancellationToken ct = default);
    }

    public class FinancialService(
        IHttpClientService httpClientService,
        ILogger<FinancialService> logger,
        ILoadingDialogService loadingDialogService,
        IMemoryCache cache) : IFinancialService
    {
        private const string BaseUrl = "api/v1/financial";

        #region Bank Management

        public async Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.GetAsync<PagedResult<BankDto>>(
                    $"api/v1/financial/banks?page={page}&pageSize={pageSize}", ct);
                return result ?? new PagedResult<BankDto>
                {
                    Items = [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving banks (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        public async Task<BankDto?> GetBankAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<BankDto>($"api/v1/financial/banks/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving bank {Id}", id);
                throw;
            }
        }

        public async Task<BankDto> CreateBankAsync(CreateBankDto createDto, CancellationToken ct = default)
        {
            try
            {
                await loadingDialogService.ShowAsync("Creazione Banca", "Configurazione nuovo istituto bancario...", true);
                await loadingDialogService.UpdateProgressAsync(30);

                await loadingDialogService.UpdateOperationAsync("Validazione dati bancari...");
                await loadingDialogService.UpdateProgressAsync(60);

                var result = await httpClientService.PostAsync<CreateBankDto, BankDto>("api/v1/financial/banks", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create bank");

                await loadingDialogService.UpdateOperationAsync("Banca creata con successo");
                await loadingDialogService.UpdateProgressAsync(100);

                await Task.Delay(1000);
                await loadingDialogService.HideAsync();

                return result;
            }
            catch (Exception)
            {
                await loadingDialogService.HideAsync();
                throw;
            }
        }

        public async Task<BankDto> UpdateBankAsync(Guid id, UpdateBankDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateBankDto, BankDto>($"api/v1/financial/banks/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update bank");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating bank {Id}", id);
                throw;
            }
        }

        public async Task DeleteBankAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/financial/banks/{id}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting bank {Id}", id);
                throw;
            }
        }

        #endregion

        #region VAT Rate Management

        public async Task<PagedResult<VatRateDto>> GetVatRatesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            try
            {
                // Cache ONLY if full list request (page=1, pageSize>=100)
                var isFullListRequest = page == 1 && pageSize >= 100;

                if (isFullListRequest)
                {
                    // Try cache first
                    if (cache.TryGetValue(CacheHelper.VAT_RATES, out PagedResult<VatRateDto>? cached) && cached is not null)
                    {
                        logger.LogDebug("Cache HIT: VAT rates ({Count} items)", cached.TotalCount);
                        return cached;
                    }
                }

                // Cache miss or paginated request
                logger.LogDebug("Cache MISS or paginated request: Loading VAT rates from API (page={Page}, size={PageSize})",
                    page, pageSize);

                var result = await httpClientService.GetAsync<PagedResult<VatRateDto>>(
                    $"api/v1/financial/vat-rates?page={page}&pageSize={pageSize}", ct);

                // Store in cache only for full list
                if (isFullListRequest && result is not null)
                {
                    cache.Set(
                        CacheHelper.VAT_RATES,
                        result,
                        CacheHelper.GetExtraLongCacheOptions() // 24 hours
                    );

                    logger.LogInformation(
                        "Cached {Count} VAT rates for {Hours} hours",
                        result.TotalCount,
                        CacheHelper.ExtraLongCache.TotalHours
                    );
                }

                return result ?? new PagedResult<VatRateDto>
                {
                    Items = [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving VAT rates (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        public async Task<VatRateDto?> GetVatRateAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<VatRateDto>($"api/v1/financial/vat-rates/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving VAT rate {Id}", id);
                throw;
            }
        }

        public async Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto, CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.PostAsync<CreateVatRateDto, VatRateDto>("api/v1/financial/vat-rates", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create VAT rate");

                // Invalidate cache
                cache.Remove(CacheHelper.VAT_RATES);
                logger.LogDebug("Invalidated VAT rates cache after create");

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating VAT rate");
                throw;
            }
        }

        public async Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto, CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.PutAsync<UpdateVatRateDto, VatRateDto>($"api/v1/financial/vat-rates/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update VAT rate");

                // Invalidate cache
                cache.Remove(CacheHelper.VAT_RATES);
                logger.LogDebug("Invalidated VAT rates cache after update (ID: {Id})", id);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating VAT rate {Id}", id);
                throw;
            }
        }

        public async Task DeleteVatRateAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/financial/vat-rates/{id}", ct);

                // Invalidate cache
                cache.Remove(CacheHelper.VAT_RATES);
                logger.LogDebug("Invalidated VAT rates cache after delete (ID: {Id})", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting VAT rate {Id}", id);
                throw;
            }
        }

        #endregion

        #region VAT Nature Management

        public async Task<PagedResult<VatNatureDto>> GetVatNaturesAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.GetAsync<PagedResult<VatNatureDto>>(
                    $"api/v1/financial/vat-natures?page={page}&pageSize={pageSize}", ct);
                return result ?? new PagedResult<VatNatureDto>
                {
                    Items = [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving VAT natures (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        public async Task<VatNatureDto?> GetVatNatureAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<VatNatureDto>($"api/v1/financial/vat-natures/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving VAT nature {Id}", id);
                throw;
            }
        }

        public async Task<VatNatureDto> CreateVatNatureAsync(CreateVatNatureDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreateVatNatureDto, VatNatureDto>("api/v1/financial/vat-natures", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create VAT nature");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating VAT nature");
                throw;
            }
        }

        public async Task<VatNatureDto> UpdateVatNatureAsync(Guid id, UpdateVatNatureDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdateVatNatureDto, VatNatureDto>($"api/v1/financial/vat-natures/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update VAT nature");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating VAT nature {Id}", id);
                throw;
            }
        }

        public async Task DeleteVatNatureAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/financial/vat-natures/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting VAT nature {Id}", id);
                throw;
            }
        }

        #endregion

        #region Payment Term Management

        public async Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        {
            try
            {
                var result = await httpClientService.GetAsync<PagedResult<PaymentTermDto>>(
                    $"api/v1/financial/payment-terms?page={page}&pageSize={pageSize}", ct);
                return result ?? new PagedResult<PaymentTermDto>
                {
                    Items = [],
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving payment terms (page={Page}, pageSize={PageSize})", page, pageSize);
                throw;
            }
        }

        public async Task<PaymentTermDto?> GetPaymentTermAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.GetAsync<PaymentTermDto>($"api/v1/financial/payment-terms/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving payment term {Id}", id);
                throw;
            }
        }

        public async Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PostAsync<CreatePaymentTermDto, PaymentTermDto>("api/v1/financial/payment-terms", createDto, ct) ??
                       throw new InvalidOperationException("Failed to create payment term");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating payment term");
                throw;
            }
        }

        public async Task<PaymentTermDto> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updateDto, CancellationToken ct = default)
        {
            try
            {
                return await httpClientService.PutAsync<UpdatePaymentTermDto, PaymentTermDto>($"api/v1/financial/payment-terms/{id}", updateDto, ct) ??
                       throw new InvalidOperationException("Failed to update payment term");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating payment term {Id}", id);
                throw;
            }
        }

        public async Task DeletePaymentTermAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                await httpClientService.DeleteAsync($"api/v1/financial/payment-terms/{id}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting payment term {Id}", id);
                throw;
            }
        }

        #endregion
    }
}