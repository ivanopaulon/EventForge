using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace Prym.Client.Services;

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
    Task<TransferOrderDto?> GetTransferOrderAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new transfer order.
    /// </summary>
    Task<TransferOrderDto?> CreateTransferOrderAsync(CreateTransferOrderDto dto, CancellationToken ct = default);

    /// <summary>
    /// Ships a transfer order.
    /// </summary>
    Task<TransferOrderDto?> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto dto, CancellationToken ct = default);

    /// <summary>
    /// Receives a transfer order.
    /// </summary>
    Task<TransferOrderDto?> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto dto, CancellationToken ct = default);

    /// <summary>
    /// Cancels a transfer order.
    /// </summary>
    Task<bool> CancelTransferOrderAsync(Guid id, CancellationToken ct = default);
}
