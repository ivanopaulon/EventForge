namespace Prym.DTOs.Warehouse;

/// <summary>
/// Represents a recent closed inventory document, used to offer quick-select
/// reference dates in the stock snapshot dialog.
/// </summary>
public class InventorySnapshotDateDto
{
    /// <summary>Unique identifier of the inventory DocumentHeader.</summary>
    public Guid DocumentHeaderId { get; set; }

    /// <summary>Date of the inventory document (UTC, day precision).</summary>
    public DateTime Date { get; set; }

    /// <summary>Document number shown as a label in the UI quick-select.</summary>
    public string DocumentNumber { get; set; } = string.Empty;
}
