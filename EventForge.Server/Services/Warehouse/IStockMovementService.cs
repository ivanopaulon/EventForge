using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing stock movements and transaction history.
/// </summary>
public interface IStockMovementService
{
    /// <summary>
    /// Gets all stock movements with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? locationId = null,
        string? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stock movement by ID.
    /// </summary>
    Task<StockMovementDto?> GetMovementByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by product ID.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetMovementsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by lot ID.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetMovementsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by serial ID.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetMovementsBySerialIdAsync(Guid serialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by location ID (both from and to locations).
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetMovementsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock movements by document header ID.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetMovementsByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new stock movement.
    /// </summary>
    Task<StockMovementDto> CreateMovementAsync(CreateStockMovementDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple stock movements in a batch.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> CreateMovementsBatchAsync(IEnumerable<CreateStockMovementDto> createDtos, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an inbound movement (receiving stock).
    /// </summary>
    Task<StockMovementDto> ProcessInboundMovementAsync(
        Guid productId,
        Guid toLocationId,
        decimal quantity,
        decimal? unitCost = null,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        Guid? documentRowId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an outbound movement (shipping/selling stock).
    /// </summary>
    Task<StockMovementDto> ProcessOutboundMovementAsync(
        Guid productId,
        Guid fromLocationId,
        decimal quantity,
        Guid? lotId = null,
        Guid? serialId = null,
        Guid? documentHeaderId = null,
        Guid? documentRowId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a transfer movement between locations.
    /// </summary>
    Task<StockMovementDto> ProcessTransferMovementAsync(
        Guid productId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        Guid? lotId = null,
        Guid? serialId = null,
        string? notes = null,
        string? currentUser = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an inventory adjustment movement.
    /// </summary>
    Task<StockMovementDto> ProcessAdjustmentMovementAsync(
        Guid productId,
        Guid locationId,
        decimal adjustmentQuantity,
        string reason,
        Guid? lotId = null,
        string? notes = null,
        string? currentUser = null,
        DateTime? movementDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses a stock movement (creates a counter-movement).
    /// </summary>
    Task<StockMovementDto> ReverseMovementAsync(Guid movementId, string reason, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets movement summary for a specific period.
    /// </summary>
    Task<MovementSummaryDto> GetMovementSummaryAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? productId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a movement is possible (sufficient stock, location exists, etc.).
    /// </summary>
    Task<MovementValidationResult> ValidateMovementAsync(CreateStockMovementDto movementDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending movements (planned but not executed).
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetPendingMovementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a planned movement.
    /// </summary>
    Task<StockMovementDto> ExecutePlannedMovementAsync(Guid movementPlanId, string currentUser, CancellationToken cancellationToken = default);
}