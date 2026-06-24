using Prym.DTOs.RetailCart;

namespace Prym.Web.Services;

public interface IRetailCartSessionService
{
    Task<CartSessionDto> CreateSessionAsync(CreateCartSessionDto dto, CancellationToken ct = default);
    Task<CartSessionDto?> GetSessionAsync(Guid id, CancellationToken ct = default);
    Task<CartSessionDto?> AddItemAsync(Guid sessionId, AddCartItemDto dto, CancellationToken ct = default);
    Task<CartSessionDto?> UpdateItemQuantityAsync(Guid sessionId, Guid itemId, UpdateCartItemDto dto, CancellationToken ct = default);
    Task<CartSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken ct = default);
    Task<CartSessionDto?> ApplyCouponsAsync(Guid sessionId, ApplyCouponsDto dto, CancellationToken ct = default);
    Task<CartSessionDto?> ClearSessionAsync(Guid sessionId, CancellationToken ct = default);
}

public class RetailCartSessionService(
    IHttpClientService httpClientService,
    ILogger<RetailCartSessionService> logger) : IRetailCartSessionService
{
    private const string BaseUrl = "api/v1/retailcartsessions";

    public async Task<CartSessionDto> CreateSessionAsync(CreateCartSessionDto dto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateCartSessionDto, CartSessionDto>(BaseUrl, dto, ct);
            return result ?? throw new InvalidOperationException("Failed to create retail cart session");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating retail cart session");
            throw;
        }
    }

    public async Task<CartSessionDto?> GetSessionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<CartSessionDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving retail cart session {SessionId}", id);
            throw;
        }
    }

    public async Task<CartSessionDto?> AddItemAsync(Guid sessionId, AddCartItemDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<AddCartItemDto, CartSessionDto>($"{BaseUrl}/{sessionId}/items", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding item to retail cart session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<CartSessionDto?> UpdateItemQuantityAsync(Guid sessionId, Guid itemId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PatchAsync<UpdateCartItemDto, CartSessionDto>($"{BaseUrl}/{sessionId}/items/{itemId}", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating item {ItemId} in retail cart session {SessionId}", itemId, sessionId);
            throw;
        }
    }

    public async Task<CartSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.DeleteAsync<CartSessionDto>($"{BaseUrl}/{sessionId}/items/{itemId}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing item {ItemId} from retail cart session {SessionId}", itemId, sessionId);
            throw;
        }
    }

    public async Task<CartSessionDto?> ApplyCouponsAsync(Guid sessionId, ApplyCouponsDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<ApplyCouponsDto, CartSessionDto>($"{BaseUrl}/{sessionId}/coupons", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying coupons to retail cart session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<CartSessionDto?> ClearSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, CartSessionDto>($"{BaseUrl}/{sessionId}/clear", new { }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing retail cart session {SessionId}", sessionId);
            throw;
        }
    }
}
