using Prym.DTOs.Warehouse;

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
    /// Returns the stock ids matching the reconciliation filters.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetStockIdsForReconciliationAsync(
        StockReconciliationRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates reconciled stock for a specific set of stock ids.
    /// </summary>
    Task<StockReconciliationResultDto> CalculateReconciledStockForStocksAsync(
        IReadOnlyCollection<Guid> stockIds,
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

    /// <summary>
    /// Rebuilds missing stock movements from approved/closed documents.
    /// Scans all approved or closed documents and creates stock movements for rows
    /// that do not yet have a corresponding movement (identified by DocumentRowId).
    /// If DryRun is true, only previews what would be done without creating movements.
    /// </summary>
    Task<RebuildMovementsResultDto> RebuildMissingMovementsFromDocumentsAsync(
        RebuildMovementsRequestDto request,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all StockMovement rows with Quantity &lt; 0 for the current tenant.
    /// These are legacy anomalies that should be normalised via
    /// <see cref="FixNegativeMovementsAsync"/>.
    /// </summary>
    Task<NegativeMovementsReportDto> GetNegativeMovementsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Normalises negative-quantity StockMovement rows by negating their Quantity so it
    /// becomes positive.  Stock levels are also corrected for every affected
    /// product/location pair: the legacy negative value was incorrectly subtracting from
    /// the balance; flipping the sign (twice the absolute value added as correction) restores
    /// the correct net.
    /// <para>When <paramref name="dryRun"/> is <c>true</c> no changes are persisted.</para>
    /// </summary>
    Task<FixNegativeMovementsResultDto> FixNegativeMovementsAsync(
        bool dryRun,
        string currentUser,
        CancellationToken cancellationToken = default);
}
