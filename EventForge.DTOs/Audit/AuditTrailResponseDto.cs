namespace EventForge.DTOs.Audit;

public class AuditTrailResponseDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityDisplayName { get; set; }
    public Guid EntityId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
