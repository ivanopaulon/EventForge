using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.Server.Data.Entities.PriceList;


/// <summary>
/// Represents a price list that can be used for one or more events.
/// </summary>
public class PriceList : AuditableEntity
{
    /// <summary>
    /// Name of the price list.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name of the price list.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the price list.
    /// </summary>
    [MaxLength(50, ErrorMessage = "The code cannot exceed 50 characters.")]
    [Display(Name = "Code", Description = "Unique code for the price list.")]
    public string? Code { get; set; }

    /// <summary>
    /// Description of the price list.
    /// </summary>
    [MaxLength(500, ErrorMessage = "The description cannot exceed 500 characters.")]
    [Display(Name = "Description", Description = "Description of the price list.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the price list validity.
    /// </summary>
    [Display(Name = "Valid From", Description = "Start date of the price list validity.")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End date of the price list validity.
    /// </summary>
    [Display(Name = "Valid To", Description = "End date of the price list validity.")]
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Additional notes for the price list.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The notes cannot exceed 1000 characters.")]
    [Display(Name = "Notes", Description = "Additional notes for the price list.")]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Status of the price list.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the price list.")]
    public PriceListStatus Status { get; set; } = PriceListStatus.Active;

    /// <summary>
    /// Indicates if this is the default price list for the event.
    /// </summary>
    [Display(Name = "Default", Description = "Indicates if this is the default price list for the event.")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Priority of the price list (0 = highest priority).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Priority must be between 0 and 100.")]
    [Display(Name = "Priority", Description = "Priority of the price list (0 = highest priority).")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Tipo di listino (vendita o acquisto).
    /// </summary>
    [Required]
    [Display(Name = "Type", Description = "Tipo di listino (vendita o acquisto).")]
    public PriceListType Type { get; set; } = PriceListType.Sales;

    /// <summary>
    /// Direzione del listino (output = vendita, input = acquisto).
    /// </summary>
    [Required]
    [Display(Name = "Direction", Description = "Direzione del listino.")]
    public PriceListDirection Direction { get; set; } = PriceListDirection.Output;

    /// <summary>
    /// Event associato al listino (opzionale, null per listini acquisto generici).
    /// </summary>
    [Display(Name = "Event", Description = "Event associato al listino.")]
    public Guid? EventId { get; set; }

    /// <summary>
    /// Navigation property for the associated event.
    /// </summary>
    public Event? Event { get; set; }

    /// <summary>
    /// Product prices associated with this price list.
    /// </summary>
    [Display(Name = "Product Prices", Description = "Product prices associated with this price list.")]
    public ICollection<PriceListEntry> ProductPrices { get; set; } = new List<PriceListEntry>();

    /// <summary>
    /// Relazione many-to-many con BusinessParty.
    /// </summary>
    [Display(Name = "Business Parties", Description = "Partner commerciali assegnati a questo listino.")]
    public ICollection<PriceListBusinessParty> BusinessParties { get; set; } = new List<PriceListBusinessParty>();

    /// <summary>
    /// Indica se il listino è stato generato automaticamente da documenti di carico
    /// </summary>
    [Display(Name = "Is Generated From Documents", Description = "Indica se il listino è stato generato automaticamente da documenti di carico.")]
    public bool IsGeneratedFromDocuments { get; set; } = false;

    /// <summary>
    /// Metadati sulla generazione (JSON serializzato)
    /// Contiene: strategia usata, range date, numero documenti analizzati, etc.
    /// </summary>
    [MaxLength(4000)]
    [Display(Name = "Generation Metadata", Description = "Metadati sulla generazione (JSON serializzato).")]
    public string? GenerationMetadata { get; set; }

    /// <summary>
    /// Data/ora ultimo aggiornamento da documenti
    /// </summary>
    [Display(Name = "Last Synced At", Description = "Data/ora ultimo aggiornamento da documenti.")]
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Utente che ha eseguito l'ultimo sync
    /// </summary>
    [MaxLength(256)]
    [Display(Name = "Last Synced By", Description = "Utente che ha eseguito l'ultimo sync.")]
    public string? LastSyncedBy { get; set; }
}

/// <summary>
/// Status for the price list.
/// </summary>
public enum PriceListStatus
{
    Draft,      // Bozza (non applicato)
    Active,     // Attivo
    Suspended,  // Sospeso temporaneamente
    Expired,    // Scaduto (ValidTo passato)
    Archived,   // Archiviato
    Deleted     // Eliminato (soft delete)
}