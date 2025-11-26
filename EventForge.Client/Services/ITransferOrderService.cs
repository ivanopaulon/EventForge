using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing transfer orders.
/// </summary>
public interface ITransferOrderService
{
    /// <summary>
    /// Gets all transfer orders with pagination and filters.
    /// </summary>
    Task<PagedResult<TransferOrderDto>?> GetTransferOrdersAsync(
        int page = 1, 
        int pageSize = 20,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        string? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific transfer order by ID.
    /// </summary>
    Task<TransferOrderDto?> GetTransferOrderAsync(Guid id);

    /// <summary>
    /// Creates a new transfer order.
    /// </summary>
    Task<TransferOrderDto?> CreateTransferOrderAsync(CreateTransferOrderDto dto);

    /// <summary>
    /// Ships a transfer order.
    /// </summary>
    Task<TransferOrderDto?> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto dto);

    /// <summary>
    /// Receives a transfer order.
    /// </summary>
    Task<TransferOrderDto?> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto dto);

    /// <summary>
    /// Cancels a transfer order.
    /// </summary>
    Task<bool> CancelTransferOrderAsync(Guid id);
}
