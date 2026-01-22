using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Preview della generazione listino (senza salvataggio)
/// </summary>
public class GeneratePriceListPreviewDto
{
    /// <summary>
    /// Numero totale documenti analizzati
    /// </summary>
    public int TotalDocumentsAnalyzed { get; init; }
    
    /// <summary>
    /// Numero prodotti distinti trovati
    /// </summary>
    public int TotalProductsFound { get; init; }
    
    /// <summary>
    /// Prodotti con prezzi multipli nei documenti
    /// </summary>
    public int ProductsWithMultiplePrices { get; init; }
    
    /// <summary>
    /// Numero prodotti esclusi per filtri
    /// </summary>
    public int ProductsExcluded { get; init; }
    
    /// <summary>
    /// Lista preview prezzi per prodotto
    /// </summary>
    public List<ProductPricePreview> ProductPreviews { get; init; } = new();
    
    /// <summary>
    /// Valore totale stimato del listino
    /// </summary>
    public decimal TotalEstimatedValue { get; init; }
    
    /// <summary>
    /// Range date analisi
    /// </summary>
    public DateTime AnalysisFromDate { get; init; }
    public DateTime AnalysisToDate { get; init; }
}

/// <summary>
/// Preview prezzo singolo prodotto
/// </summary>
public class ProductPricePreview
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Prezzo calcolato secondo strategia
    /// </summary>
    public decimal CalculatedPrice { get; init; }
    
    /// <summary>
    /// Prezzo prima arrotondamento/maggiorazione
    /// </summary>
    public decimal OriginalPrice { get; init; }
    
    /// <summary>
    /// Numero occorrenze nei documenti
    /// </summary>
    public int OccurrencesInDocuments { get; init; }
    
    /// <summary>
    /// Statistiche prezzi trovati
    /// </summary>
    public decimal? LowestPrice { get; init; }
    public decimal? HighestPrice { get; init; }
    public decimal? AveragePrice { get; init; }
    
    /// <summary>
    /// Data ultimo acquisto
    /// </summary>
    public DateTime? LastPurchaseDate { get; init; }
}
