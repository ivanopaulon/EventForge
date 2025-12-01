using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Implementation of transfer order service using HTTP client.
/// </summary>
public class TransferOrderService : ITransferOrderService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<TransferOrderService> _logger;
    private const string BaseUrl = "api/v1/transferorder";

    public TransferOrderService(IHttpClientService httpClientService, ILogger<TransferOrderService> logger)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

            return await _httpClientService.GetAsync<PagedResult<TransferOrderDto>>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer orders");
            return null;
        }
    }

    public async Task<TransferOrderDto?> GetTransferOrderAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<TransferOrderDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<TransferOrderDto?> CreateTransferOrderAsync(CreateTransferOrderDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateTransferOrderDto, TransferOrderDto>(BaseUrl, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer order");
            return null;
        }
    }

    public async Task<TransferOrderDto?> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<ShipTransferOrderDto, TransferOrderDto>($"{BaseUrl}/{id}/ship", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<TransferOrderDto?> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<ReceiveTransferOrderDto, TransferOrderDto>($"{BaseUrl}/{id}/receive", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving transfer order {TransferOrderId}", id);
            return null;
        }
    }

    public async Task<bool> CancelTransferOrderAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}/cancel");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer order {TransferOrderId}", id);
            return false;
        }
    }
}
