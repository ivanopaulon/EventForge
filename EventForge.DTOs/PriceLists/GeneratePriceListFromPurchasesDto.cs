using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request per generare un nuovo listino da documenti di acquisto
/// </summary>
public class GeneratePriceListFromPurchasesDto
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
    /// Fornitore OBBLIGATORIO per listini acquisto
    /// </summary>
    [Required(ErrorMessage = "Il fornitore è obbligatorio per i listini acquisto")]
    public required Guid SupplierId { get; init; }
    
    /// <summary>
    /// Data inizio range analisi documenti
    /// </summary>
    [Required]
    public required DateTime FromDate { get; init; }
    
    /// <summary>
    /// Data fine range analisi documenti
    /// </summary>
    [Required]
    public required DateTime ToDate { get; init; }
    
    /// <summary>
    /// Strategia di calcolo prezzo
    /// </summary>
    public PriceCalculationStrategy CalculationStrategy { get; init; } = PriceCalculationStrategy.LastPurchasePrice;
    
    /// <summary>
    /// Arrotondamento da applicare (riusa RoundingStrategy da PR #2)
    /// </summary>
    public RoundingStrategy RoundingStrategy { get; init; } = RoundingStrategy.None;
    
    /// <summary>
    /// Maggiorazione percentuale da applicare (es. +10% sul prezzo calcolato)
    /// Range: -100% a +1000%
    /// </summary>
    [Range(-100, 1000, ErrorMessage = "La maggiorazione deve essere tra -100% e +1000%")]
    public decimal? MarkupPercentage { get; init; }
    
    /// <summary>
    /// Filtro per categorie prodotti (opzionale)
    /// </summary>
    public List<Guid>? FilterByCategoryIds { get; init; }
    
    /// <summary>
    /// Solo prodotti attivi
    /// </summary>
    public bool OnlyActiveProducts { get; init; } = true;
    
    /// <summary>
    /// Quantità minima totale negli ordini per includere il prodotto
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "La quantità minima deve essere maggiore di 0")]
    public decimal? MinimumQuantity { get; init; }
}
