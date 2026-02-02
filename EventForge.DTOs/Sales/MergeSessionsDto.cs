using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales;

/// <summary>
/// Request DTO for merging multiple sale sessions.
/// </summary>
public class MergeSessionsDto
{
    /// <summary>
    /// IDs of sessions to merge (minimum 2 required).
    /// </summary>
    [Required]
    [MinLength(2, ErrorMessage = "Almeno 2 sessioni richieste per il merge")]
    public List<Guid> SessionIds { get; set; } = new();
    
    /// <summary>
    /// Target table ID for the merged session (optional).
    /// If null, uses the table from the first session.
    /// </summary>
    public Guid? TargetTableId { get; set; }
    
    /// <summary>
    /// Reason or notes for the merge operation (optional).
    /// </summary>
    [MaxLength(500)]
    public string? MergeReason { get; set; }
}
