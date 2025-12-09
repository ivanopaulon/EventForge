using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Warehouse;

public class InventoryRepairResultDto
{
    public Guid DocumentId { get; set; }
    public DateTime RepairedAt { get; set; }
    public int RowsAnalyzed { get; set; }
    public int RowsCorrected { get; set; }
    public int RowsRemoved { get; set; }
    public List<string> ActionsPerformed { get; set; } = new();
}
