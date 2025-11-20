using EventForge.DTOs.Sales;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for sale sessions.
/// </summary>
public class SalesService : ISalesService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<SalesService> _logger;
    private const string BaseUrl = "api/v1/sales/sessions";

    public SalesService(IHttpClientService httpClientService, ILogger<SalesService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateSaleSessionDto, SaleSessionDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale session");
            return null;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId)
    {
        try
        {
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateSaleSessionDto, SaleSessionDto>($"{BaseUrl}/{sessionId}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<SaleSessionDto>?> GetActiveSessionsAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<SaleSessionDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return null;
        }
    }

    public async Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId)
    {
        try
        {
            return await _httpClientService.GetAsync<List<SaleSessionDto>>($"{BaseUrl}/operator/{operatorId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operator sessions for {OperatorId}", operatorId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items", itemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items/{itemId}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} in session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId)
    {
        try
        {
            // DeleteAsync with return type requires special handling - we'll use a workaround
            var result = await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}");
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/items/{itemId}");
            // Fetch updated session
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSalePaymentDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/payments", paymentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/payments/{paymentId}");
            // Fetch updated session
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment {PaymentId} from session {SessionId}", paymentId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSessionNoteDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/notes", noteDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId)
    {
        try
        {
            return await _httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/totals", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating totals for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId)
    {
        try
        {
            return await _httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/close", new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session {SessionId}", sessionId);
            return null;
        }
    }
}
