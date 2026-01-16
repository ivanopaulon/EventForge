using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Storico delle transizioni di stato di un documento.
/// Garantisce audit trail completo per compliance.
/// </summary>
public class DocumentStatusHistory : AuditableEntity
{
    [Required]
    public Guid DocumentHeaderId { get; set; }
    public DocumentHeader? DocumentHeader { get; set; }
    
    [Required]
    public DocumentStatus FromStatus { get; set; }
    
    [Required]
    public DocumentStatus ToStatus { get; set; }
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    [Required]
    [StringLength(256)]
    public string ChangedBy { get; set; } = string.Empty;
    
    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public string? MetadataJson { get; set; }
}
