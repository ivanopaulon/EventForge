using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for payment methods.
/// </summary>
public class PaymentMethodService : IPaymentMethodService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<PaymentMethodService> _logger;
    private const string BaseUrl = "api/v1/payment-methods";
    private const int DefaultPageSize = 100; // Default page size for pagination

    public PaymentMethodService(IHttpClientService httpClientService, ILogger<PaymentMethodService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<PaymentMethodDto>?> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Fetching all payment methods from server");

            var allItems = new List<PaymentMethodDto>();
            int page = 1;
            int pageSize = DefaultPageSize;

            while (true)
            {
                // Server returns PagedResult<PaymentMethodDto>
                var pagedResult = await _httpClientService.GetAsync<PagedResult<PaymentMethodDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");

                if (pagedResult?.Items == null || !pagedResult.Items.Any())
                {
                    break;
                }

                allItems.AddRange(pagedResult.Items);

                // Check if we've retrieved all items
                if (!pagedResult.HasNextPage || allItems.Count >= pagedResult.TotalCount)
                {
                    break;
                }

                page++;
            }

            _logger.LogDebug("Retrieved {Count} payment methods from server across {Pages} page(s)", allItems.Count, page);
            return allItems;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentMethodService] Error retrieving all payment methods: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error retrieving all payment methods");
            throw;
        }
    }

    public async Task<PagedResult<PaymentMethodDto>> GetPagedAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<PaymentMethodDto>>(
                $"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<PaymentMethodDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentMethodService] Error retrieving paged payment methods (page: {page}, pageSize: {pageSize}): {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error retrieving paged payment methods");
            throw;
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
            Console.WriteLine($"[PaymentMethodService] Error retrieving active payment methods: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error retrieving active payment methods");
            throw;
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
            Console.WriteLine($"[PaymentMethodService] Error retrieving payment method {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error retrieving payment method {Id}", id);
            throw;
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
            Console.WriteLine($"[PaymentMethodService] Error creating payment method: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error creating payment method");
            throw;
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
            Console.WriteLine($"[PaymentMethodService] Error updating payment method {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error updating payment method {Id}", id);
            throw;
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
            Console.WriteLine($"[PaymentMethodService] Error deleting payment method {id}: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[PaymentMethodService] StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "Error deleting payment method {Id}", id);
            throw;
        }
    }
}
