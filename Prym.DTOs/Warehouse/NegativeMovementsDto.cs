namespace Prym.DTOs.Warehouse;

/// <summary>
/// Summary result of the negative-movements normalization operation.
/// </summary>
public class NegativeMovementsReportDto
{
    /// <summary>Total number of StockMovement rows with Quantity &lt; 0.</summary>
    public int TotalNegative { get; set; }

    /// <summary>Individual anomalous movements found.</summary>
    public List<NegativeMovementItemDto> Items { get; set; } = new();
}

/// <summary>
/// A single StockMovement row with a negative quantity.
/// </summary>
public class NegativeMovementItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? WarehouseName { get; set; }
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime MovementDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of a batch fix of negative-quantity movements.
/// </summary>
public class FixNegativeMovementsResultDto
{
    /// <summary>Whether this was a dry run (no changes persisted).</summary>
    public bool IsDryRun { get; set; }

    /// <summary>Number of movements whose Quantity sign was flipped.</summary>
    public int MovementsFixed { get; set; }

    /// <summary>Number of stock rows whose balance was corrected.</summary>
    public int StocksAdjusted { get; set; }

    /// <summary>Number of movements that could not be fixed.</summary>
    public int Errors { get; set; }
}
