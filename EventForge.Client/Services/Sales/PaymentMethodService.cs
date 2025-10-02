using EventForge.DTOs.Sales;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for payment methods.
/// </summary>
public class PaymentMethodService : IPaymentMethodService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentMethodService> _logger;
    private const string BaseUrl = "api/v1/payment-methods";

    public PaymentMethodService(IHttpClientFactory httpClientFactory, ILogger<PaymentMethodService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PaymentMethodDto>?> GetAllAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<PaymentMethodDto>>(BaseUrl, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all payment methods");
            return null;
        }
    }

    public async Task<List<PaymentMethodDto>?> GetActiveAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<PaymentMethodDto>>($"{BaseUrl}/active", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active payment methods");
            return null;
        }
    }

    public async Task<PaymentMethodDto?> GetByIdAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentMethodDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve payment method {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {Id}", id);
            return null;
        }
    }

    public async Task<PaymentMethodDto?> CreateAsync(CreatePaymentMethodDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentMethodDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create payment method. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return null;
        }
    }

    public async Task<PaymentMethodDto?> UpdateAsync(Guid id, UpdatePaymentMethodDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PaymentMethodDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update payment method {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {Id}", id);
            return false;
        }
    }
}
