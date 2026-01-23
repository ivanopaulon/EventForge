using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing stock reconciliation operations.
/// </summary>
public interface IStockReconciliationService
{
    /// <summary>
    /// Calculates stock reconciliation preview based on the provided criteria.
    /// </summary>
    Task<StockReconciliationResultDto?> CalculateReconciliationAsync(StockReconciliationRequestDto request);

    /// <summary>
    /// Applies the calculated reconciliation corrections to the stock.
    /// </summary>
    Task<StockReconciliationApplyResultDto?> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request);

    /// <summary>
    /// Exports stock reconciliation data to Excel format.
    /// </summary>
    Task<byte[]?> ExportReconciliationAsync(StockReconciliationRequestDto request);
}
