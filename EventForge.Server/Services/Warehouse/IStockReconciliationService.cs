using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for stock reconciliation operations.
/// Provides functionality to calculate and apply stock discrepancies.
/// </summary>
public interface IStockReconciliationService
{
    /// <summary>
    /// Calculates reconciled stock based on documents, inventories, and manual movements.
    /// This method does not modify any data - it only calculates and returns discrepancies.
    /// </summary>
    /// <param name="request">Reconciliation request with filters and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation result with calculated quantities and discrepancies</returns>
    Task<StockReconciliationResultDto> CalculateReconciledStockAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies stock reconciliation corrections to selected items.
    /// Updates stock quantities and creates adjustment movements.
    /// </summary>
    /// <param name="request">Apply request with items to update and reason</param>
    /// <param name="currentUser">Current user identifier for audit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the apply operation</returns>
    Task<StockReconciliationApplyResultDto> ApplyReconciliationAsync(
        StockReconciliationApplyRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports reconciliation report as Excel file.
    /// </summary>
    /// <param name="request">Reconciliation request with filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> ExportReconciliationReportAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default);
}
