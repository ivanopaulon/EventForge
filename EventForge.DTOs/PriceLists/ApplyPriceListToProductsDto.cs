using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Request per applicare prezzi da listino ai Product.DefaultPrice
/// </summary>
public class ApplyPriceListToProductsDto
{
    /// <summary>
    /// ID del listino da applicare
    /// </summary>
    [Required]
    public required Guid PriceListId { get; init; }
    
    /// <summary>
    /// Modalità di applicazione
    /// </summary>
    public PriceListApplicationMode ApplicationMode { get; init; } = PriceListApplicationMode.UpdateExisting;
    
    /// <summary>
    /// Aggiorna solo se prezzo listino > DefaultPrice
    /// </summary>
    public bool OnlyUpdateIfHigher { get; init; } = false;
    
    /// <summary>
    /// Aggiorna solo se prezzo listino < DefaultPrice
    /// </summary>
    public bool OnlyUpdateIfLower { get; init; } = false;
    
    /// <summary>
    /// Crea backup dei prezzi precedenti in audit log
    /// </summary>
    public bool CreateBackup { get; init; } = true;
    
    /// <summary>
    /// Filtro per prodotti specifici (opzionale)
    /// </summary>
    public List<Guid>? FilterByProductIds { get; init; }
    
    /// <summary>
    /// Filtro per categorie prodotti (opzionale)
    /// </summary>
    public List<Guid>? FilterByCategoryIds { get; init; }
}

/// <summary>
/// Modalità di applicazione prezzi da listino a prodotti
/// </summary>
public enum PriceListApplicationMode
{
    /// <summary>
    /// Aggiorna solo prodotti già nel listino
    /// </summary>
    UpdateExisting = 0,
    
    /// <summary>
    /// Aggiorna tutti i prodotti (anche se non nel listino, usa DefaultPrice)
    /// </summary>
    UpdateAll = 1
}
