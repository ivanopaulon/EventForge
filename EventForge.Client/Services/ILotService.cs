using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing lots and traceability operations.
/// </summary>
public interface ILotService
{
    Task<PagedResult<LotDto>?> GetLotsAsync(int page = 1, int pageSize = 20, Guid? productId = null, string? status = null, bool? expiringSoon = null, CancellationToken ct = default);
    Task<LotDto?> GetLotByIdAsync(Guid id, CancellationToken ct = default);
    Task<LotDto?> GetLotByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<LotDto>?> GetExpiringLotsAsync(int daysAhead = 30, CancellationToken ct = default);
    Task<LotDto?> CreateLotAsync(CreateLotDto createDto, CancellationToken ct = default);
    Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto, CancellationToken ct = default);
    Task<bool> DeleteLotAsync(Guid id, CancellationToken ct = default);
    Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string? notes = null, CancellationToken ct = default);
    Task<bool> BlockLotAsync(Guid id, string reason, CancellationToken ct = default);
    Task<bool> UnblockLotAsync(Guid id, CancellationToken ct = default);
}