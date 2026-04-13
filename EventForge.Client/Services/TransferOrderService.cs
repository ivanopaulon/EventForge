using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of transfer order service using HTTP client.
/// </summary>
public class TransferOrderService(
    IHttpClientService httpClientService,
    ILogger<TransferOrderService> logger) : ITransferOrderService
{
    private const string BaseUrl = "api/v1/transferorder";

    public async Task<PagedResult<TransferOrderDto>?> GetTransferOrdersAsync(
        int page = 1,
        int pageSize = 20,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        string? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}?page={page}&pageSize={pageSize}";

            if (sourceWarehouseId.HasValue)
                url += $"&sourceWarehouseId={sourceWarehouseId.Value}";

            if (destinationWarehouseId.HasValue)
                url += $"&destinationWarehouseId={destinationWarehouseId.Value}";

            if (!string.IsNullOrWhiteSpace(status))
                url += $"&status={Uri.EscapeDataString(status)}";

            if (!string.IsNullOrWhiteSpace(searchTerm))
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

            return await httpClientService.GetAsync<PagedResult<TransferOrderDto>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving transfer orders");
            return null;
        }
    }

    public async Task<TransferOrderDto?> GetTransferOrderAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<TransferOrderDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<TransferOrderDto?> CreateTransferOrderAsync(CreateTransferOrderDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<CreateTransferOrderDto, TransferOrderDto>(BaseUrl, dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating transfer order");
            return null;
        }
    }

    public async Task<TransferOrderDto?> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<ShipTransferOrderDto, TransferOrderDto>($"{BaseUrl}/{id}/ship", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error shipping transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<TransferOrderDto?> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto dto, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.PostAsync<ReceiveTransferOrderDto, TransferOrderDto>($"{BaseUrl}/{id}/receive", dto, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error receiving transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<bool> CancelTransferOrderAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}/cancel", ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling transfer order {TransferOrderId}", id);
            return false;
        }
    }
}
