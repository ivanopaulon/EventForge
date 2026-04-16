using Prym.DTOs.Sales;

namespace Prym.Web.Services.Sales;

/// <summary>
/// Client service implementation for sale sessions.
/// </summary>
public class SalesService(
    IHttpClientService httpClientService,
    ILogger<SalesService> logger) : ISalesService
{
    private const string BaseUrl = "api/v1/sales/sessions";

    public async Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateSaleSessionDto, SaleSessionDto>(BaseUrl, createDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating sale session");
            return null;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateSaleSessionDto, SaleSessionDto>($"{BaseUrl}/{sessionId}", updateDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<SaleSessionDto>?> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<SaleSessionDto>>(BaseUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active sessions");
            return null;
        }
    }

    public async Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<SaleSessionDto>>($"{BaseUrl}/operator/{operatorId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving operator sessions for {OperatorId}", operatorId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<AddSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items", itemDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding item to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items/{itemId}", updateDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating item {ItemId} in session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Optimized: DELETE + GET (2 calls instead of 3)
            // Future optimization: Modify server DELETE endpoint to return updated session (1 call)
            await httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/items/{itemId}", cancellationToken);
            // Fetch updated session
            return await httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing item {ItemId} from session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<AddSalePaymentDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/payments", paymentDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding payment to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/payments/{paymentId}", cancellationToken);
            // Fetch updated session
            return await httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing payment {PaymentId} from session {SessionId}", paymentId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<AddSessionNoteDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/notes", noteDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding note to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> ApplyGlobalDiscountAsync(Guid sessionId, ApplyGlobalDiscountDto discountDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<ApplyGlobalDiscountDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/discount", discountDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying global discount to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/totals", new { }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating totals for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/close", new { }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SplitResultDto?> SplitSessionAsync(SplitSessionDto splitDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<SplitSessionDto, SplitResultDto>($"{BaseUrl}/split", splitDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error splitting session {SessionId}", splitDto.SessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> MergeSessionsAsync(MergeSessionsDto mergeDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.PostAsync<MergeSessionsDto, SaleSessionDto>($"{BaseUrl}/merge", mergeDto, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error merging sessions");
            return null;
        }
    }

    public async Task<List<SaleSessionDto>> GetChildSessionsAsync(Guid parentSessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<List<SaleSessionDto>>(
                $"{BaseUrl}/{parentSessionId}/children", cancellationToken) ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving child sessions for {ParentSessionId}", parentSessionId);
            return new();
        }
    }

    public async Task<bool> CanSplitSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClientService.GetAsync<Dictionary<string, bool>>(
                $"{BaseUrl}/{sessionId}/can-split", cancellationToken);
            return response?.TryGetValue("canSplit", out var canSplit) == true && canSplit;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if session {SessionId} can be split", sessionId);
            return false;
        }
    }

    public async Task<bool> CanMergeSessionsAsync(List<Guid> sessionIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = string.Join("&", sessionIds.Select(id => $"sessionIds={id}"));
            var response = await httpClientService.GetAsync<Dictionary<string, bool>>(
                $"{BaseUrl}/can-merge?{query}", cancellationToken);
            return response?.TryGetValue("canMerge", out var canMerge) == true && canMerge;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if sessions can be merged");
            return false;
        }
    }

    public async Task<IEnumerable<Guid>?> GetCustomerPurchasedProductIdsAsync(Guid customerId, int maxSessions = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<Guid>>(
                $"{BaseUrl}/customers/{customerId}/purchased-product-ids?maxSessions={maxSessions}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving purchased product IDs for customer {CustomerId}", customerId);
            return null;
        }
    }
}
