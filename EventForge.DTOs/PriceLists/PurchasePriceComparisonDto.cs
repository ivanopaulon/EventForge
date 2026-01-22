using System;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO per confrontare prezzi di acquisto da diversi fornitori.
/// Utilizzato per trovare il miglior prezzo/condizioni tra i listini acquisto.
/// </summary>
public class PurchasePriceComparisonDto
{
    /// <summary>
    /// ID prodotto.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// ID fornitore.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Nome fornitore.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// ID listino.
    /// </summary>
    public Guid PriceListId { get; set; }

    /// <summary>
    /// Nome listino.
    /// </summary>
    public string PriceListName { get; set; } = string.Empty;

    /// <summary>
    /// Prezzo finale (con sconti applicati).
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Prezzo originale (senza sconti).
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Valuta.
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Tempo di consegna in giorni.
    /// </summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>
    /// Quantità minima ordine (MOQ).
    /// </summary>
    public int? MinimumOrderQuantity { get; set; }

    /// <summary>
    /// Incremento quantità.
    /// </summary>
    public int? QuantityIncrement { get; set; }

    /// <summary>
    /// Codice prodotto del fornitore.
    /// </summary>
    public string? SupplierProductCode { get; set; }

    /// <summary>
    /// Indica se è il fornitore principale.
    /// </summary>
    public bool IsPrimarySupplier { get; set; }

    /// <summary>
    /// Priorità del listino per questo fornitore.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Percentuale di sconto applicata.
    /// </summary>
    public decimal? AppliedDiscountPercentage { get; set; }
}
