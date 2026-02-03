namespace EventForge.DTOs.Audit;

public class AuditTrailSearchDto
{
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? OperationType { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
