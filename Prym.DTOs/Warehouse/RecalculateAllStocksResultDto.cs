namespace Prym.DTOs.Warehouse;

/// <summary>
/// Result of the standalone "recalculate all stock quantities from movement history" operation.
/// </summary>
public class RecalculateAllStocksResultDto
{
    /// <summary>Whether this was a dry-run (preview only, no stocks written).</summary>
    public bool IsDryRun { get; set; }

    /// <summary>Total number of distinct product/location pairs found in movement history.</summary>
    public int PairsScanned { get; set; }

    /// <summary>
    /// Number of existing Stock rows whose quantity differed from the movement net and was
    /// (or would be in dry-run) updated.
    /// </summary>
    public int StocksUpdated { get; set; }

    /// <summary>
    /// Number of product/location pairs that had no Stock record yet and were
    /// (or would be in dry-run) created.
    /// </summary>
    public int StocksCreated { get; set; }

    /// <summary>Number of existing Stock rows whose quantity already matched the movement net (no change).</summary>
    public int StocksAlreadyCorrect { get; set; }

    /// <summary>Number of pairs that could not be processed due to an error.</summary>
    public int Errors { get; set; }
}
