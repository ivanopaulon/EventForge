using EventForge.DTOs.Sales;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for sale sessions.
/// </summary>
public class SalesService : ISalesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SalesService> _logger;
    private const string BaseUrl = "api/v1/sales/sessions";

    public SalesService(IHttpClientFactory httpClientFactory, ILogger<SalesService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to create sale session. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale session");
            return null;
        }
    }

    public async Task<SaleSessionDto?> GetSessionAsync(Guid sessionId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{sessionId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to retrieve session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{sessionId}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{sessionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<List<SaleSessionDto>?> GetActiveSessionsAsync()
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<SaleSessionDto>>(BaseUrl, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return null;
        }
    }

    public async Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            return await httpClient.GetFromJsonAsync<List<SaleSessionDto>>($"{BaseUrl}/operator/{operatorId}", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operator sessions for {OperatorId}", operatorId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/{sessionId}/items", itemDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to add item to session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{sessionId}/items/{itemId}", updateDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to update item {ItemId} in session {SessionId}. Status: {StatusCode}", itemId, sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {ItemId} in session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{sessionId}/items/{itemId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to remove item {ItemId} from session {SessionId}. Status: {StatusCode}", itemId, sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from session {SessionId}", itemId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/{sessionId}/payments", paymentDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to add payment to session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{sessionId}/payments/{paymentId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to remove payment {PaymentId} from session {SessionId}. Status: {StatusCode}", paymentId, sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing payment {PaymentId} from session {SessionId}", paymentId, sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{BaseUrl}/{sessionId}/notes", noteDto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogError("Failed to add note to session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/{sessionId}/totals", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to calculate totals for session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating totals for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/{sessionId}/close", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SaleSessionDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogError("Failed to close session {SessionId}. Status: {StatusCode}", sessionId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session {SessionId}", sessionId);
            return null;
        }
    }
}
