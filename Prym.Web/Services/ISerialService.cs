using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace Prym.Web.Services;

/// <summary>
/// Client service interface for managing serial numbers/matricole.
/// </summary>
public interface ISerialService
{
    /// <summary>
    /// Gets serials with pagination and optional filters.
    /// </summary>
    Task<PagedResult<SerialDto>?> GetSerialsAsync(int page = 1, int pageSize = 20, Guid? productId = null, Guid? lotId = null, string? status = null, string? searchTerm = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a serial by ID.
    /// </summary>
    Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all serials for a specific product.
    /// </summary>
    Task<IEnumerable<SerialDto>?> GetSerialsByProductIdAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Gets all serials for a specific lot.
    /// </summary>
    Task<IEnumerable<SerialDto>?> GetSerialsByLotIdAsync(Guid lotId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new serial.
    /// </summary>
    Task<SerialDto?> CreateSerialAsync(CreateSerialDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing serial.
    /// </summary>
    Task<SerialDto?> UpdateSerialAsync(Guid id, UpdateSerialDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of a serial.
    /// </summary>
    Task<bool> UpdateSerialStatusAsync(Guid id, string status, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a serial.
    /// </summary>
    Task<bool> DeleteSerialAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Moves a serial to a new storage location.
    /// </summary>
    Task<bool> MoveSerialAsync(Guid id, Guid newLocationId, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Sells a serial to a customer.
    /// </summary>
    Task<bool> SellSerialAsync(Guid id, Guid customerId, DateTime saleDate, CancellationToken ct = default);

    /// <summary>
    /// Returns a sold serial to stock.
    /// </summary>
    Task<bool> ReturnSerialAsync(Guid id, Guid? newLocationId = null, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Gets movement history for a serial.
    /// </summary>
    Task<IEnumerable<StockMovementDto>?> GetSerialHistoryAsync(Guid id, CancellationToken ct = default);
}
