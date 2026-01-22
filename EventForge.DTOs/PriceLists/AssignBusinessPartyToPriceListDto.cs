using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO per assegnare un BusinessParty a un PriceList.
/// </summary>
public class AssignBusinessPartyToPriceListDto
{
    /// <summary>
    /// ID del BusinessParty da assegnare.
    /// </summary>
    [Required]
    public Guid BusinessPartyId { get; set; }

    /// <summary>
    /// Indica se è il partner principale.
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Priorità override.
    /// </summary>
    [Range(0, 100)]
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
    [Range(-100, 100)]
    public decimal? GlobalDiscountPercentage { get; set; }

    /// <summary>
    /// Note.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
