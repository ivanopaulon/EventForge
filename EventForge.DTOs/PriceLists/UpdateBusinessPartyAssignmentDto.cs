using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO per aggiornare la configurazione di un BusinessParty già assegnato a un PriceList.
/// </summary>
public class UpdateBusinessPartyAssignmentDto
{
    /// <summary>
    /// Indica se è il partner principale.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Priorità override (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? OverridePriority { get; set; }

    /// <summary>
    /// Sconto globale percentuale (-100 a +100).
    /// </summary>
    [Range(-100, 100)]
    public decimal? GlobalDiscountPercentage { get; set; }

    /// <summary>
    /// Validità specifica - inizio.
    /// </summary>
    public DateTime? SpecificValidFrom { get; set; }

    /// <summary>
    /// Validità specifica - fine.
    /// </summary>
    public DateTime? SpecificValidTo { get; set; }

    /// <summary>
    /// Note.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
