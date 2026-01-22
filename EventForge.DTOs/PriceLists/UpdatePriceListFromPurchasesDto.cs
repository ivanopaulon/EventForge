using System;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request per aggiornare un listino esistente con prezzi da documenti
/// </summary>
public class UpdatePriceListFromPurchasesDto
{
    /// <summary>
    /// ID del listino da aggiornare
    /// </summary>
    [Required]
    public required Guid PriceListId { get; init; }
    
    /// <summary>
    /// Range date analisi (default: ultimi 90 giorni se null)
    /// </summary>
    public DateTime? FromDate { get; init; }
    
    /// <summary>
    /// Range date analisi (default: oggi se null)
    /// </summary>
    public DateTime? ToDate { get; init; }
    
    /// <summary>
    /// Strategia di calcolo prezzo
    /// </summary>
    public PriceCalculationStrategy CalculationStrategy { get; init; } = PriceCalculationStrategy.LastPurchasePrice;
    
    /// <summary>
    /// Arrotondamento da applicare
    /// </summary>
    public RoundingStrategy RoundingStrategy { get; init; } = RoundingStrategy.None;
    
    /// <summary>
    /// Maggiorazione percentuale da applicare
    /// </summary>
    [Range(-100, 1000)]
    public decimal? MarkupPercentage { get; init; }
    
    /// <summary>
    /// Se true, aggiunge nuovi prodotti trovati nei documenti
    /// </summary>
    public bool AddNewProducts { get; init; } = false;
    
    /// <summary>
    /// Se true, rimuove prodotti non più presenti nei documenti del range
    /// </summary>
    public bool RemoveObsoleteProducts { get; init; } = false;
}
