using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace Prym.Web.Services;

/// <summary>
/// Implementation of serial number management service using HTTP client.
/// </summary>
public class SerialService(
    IHttpClientService httpClientService,
    ILogger<SerialService> logger) : ISerialService
{
    private const string BaseUrl = "api/v1/warehouse/serials";

    public async Task<PagedResult<SerialDto>?> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, string? status = null, string? searchTerm = null, CancellationToken ct = default)
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

            if (lotId.HasValue)
                queryParams.Add($"lotId={lotId.Value}");

            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (!string.IsNullOrEmpty(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            var query = string.Join("&", queryParams);
            return await httpClientService.GetAsync<PagedResult<SerialDto>>($"{BaseUrl}?{query}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting serials");
            return null;
        }
    }

    public async Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<SerialDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting serial {SerialId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<SerialDto>?> GetSerialsByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<SerialDto>>($"{BaseUrl}?productId={productId}&pageSize=500", ct);
            return result?.Items;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting serials for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<IEnumerable<SerialDto>?> GetSerialsByLotIdAsync(Guid lotId, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<SerialDto>>($"{BaseUrl}?lotId={lotId}&pageSize=500", ct);
            return result?.Items;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting serials for lot {LotId}", lotId);
            return null;
        }
    }

    public async Task<SerialDto?> CreateSerialAsync(CreateSerialDto createDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateSerialDto, SerialDto>(BaseUrl, createDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating serial");
            return null;
        }
    }

    public async Task<SerialDto?> UpdateSerialAsync(Guid id, UpdateSerialDto updateDto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PutAsync<UpdateSerialDto, SerialDto>($"{BaseUrl}/{id}", updateDto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating serial {SerialId}", id);
            return null;
        }
    }

    public async Task<bool> UpdateSerialStatusAsync(Guid id, string status, string? notes = null, CancellationToken ct = default)
    {
        try
        {
            var queryParams = $"status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrEmpty(notes))
                queryParams += $"&notes={Uri.EscapeDataString(notes)}";

            _ = await httpClientService.PutAsync<object, object>($"{BaseUrl}/{id}/status?{queryParams}", new { });
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for serial {SerialId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteSerialAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting serial {SerialId}", id);
            return false;
        }
    }
}
