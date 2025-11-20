using EventForge.DTOs.Common;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;
using EventForge.DTOs.Warehouse;
using EventForge.Client.Services.UI;
using EventForge.Client.Services.Infrastructure;
using EventForge.Client.Services.Core;

namespace EventForge.Client.Services.Domain.Warehouse;

/// <summary>
/// Implementation of lot management service using HTTP client.
/// </summary>
public class LotService : ILotService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<LotService> _logger;
    private const string BaseUrl = "api/v1/warehouse/lots";

    public LotService(IHttpClientService httpClientService, ILogger<LotService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<LotDto>?> GetLotsAsync(int page = 1, int pageSize = 20, Guid? productId = null, string? status = null, bool? expiringSoon = null)
    {
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
            return await _httpClientService.GetAsync<PagedResult<LotDto>>($"{BaseUrl}?{query}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lots");
            return null;
        }
    }

    public async Task<LotDto?> GetLotByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<LotDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lot {LotId}", id);
            return null;
        }
    }

    public async Task<LotDto?> GetLotByCodeAsync(string code)
    {
        try
        {
            return await _httpClientService.GetAsync<LotDto>($"{BaseUrl}/code/{Uri.EscapeDataString(code)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lot by code {Code}", code);
            return null;
        }
    }

    public async Task<IEnumerable<LotDto>?> GetExpiringLotsAsync(int daysAhead = 30)
    {
        try
        {
            return await _httpClientService.GetAsync<IEnumerable<LotDto>>($"{BaseUrl}/expiring?daysAhead={daysAhead}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring lots");
            return null;
        }
    }

    public async Task<LotDto?> CreateLotAsync(CreateLotDto createDto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateLotDto, LotDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lot");
            return null;
        }
    }

    public async Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto)
    {
        try
        {
            return await _httpClientService.PutAsync<UpdateLotDto, LotDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lot {LotId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteLotAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string? notes = null)
    {
        try
        {
            var queryParams = $"qualityStatus={Uri.EscapeDataString(qualityStatus)}";
            if (!string.IsNullOrEmpty(notes))
                queryParams += $"&notes={Uri.EscapeDataString(notes)}";

            _ = await _httpClientService.PatchAsync<object, object>($"{BaseUrl}/{id}/quality-status?{queryParams}", new { });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quality status for lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> BlockLotAsync(Guid id, string reason)
    {
        try
        {
            await _httpClientService.PostAsync($"{BaseUrl}/{id}/block?reason={Uri.EscapeDataString(reason)}", new { });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blocking lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UnblockLotAsync(Guid id)
    {
        try
        {
            await _httpClientService.PostAsync($"{BaseUrl}/{id}/unblock", new { });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking lot {LotId}", id);
            return false;
        }
    }
}