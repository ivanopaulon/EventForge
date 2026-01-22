using System;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Metadati sulla generazione (salvati in JSON nel campo GenerationMetadata)
/// </summary>
public class PriceListGenerationMetadata
{
    public PriceCalculationStrategy Strategy { get; set; }
    public RoundingStrategy Rounding { get; set; }
    public decimal? MarkupPercentage { get; set; }
    
    public DateTime AnalysisFromDate { get; set; }
    public DateTime AnalysisToDate { get; set; }
    
    public int DocumentsAnalyzed { get; set; }
    public int ProductsGenerated { get; set; }
    
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
}
