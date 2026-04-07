namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO per la relazione PriceList-BusinessParty.
/// </summary>
public class PriceListBusinessPartyDto
{
    /// <summary>
    /// ID del BusinessParty.
    /// </summary>
    public Guid BusinessPartyId { get; set; }

    /// <summary>
    /// Nome del BusinessParty.
    /// </summary>
    public string BusinessPartyName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo di BusinessParty (Cliente, Fornitore, etc.).
    /// </summary>
    public string BusinessPartyType { get; set; } = string.Empty;

    /// <summary>
    /// Indica se è il partner principale.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Priorità override.
    /// </summary>
    public int? OverridePriority { get; set; }

    /// <summary>
    /// Validità specifica - inizio.
    /// </summary>
    public DateTime? SpecificValidFrom { get; set; }

    /// <summary>
    /// Validità specifica - fine.
    /// </summary>
    public DateTime? SpecificValidTo { get; set; }

    /// <summary>
    /// Sconto globale percentuale.
    /// </summary>
    public decimal? GlobalDiscountPercentage { get; set; }

    /// <summary>
    /// Note.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Stato della relazione.
    /// </summary>
    public string Status { get; set; } = "Active";
}
