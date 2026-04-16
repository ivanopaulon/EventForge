using Prym.Web.Helpers;
using Prym.DTOs.Common;
using Prym.DTOs.Sales;
using Microsoft.Extensions.Caching.Memory;

namespace Prym.Web.Services.Sales;

/// <summary>
/// Client service implementation for payment methods.
/// </summary>
public class PaymentMethodService(
    IHttpClientService httpClientService,
    ILogger<PaymentMethodService> logger,
    IMemoryCache cache) : IPaymentMethodService
{
    private const string BaseUrl = "api/v1/payment-methods";
    private const int DefaultPageSize = 100;

    public async Task<List<PaymentMethodDto>?> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogDebug("Fetching all payment methods from server");

            List<PaymentMethodDto> allItems = [];
            int page = 1;
            int pageSize = DefaultPageSize;

            while (true)
            {
                // Server returns PagedResult<PaymentMethodDto>
                var pagedResult = await httpClientService.GetAsync<PagedResult<PaymentMethodDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);

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

            logger.LogDebug("Retrieved {Count} payment methods from server across {Pages} page(s)", allItems.Count, page);
            return allItems;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all payment methods");
            throw;
        }
    }

    public async Task<PagedResult<PaymentMethodDto>> GetPagedAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<PaymentMethodDto>>(
                $"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<PaymentMethodDto>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged payment methods");
            throw;
        }
    }

    public async Task<List<PaymentMethodDto>?> GetActiveAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheHelper.ACTIVE_PAYMENT_METHODS, out List<PaymentMethodDto>? cached) && cached != null)
        {
            logger.LogDebug("Cache HIT: Active payment methods ({Count} items)", cached.Count);
            return cached;
        }

        logger.LogDebug("Cache MISS: Loading active payment methods from API");

        try
        {
            var pagedResult = await httpClientService.GetAsync<PagedResult<PaymentMethodDto>>($"{BaseUrl}/active", ct);

            var activePaymentMethods = pagedResult?.Items?.ToList() ?? [];

            cache.Set(
                CacheHelper.ACTIVE_PAYMENT_METHODS,
                activePaymentMethods,
                CacheHelper.GetLongCacheOptions()
            );

            logger.LogInformation(
                "Cached {Count} active payment methods for {Minutes} minutes",
                activePaymentMethods.Count,
                CacheHelper.LongCache.TotalMinutes
            );

            return activePaymentMethods;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active payment methods");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<PaymentMethodDto>($"{BaseUrl}/{id}", ct);

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment method {Id}", id);
            throw;
        }
    }

    public async Task<PaymentMethodDto?> CreateAsync(CreatePaymentMethodDto createDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreatePaymentMethodDto, PaymentMethodDto>(BaseUrl, createDto, ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_PAYMENT_METHODS);
            logger.LogDebug("Invalidated active payment methods cache after create");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment method");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> UpdateAsync(Guid id, UpdatePaymentMethodDto updateDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdatePaymentMethodDto, PaymentMethodDto>($"{BaseUrl}/{id}", updateDto, ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_PAYMENT_METHODS);
            logger.LogDebug("Invalidated active payment methods cache after update (ID: {Id})", id);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment method {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_PAYMENT_METHODS);
            logger.LogDebug("Invalidated active payment methods cache after delete (ID: {Id})", id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting payment method {Id}", id);
            throw;
        }
    }
}
