using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.Business;

/// <summary>
/// Rappresenta un gruppo di Business Party (clienti, fornitori, o entrambi).
/// Utilizzato per applicare promozioni, listini, o politiche specifiche a insiemi di partner.
/// </summary>
public class BusinessPartyGroup : AuditableEntity
{
    [Required(ErrorMessage = "Il nome del gruppo è obbligatorio")]
    [StringLength(100, ErrorMessage = "Il nome non può superare 100 caratteri")]
    [Display(Name = "Group Name", Description = "Nome del gruppo di Business Party")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Group Code", Description = "Codice identificativo univoco")]
    public string? Code { get; set; }

    [StringLength(500)]
    [Display(Name = "Description", Description = "Descrizione del gruppo")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Group Type", Description = "Tipo di gruppo (Cliente, Fornitore, Entrambi)")]
    public BusinessPartyGroupType GroupType { get; set; } = BusinessPartyGroupType.Customer;

    [StringLength(7, MinimumLength = 7, ErrorMessage = "Il colore deve essere in formato #RRGGBB")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Formato colore non valido")]
    [Display(Name = "Color", Description = "Colore esadecimale per badge UI")]
    public string? ColorHex { get; set; } = "#1976D2";

    [StringLength(50)]
    [Display(Name = "Icon", Description = "Icona MaterialDesign per visualizzazione")]
    public string? Icon { get; set; } = "Group";

    [Range(0, 100, ErrorMessage = "La priorità deve essere tra 0 e 100")]
    [Display(Name = "Priority", Description = "Priorità del gruppo (0-100)")]
    public int Priority { get; set; } = 50;

    [Display(Name = "Valid From", Description = "Data inizio validità")]
    public DateTime? ValidFrom { get; set; }

    [Display(Name = "Valid To", Description = "Data fine validità")]
    public DateTime? ValidTo { get; set; }

    [Display(Name = "Members", Description = "Business Party appartenenti al gruppo")]
    public ICollection<BusinessPartyGroupMember> Members { get; set; } = new List<BusinessPartyGroupMember>();

    [Display(Name = "Metadata", Description = "Metadati aggiuntivi in formato JSON")]
    public string? MetadataJson { get; set; }
}
