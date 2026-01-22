using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Business;

namespace EventForge.Server.Data.Entities.PriceList;

/// <summary>
/// Relazione many-to-many tra PriceList e BusinessParty.
/// Permette di assegnare un listino a più partner commerciali con configurazioni specifiche.
/// </summary>
public class PriceListBusinessParty : AuditableEntity
{
    /// <summary>
    /// Foreign key al listino.
    /// </summary>
    [Required]
    [Display(Name = "Price List", Description = "Listino associato.")]
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Navigation property al listino.
    /// </summary>
    public PriceList PriceList { get; set; } = null!;

    /// <summary>
    /// Foreign key al partner commerciale.
    /// </summary>
    [Required]
    [Display(Name = "Business Party", Description = "Partner commerciale associato.")]
    public Guid BusinessPartyId { get; set; }

    /// <summary>
    /// Navigation property al partner commerciale.
    /// </summary>
    public BusinessParty BusinessParty { get; set; } = null!;

    /// <summary>
    /// Indica se questo partner è quello principale per questo listino.
    /// </summary>
    [Display(Name = "Primary", Description = "Partner principale per questo listino.")]
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Priorità specifica per questo partner (override della priorità generale del listino).
    /// Se null, usa la priorità del listino.
    /// </summary>
    [Range(0, 100)]
    [Display(Name = "Override Priority", Description = "Priorità specifica per questo partner.")]
    public int? OverridePriority { get; set; }

    /// <summary>
    /// Data inizio validità per questo partner specifico.
    /// Se null, usa ValidFrom del listino.
    /// </summary>
    [Display(Name = "Specific Valid From", Description = "Data inizio validità specifica.")]
    public DateTime? SpecificValidFrom { get; set; }

    /// <summary>
    /// Data fine validità per questo partner specifico.
    /// Se null, usa ValidTo del listino.
    /// </summary>
    [Display(Name = "Specific Valid To", Description = "Data fine validità specifica.")]
    public DateTime? SpecificValidTo { get; set; }

    /// <summary>
    /// Sconto globale da applicare a tutti i prezzi del listino per questo partner.
    /// Espresso in percentuale (es: 5.0 = 5%).
    /// Valori negativi = maggiorazione.
    /// </summary>
    [Range(-100, 100)]
    [Display(Name = "Global Discount %", Description = "Sconto percentuale globale.")]
    public decimal? GlobalDiscountPercentage { get; set; }

    /// <summary>
    /// Note specifiche per questo partner.
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "Notes", Description = "Note specifiche.")]
    public string? Notes { get; set; }

    /// <summary>
    /// Stato della relazione.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Stato della relazione.")]
    public PriceListBusinessPartyStatus Status { get; set; } = PriceListBusinessPartyStatus.Active;
}

/// <summary>
/// Stato della relazione PriceList-BusinessParty.
/// </summary>
public enum PriceListBusinessPartyStatus
{
    Active,     // Attiva
    Suspended,  // Sospesa
    Deleted     // Eliminata
}
