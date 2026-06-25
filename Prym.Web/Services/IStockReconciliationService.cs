using Prym.DTOs.Warehouse;

namespace Prym.Web.Services;

/// <summary>
/// Service for managing stock reconciliation operations.
/// </summary>
public interface IStockReconciliationService
{
    /// <summary>
    /// Calculates stock reconciliation preview based on the provided criteria.
    /// </summary>
    Task<StockReconciliationResultDto?> CalculateReconciliationAsync(StockReconciliationRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the stock ids that match the reconciliation filters.
    /// </summary>
    Task<List<Guid>?> GetReconciliationStockIdsAsync(StockReconciliationRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Calculates reconciliation for a specific batch of stock ids.
    /// </summary>
    Task<StockReconciliationResultDto?> CalculateReconciliationBatchAsync(StockReconciliationBatchRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Applies the calculated reconciliation corrections to the stock.
    /// </summary>
    Task<StockReconciliationApplyResultDto?> ApplyReconciliationAsync(StockReconciliationApplyRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Exports stock reconciliation data to Excel format.
    /// </summary>
    Task<byte[]?> ExportReconciliationAsync(StockReconciliationRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Previews which stock movements would be rebuilt from approved/closed documents (dry-run).
    /// Does NOT create any movements.
    /// </summary>
    Task<RebuildMovementsResultDto?> RebuildMovementsPreviewAsync(RebuildMovementsRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds missing stock movements from approved/closed documents.
    /// Creates stock movements for document rows that do not yet have a corresponding movement.
    /// </summary>
    Task<RebuildMovementsResultDto?> RebuildMovementsExecuteAsync(RebuildMovementsRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all StockMovements with Quantity &lt; 0 for the current tenant (legacy anomalies).
    /// </summary>
    Task<NegativeMovementsReportDto?> GetNegativeMovementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalises negative-quantity StockMovements by flipping their sign.
    /// Pass dryRun=true to preview without persisting.
    /// </summary>
    Task<FixNegativeMovementsResultDto?> FixNegativeMovementsAsync(bool dryRun, CancellationToken cancellationToken = default);
}
