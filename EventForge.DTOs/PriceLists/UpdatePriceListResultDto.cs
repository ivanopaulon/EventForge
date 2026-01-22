using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// Risultato aggiornamento listino da documenti
/// </summary>
public class UpdatePriceListResultDto
{
    public Guid PriceListId { get; init; }
    public string PriceListName { get; init; } = string.Empty;
    
    /// <summary>
    /// Statistiche modifiche
    /// </summary>
    public int PricesUpdated { get; init; }
    public int PricesAdded { get; init; }
    public int PricesRemoved { get; init; }
    public int PricesUnchanged { get; init; }
    
    /// <summary>
    /// Timestamp sincronizzazione
    /// </summary>
    public DateTime SyncedAt { get; init; }
    public string SyncedBy { get; init; } = string.Empty;
    
    /// <summary>
    /// Eventuali warning
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}
