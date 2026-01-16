using EventForge.DTOs.Common;
using System;

namespace EventForge.DTOs.Documents;

public class DocumentStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid DocumentHeaderId { get; set; }
    public DocumentStatus FromStatus { get; set; }
    public DocumentStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class ChangeDocumentStatusDto
{
    public DocumentStatus NewStatus { get; set; }
    public string? Reason { get; set; }
}
