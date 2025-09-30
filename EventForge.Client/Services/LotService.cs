using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of lot management service using HTTP client.
/// </summary>
public class LotService : ILotService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LotService> _logger;
    private const string BaseUrl = "api/v1/warehouse/lots";

    public LotService(IHttpClientFactory httpClientFactory, ILogger<LotService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<LotDto>?> GetLotsAsync(int page = 1, int pageSize = 20, Guid? productId = null, string? status = null, bool? expiringSoon = null)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (productId.HasValue)
                queryParams.Add($"productId={productId.Value}");

            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (expiringSoon.HasValue)
                queryParams.Add($"expiringSoon={expiringSoon.Value}");

            var query = string.Join("&", queryParams);
            var response = await httpClient.GetAsync($"{BaseUrl}?{query}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PagedResult<LotDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogWarning("Failed to get lots. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lots");
            return null;
        }
    }

    public async Task<LotDto?> GetLotByIdAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/{id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LotDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get lot {LotId}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lot {LotId}", id);
            return null;
        }
    }

    public async Task<LotDto?> GetLotByCodeAsync(string code)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/code/{Uri.EscapeDataString(code)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LotDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to get lot by code {Code}. Status: {StatusCode}", code, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lot by code {Code}", code);
            return null;
        }
    }

    public async Task<IEnumerable<LotDto>?> GetExpiringLotsAsync(int daysAhead = 30)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.GetAsync($"{BaseUrl}/expiring?daysAhead={daysAhead}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<LotDto>>();
            }

            _logger.LogWarning("Failed to get expiring lots. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring lots");
            return null;
        }
    }

    public async Task<LotDto?> CreateLotAsync(CreateLotDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LotDto>();
            }

            _logger.LogWarning("Failed to create lot. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot");
            return null;
        }
    }

    public async Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", updateDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LotDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Failed to update lot {LotId}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lot {LotId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteLotAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string? notes = null)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var queryParams = $"qualityStatus={Uri.EscapeDataString(qualityStatus)}";
            if (!string.IsNullOrEmpty(notes))
                queryParams += $"&notes={Uri.EscapeDataString(notes)}";

            var response = await httpClient.PatchAsync($"{BaseUrl}/{id}/quality-status?{queryParams}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quality status for lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> BlockLotAsync(Guid id, string reason)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/{id}/block?reason={Uri.EscapeDataString(reason)}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UnblockLotAsync(Guid id)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        try
        {
            var response = await httpClient.PostAsync($"{BaseUrl}/{id}/unblock", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking lot {LotId}", id);
            return false;
        }
    }
}