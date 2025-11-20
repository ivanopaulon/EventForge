using EventForge.DTOs.Sales;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for payment methods.
/// </summary>
public class PaymentMethodService : IPaymentMethodService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PaymentMethodService> _logger;
    private const string BaseUrl = "api/v1/payment-methods";

    public PaymentMethodService(IHttpClientService httpClientService, ILogger<PaymentMethodService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PaymentMethodDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<PaymentMethodDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all payment methods");
            return null;
        }
    }

    public async Task<List<PaymentMethodDto>?> GetActiveAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<PaymentMethodDto>>($"{BaseUrl}/active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active payment methods");
            return null;
        }
    }

    public async Task<PaymentMethodDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<PaymentMethodDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {Id}", id);
            return null;
        }
    }

    public async Task<PaymentMethodDto?> CreateAsync(CreatePaymentMethodDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreatePaymentMethodDto, PaymentMethodDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return null;
        }
    }

    public async Task<PaymentMethodDto?> UpdateAsync(Guid id, UpdatePaymentMethodDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdatePaymentMethodDto, PaymentMethodDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {Id}", id);
            return false;
        }
    }
}
