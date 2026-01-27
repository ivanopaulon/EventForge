using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Business;

/// <summary>
/// Rappresenta l'appartenenza di un Business Party ad un gruppo.
/// Relazione Many-to-Many - un BP può appartenere a MULTIPLI gruppi.
/// </summary>
public class BusinessPartyGroupMember : AuditableEntity
{
    [Required]
    [Display(Name = "Group", Description = "Gruppo di appartenenza")]
    public Guid BusinessPartyGroupId { get; set; }

    public BusinessPartyGroup Group { get; set; } = null!;

    [Required]
    [Display(Name = "Business Party", Description = "Business Party membro")]
    public Guid BusinessPartyId { get; set; }

    public BusinessParty BusinessParty { get; set; } = null!;

    [Display(Name = "Member Since", Description = "Data inizio appartenenza")]
    public DateTime? MemberSince { get; set; } = DateTime.UtcNow;

    [Display(Name = "Member Until", Description = "Data fine appartenenza")]
    public DateTime? MemberUntil { get; set; }

    [Required]
    [Display(Name = "Status", Description = "Stato della membership")]
    public BusinessPartyGroupMemberStatus Status { get; set; } = BusinessPartyGroupMemberStatus.Active;

    [Range(0, 100)]
    [Display(Name = "Override Priority", Description = "Priorità specifica per questo membro")]
    public int? OverridePriority { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes", Description = "Note aggiuntive")]
    public string? Notes { get; set; }

    [Display(Name = "Is Featured", Description = "Indica se è un membro in evidenza")]
    public bool IsFeatured { get; set; } = false;
}
