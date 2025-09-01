using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing lots and traceability.
/// </summary>
public interface ILotService
{
    /// <summary>
    /// Gets all lots with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<LotDto>> GetLotsAsync(
        int page = 1, 
        int pageSize = 20, 
        Guid? productId = null, 
        string? status = null,
        bool? expiringSoon = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lot by ID.
    /// </summary>
    Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lot by code.
    /// </summary>
    Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lots by product ID.
    /// </summary>
    Task<IEnumerable<LotDto>> GetLotsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets lots that are expiring within the specified number of days.
    /// </summary>
    Task<IEnumerable<LotDto>> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new lot.
    /// </summary>
    Task<LotDto> CreateLotAsync(CreateLotDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing lot.
    /// </summary>
    Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a lot by ID.
    /// </summary>
    Task<bool> DeleteLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the quality status of a lot.
    /// </summary>
    Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string currentUser, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks a lot (sets status to Blocked).
    /// </summary>
    Task<bool> BlockLotAsync(Guid id, string reason, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks a lot (sets status to Active).
    /// </summary>
    Task<bool> UnblockLotAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available quantity for a lot across all locations.
    /// </summary>
    Task<decimal> GetAvailableQuantityAsync(Guid lotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a lot code is unique within the tenant.
    /// </summary>
    Task<bool> IsLotCodeUniqueAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}