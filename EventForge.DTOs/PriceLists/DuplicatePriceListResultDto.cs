using EventForge.DTOs.Common;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Risultato dell'operazione di duplicazione listino
/// </summary>
public record DuplicatePriceListResultDto
{
    /// <summary>
    /// Listino originale
    /// </summary>
    public Guid SourcePriceListId { get; init; }
    public string SourcePriceListName { get; init; } = string.Empty;

    /// <summary>
    /// Nuovo listino creato
    /// </summary>
    public required PriceListDto NewPriceList { get; init; }

    /// <summary>
    /// Statistiche duplicazione
    /// </summary>
    public int SourcePriceCount { get; init; }
    public int CopiedPriceCount { get; init; }
    public int SkippedPriceCount { get; init; }
    public int CopiedBusinessPartyCount { get; init; }

    /// <summary>
    /// Maggiorazione applicata
    /// </summary>
    public decimal? AppliedMarkupPercentage { get; init; }

    /// <summary>
    /// Arrotondamento applicato
    /// </summary>
    public RoundingStrategy? AppliedRoundingStrategy { get; init; }

    /// <summary>
    /// Timestamp operazione
    /// </summary>
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}
