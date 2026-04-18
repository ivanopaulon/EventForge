using Prym.DTOs.Store;

namespace Prym.Web.Services.Store;

/// <summary>
/// Client service interface for cashier shift management.
/// </summary>
public interface IShiftService
{
    /// <summary>Gets all shifts within the specified date range.</summary>
    Task<List<CashierShiftDto>> GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Gets shifts for a specific operator within the specified date range.</summary>
    Task<List<CashierShiftDto>> GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Gets a single shift by ID.</summary>
    Task<CashierShiftDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new shift.</summary>
    Task<CashierShiftDto?> CreateAsync(CreateCashierShiftDto dto, CancellationToken ct = default);

    /// <summary>Updates an existing shift.</summary>
    Task<CashierShiftDto?> UpdateAsync(Guid id, UpdateCashierShiftDto dto, CancellationToken ct = default);

    /// <summary>Deletes a shift.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
