using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.VatRates;

namespace EventForge.Client.Services
{
    public interface IFinancialService
    {
        // Bank Management
        Task<IEnumerable<BankDto>> GetBanksAsync();
        Task<BankDto?> GetBankAsync(Guid id);
        Task<BankDto> CreateBankAsync(CreateBankDto createDto);
        Task<BankDto> UpdateBankAsync(Guid id, UpdateBankDto updateDto);
        Task DeleteBankAsync(Guid id);

        // VAT Rate Management
        Task<IEnumerable<VatRateDto>> GetVatRatesAsync();
        Task<VatRateDto?> GetVatRateAsync(Guid id);
        Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto);
        Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto);
        Task DeleteVatRateAsync(Guid id);

        // Payment Term Management
        Task<IEnumerable<PaymentTermDto>> GetPaymentTermsAsync();
        Task<PaymentTermDto?> GetPaymentTermAsync(Guid id);
        Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createDto);
        Task<PaymentTermDto> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updateDto);
        Task DeletePaymentTermAsync(Guid id);
    }

    public class FinancialService : IFinancialService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<FinancialService> _logger;

        public FinancialService(IHttpClientService httpClientService, ILogger<FinancialService> logger)
        {
            _httpClientService = httpClientService;
            _logger = logger;
        }

        #region Bank Management

        public async Task<IEnumerable<BankDto>> GetBanksAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<BankDto>>("api/v1/financial/banks") ?? new List<BankDto>();
        }

        public async Task<BankDto?> GetBankAsync(Guid id)
        {
            return await _httpClientService.GetAsync<BankDto>($"api/v1/financial/banks/{id}");
        }

        public async Task<BankDto> CreateBankAsync(CreateBankDto createDto)
        {
            return await _httpClientService.PostAsync<CreateBankDto, BankDto>("api/v1/financial/banks", createDto) ??
                   throw new InvalidOperationException("Failed to create bank");
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

        public async Task<IEnumerable<VatRateDto>> GetVatRatesAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<VatRateDto>>("api/v1/financial/vat-rates") ?? new List<VatRateDto>();
        }

        public async Task<VatRateDto?> GetVatRateAsync(Guid id)
        {
            return await _httpClientService.GetAsync<VatRateDto>($"api/v1/financial/vat-rates/{id}");
        }

        public async Task<VatRateDto> CreateVatRateAsync(CreateVatRateDto createDto)
        {
            return await _httpClientService.PostAsync<CreateVatRateDto, VatRateDto>("api/v1/financial/vat-rates", createDto) ??
                   throw new InvalidOperationException("Failed to create VAT rate");
        }

        public async Task<VatRateDto> UpdateVatRateAsync(Guid id, UpdateVatRateDto updateDto)
        {
            return await _httpClientService.PutAsync<UpdateVatRateDto, VatRateDto>($"api/v1/financial/vat-rates/{id}", updateDto) ??
                   throw new InvalidOperationException("Failed to update VAT rate");
        }

        public async Task DeleteVatRateAsync(Guid id)
        {
            await _httpClientService.DeleteAsync($"api/v1/financial/vat-rates/{id}");
        }

        #endregion

        #region Payment Term Management

        public async Task<IEnumerable<PaymentTermDto>> GetPaymentTermsAsync()
        {
            return await _httpClientService.GetAsync<IEnumerable<PaymentTermDto>>("api/v1/financial/payment-terms") ?? new List<PaymentTermDto>();
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