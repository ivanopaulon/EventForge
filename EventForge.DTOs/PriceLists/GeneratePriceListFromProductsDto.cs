using EventForge.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request per generare un nuovo listino da prezzi DefaultPrice dei prodotti
/// </summary>
public class GeneratePriceListFromProductsDto
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
    /// Tipo di listino (Sales/Purchase)
    /// </summary>
    public PriceListType Type { get; init; } = PriceListType.Sales;

    /// <summary>
    /// Direzione (Output per vendita, Input per acquisto)
    /// </summary>
    public PriceListDirection Direction { get; init; } = PriceListDirection.Output;

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
    /// Event opzionale
    /// </summary>
    public Guid? EventId { get; init; }

    /// <summary>
    /// Maggiorazione/Sconto percentuale da applicare ai DefaultPrice
    /// Range: -100% a +1000%
    /// </summary>
    [Range(-100, 1000, ErrorMessage = "La maggiorazione deve essere tra -100% e +1000%")]
    public decimal? MarkupPercentage { get; init; }

    /// <summary>
    /// Arrotondamento da applicare
    /// </summary>
    public RoundingStrategy RoundingStrategy { get; init; } = RoundingStrategy.None;

    /// <summary>
    /// Prezzo minimo per includere il prodotto
    /// </summary>
    public decimal? MinimumPrice { get; init; }

    /// <summary>
    /// Filtro per categorie prodotti (opzionale)
    /// </summary>
    public List<Guid>? FilterByCategoryIds { get; init; }

    /// <summary>
    /// Solo prodotti attivi
    /// </summary>
    public bool OnlyActiveProducts { get; init; } = true;

    /// <summary>
    /// Escludi prodotti con DefaultPrice = 0 o null
    /// </summary>
    public bool OnlyProductsWithPrice { get; init; } = true;

    /// <summary>
    /// BusinessParties da associare (opzionale)
    /// </summary>
    public List<Guid>? BusinessPartyIds { get; init; }
}
