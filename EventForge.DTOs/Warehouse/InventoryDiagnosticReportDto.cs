namespace EventForge.DTOs.Warehouse;

public class InventoryDiagnosticReportDto
{
    public Guid DocumentId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int TotalRows { get; set; }
    public int TotalIssues { get; set; }
    public bool IsHealthy { get; set; }
    public List<InventoryDiagnosticIssue> Issues { get; set; } = new();
    public InventoryDiagnosticStats Stats { get; set; } = new();
}

public class InventoryDiagnosticIssue
{
    public Guid RowId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool CanAutoFix { get; set; }
}

public class InventoryDiagnosticStats
{
    public int RowsWithMissingData { get; set; }
    public int DuplicateProducts { get; set; }
    public int InvalidReferences { get; set; }
    public int NegativeQuantities { get; set; }
}
