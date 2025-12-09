using EventForge.DTOs.Sales;

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

    public async Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateSaleSessionDto, SaleSessionDto>(BaseUrl, createDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale session");
            return null;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateSaleSessionDto, SaleSessionDto>($"{BaseUrl}/{sessionId}", updateDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}", cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<SaleSessionDto>?> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<List<SaleSessionDto>>(BaseUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return null;
        }
    }

    public async Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.GetAsync<List<SaleSessionDto>>($"{BaseUrl}/operator/{operatorId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operator sessions for {OperatorId}", operatorId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items", itemDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateSaleItemDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/items/{itemId}", updateDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} in session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Optimized: DELETE + GET (2 calls instead of 3)
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/items/{itemId}", cancellationToken);
            // Fetch updated session
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSalePaymentDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/payments", paymentDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{sessionId}/payments/{paymentId}", cancellationToken);
            // Fetch updated session
            return await _httpClientService.GetAsync<SaleSessionDto>($"{BaseUrl}/{sessionId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment {PaymentId} from session {SessionId}", paymentId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<AddSessionNoteDto, SaleSessionDto>($"{BaseUrl}/{sessionId}/notes", noteDto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/totals", new { }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating totals for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClientService.PostAsync<object, SaleSessionDto>($"{BaseUrl}/{sessionId}/close", new { }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session {SessionId}", sessionId);
            return null;
        }
    }
}
