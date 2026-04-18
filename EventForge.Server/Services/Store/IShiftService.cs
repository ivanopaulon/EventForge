using Prym.DTOs.Store;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service interface for managing cashier shift scheduling.
/// </summary>
public interface IShiftService
{
    /// <summary>Gets all shifts within the specified date range for the current tenant.</summary>
    Task<List<CashierShiftDto>> GetShiftsAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Gets a single shift by ID.</summary>
    Task<CashierShiftDto?> GetShiftByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new shift.</summary>
    Task<CashierShiftDto> CreateShiftAsync(CreateCashierShiftDto dto, string currentUser, CancellationToken ct = default);

    /// <summary>Updates an existing shift.</summary>
    Task<CashierShiftDto?> UpdateShiftAsync(Guid id, UpdateCashierShiftDto dto, string currentUser, CancellationToken ct = default);

    /// <summary>Soft-deletes a shift.</summary>
    Task<bool> DeleteShiftAsync(Guid id, string currentUser, CancellationToken ct = default);

    /// <summary>Gets all shifts for a specific operator within the specified date range.</summary>
    Task<List<CashierShiftDto>> GetShiftsByOperatorAsync(Guid storeUserId, DateOnly from, DateOnly to, CancellationToken ct = default);
}
