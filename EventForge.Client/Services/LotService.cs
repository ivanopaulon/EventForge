using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of lot management service using HTTP client.
/// </summary>
public class LotService(
    IHttpClientService httpClientService,
    ILogger<LotService> logger) : ILotService
{
    private const string BaseUrl = "api/v1/warehouse/lots";

    public async Task<PagedResult<LotDto>?> GetLotsAsync(int page = 1, int pageSize = 20, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken ct = default)
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
            return await httpClientService.GetAsync<PagedResult<LotDto>>($"{BaseUrl}?{query}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lots");
            return null;
        }
    }

    public async Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<LotDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lot {LotId}", id);
            return null;
        }
    }

    public async Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<LotDto>($"{BaseUrl}/code/{Uri.EscapeDataString(code)}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting lot by code {Code}", code);
            return null;
        }
    }

    public async Task<IEnumerable<LotDto>?> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<IEnumerable<LotDto>>($"{BaseUrl}/expiring?daysAhead={daysAhead}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting expiring lots");
            return null;
        }
    }

    public async Task<LotDto?> CreateLotAsync(CreateLotDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateLotDto, LotDto>(BaseUrl, createDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating lot");
            return null;
        }
    }

    public async Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateLotDto, LotDto>($"{BaseUrl}/{id}", updateDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating lot {LotId}", id);
            return null;
        }
    }

    public async Task<bool> DeleteLotAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string? notes = null, CancellationToken ct = default)
    {
        try
        {
            var queryParams = $"qualityStatus={Uri.EscapeDataString(qualityStatus)}";
            if (!string.IsNullOrEmpty(notes))
                queryParams += $"&notes={Uri.EscapeDataString(notes)}";

            _ = await httpClientService.PatchAsync<object, object>($"{BaseUrl}/{id}/quality-status?{queryParams}", new { });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating quality status for lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> BlockLotAsync(Guid id, string reason, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync($"{BaseUrl}/{id}/block?reason={Uri.EscapeDataString(reason)}", new { });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error blocking lot {LotId}", id);
            return false;
        }
    }

    public async Task<bool> UnblockLotAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.PostAsync($"{BaseUrl}/{id}/unblock", new { });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unblocking lot {LotId}", id);
            return false;
        }
    }
}