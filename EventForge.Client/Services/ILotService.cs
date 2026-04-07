using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing lots and traceability operations.
/// </summary>
public interface ILotService
{
    Task<PagedResult<LotDto>?> GetLotsAsync(int page = 1, int pageSize = 20, Guid? productId = null, string? status = null, bool? expiringSoon = null);
    Task<LotDto?> GetLotByIdAsync(Guid id);
    Task<LotDto?> GetLotByCodeAsync(string code);
    Task<IEnumerable<LotDto>?> GetExpiringLotsAsync(int daysAhead = 30);
    Task<LotDto?> CreateLotAsync(CreateLotDto createDto);
    Task<LotDto?> UpdateLotAsync(Guid id, UpdateLotDto updateDto);
    Task<bool> DeleteLotAsync(Guid id);
    Task<bool> UpdateQualityStatusAsync(Guid id, string qualityStatus, string? notes = null);
    Task<bool> BlockLotAsync(Guid id, string reason);
    Task<bool> UnblockLotAsync(Guid id);
}