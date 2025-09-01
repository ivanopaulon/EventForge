using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing individual serial numbers/matricole.
/// </summary>
public interface ISerialService
{
    /// <summary>
    /// Gets all serials with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<SerialDto>> GetSerialsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? lotId = null,
        Guid? locationId = null,
        string? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a serial by ID.
    /// </summary>
    Task<SerialDto?> GetSerialByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a serial by serial number.
    /// </summary>
    Task<SerialDto?> GetSerialByNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serials by product ID.
    /// </summary>
    Task<IEnumerable<SerialDto>> GetSerialsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serials by lot ID.
    /// </summary>
    Task<IEnumerable<SerialDto>> GetSerialsByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serials by current location.
    /// </summary>
    Task<IEnumerable<SerialDto>> GetSerialsByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serials by owner (customer).
    /// </summary>
    Task<IEnumerable<SerialDto>> GetSerialsByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serials with warranty expiring within specified days.
    /// </summary>
    Task<IEnumerable<SerialDto>> GetSerialsWithExpiringWarrantyAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new serial.
    /// </summary>
    Task<SerialDto> CreateSerialAsync(CreateSerialDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing serial.
    /// </summary>
    Task<SerialDto?> UpdateSerialAsync(Guid id, UpdateSerialDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates serial status.
    /// </summary>
    Task<bool> UpdateSerialStatusAsync(Guid id, string status, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves serial to a new location.
    /// </summary>
    Task<bool> MoveSerialAsync(Guid id, Guid newLocationId, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sells serial to a customer.
    /// </summary>
    Task<bool> SellSerialAsync(Guid id, Guid customerId, DateTime saleDate, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns serial from customer.
    /// </summary>
    Task<bool> ReturnSerialAsync(Guid id, Guid? newLocationId, string currentUser, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a serial number is unique within the tenant.
    /// </summary>
    Task<bool> IsSerialNumberUniqueAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a serial.
    /// </summary>
    Task<bool> DeleteSerialAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets serial movement history.
    /// </summary>
    Task<IEnumerable<StockMovementDto>> GetSerialHistoryAsync(Guid serialId, CancellationToken cancellationToken = default);
}