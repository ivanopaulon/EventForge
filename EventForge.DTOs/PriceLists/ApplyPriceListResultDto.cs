using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Risultato dell'applicazione di un listino ai prodotti
/// </summary>
public class ApplyPriceListResultDto
{
    /// <summary>
    /// ID del listino applicato
    /// </summary>
    public Guid PriceListId { get; init; }
    
    /// <summary>
    /// Nome del listino
    /// </summary>
    public string PriceListName { get; init; } = string.Empty;
    
    /// <summary>
    /// Numero di prodotti aggiornati
    /// </summary>
    public int ProductsUpdated { get; init; }
    
    /// <summary>
    /// Numero di prodotti saltati
    /// </summary>
    public int ProductsSkipped { get; init; }
    
    /// <summary>
    /// Numero di prodotti non trovati
    /// </summary>
    public int ProductsNotFound { get; init; }
    
    /// <summary>
    /// Dettagli delle modifiche
    /// </summary>
    public List<ProductPriceUpdateDetail> UpdateDetails { get; init; } = new();
    
    /// <summary>
    /// Data e ora dell'applicazione
    /// </summary>
    public DateTime AppliedAt { get; init; }
    
    /// <summary>
    /// Utente che ha eseguito l'applicazione
    /// </summary>
    public string AppliedBy { get; init; } = string.Empty;
}

/// <summary>
/// Dettaglio dell'aggiornamento prezzo di un prodotto
/// </summary>
public class ProductPriceUpdateDetail
{
    /// <summary>
    /// ID del prodotto
    /// </summary>
    public Guid ProductId { get; init; }
    
    /// <summary>
    /// Nome del prodotto
    /// </summary>
    public string ProductName { get; init; } = string.Empty;
    
    /// <summary>
    /// Codice del prodotto
    /// </summary>
    public string ProductCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Prezzo precedente
    /// </summary>
    public decimal OldPrice { get; init; }
    
    /// <summary>
    /// Nuovo prezzo
    /// </summary>
    public decimal NewPrice { get; init; }
    
    /// <summary>
    /// Motivo dell'aggiornamento o del salto
    /// </summary>
    public string UpdateReason { get; init; } = string.Empty;
}
