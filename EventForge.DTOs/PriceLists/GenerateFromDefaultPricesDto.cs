using System;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request per generare un nuovo listino dai prezzi default dei prodotti
/// </summary>
public class GenerateFromDefaultPricesDto
{
    /// <summary>
    /// Nome del nuovo listino
    /// </summary>
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [MaxLength(200)]
    public required string Name { get; init; }
    
    /// <summary>
    /// Descrizione del listino
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; init; }
    
    /// <summary>
    /// Codice univoco del listino (generato automaticamente se null)
    /// </summary>
    [MaxLength(50)]
    public string? Code { get; init; }
    
    /// <summary>
    /// Priorità del listino (0-100, default 50)
    /// </summary>
    [Range(0, 100)]
    public int Priority { get; init; } = 50;
    
    /// <summary>
    /// Indica se questo è il listino default
    /// </summary>
    public bool IsDefault { get; init; } = false;
    
    /// <summary>
    /// Data inizio validità
    /// </summary>
    public DateTime? ValidFrom { get; init; }
    
    /// <summary>
    /// Data fine validità
    /// </summary>
    public DateTime? ValidTo { get; init; }
    
    /// <summary>
    /// Maggiorazione/sconto percentuale da applicare
    /// Range: -100% a +500%
    /// </summary>
    [Range(-100, 500)]
    public decimal? MarkupPercentage { get; init; }
    
    /// <summary>
    /// Strategia di arrotondamento da applicare
    /// </summary>
    public RoundingStrategy? RoundingStrategy { get; init; }
    
    /// <summary>
    /// Solo prodotti attivi (IsDeleted = false)
    /// </summary>
    public bool OnlyActiveProducts { get; init; } = true;
    
    /// <summary>
    /// Prezzo minimo per includere il prodotto
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal? MinimumPrice { get; init; }
}
