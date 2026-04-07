using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing transfer orders.
/// </summary>
public interface ITransferOrderService
{
    /// <summary>
    /// Gets all transfer orders with optional pagination and filters.
    /// </summary>
    Task<PagedResult<TransferOrderDto>> GetTransferOrdersAsync(
        int page = 1,
        int pageSize = 20,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        string? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transfer order by ID.
    /// </summary>
    Task<TransferOrderDto?> GetTransferOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new transfer order.
    /// </summary>
    Task<TransferOrderDto> CreateTransferOrderAsync(CreateTransferOrderDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ships a transfer order - creates stock movements OUT and reduces stock.
    /// </summary>
    Task<TransferOrderDto> ShipTransferOrderAsync(Guid id, ShipTransferOrderDto shipDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Receives a transfer order - creates stock movements IN and increases stock.
    /// </summary>
    Task<TransferOrderDto> ReceiveTransferOrderAsync(Guid id, ReceiveTransferOrderDto receiveDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a transfer order.
    /// </summary>
    Task<bool> CancelTransferOrderAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}
