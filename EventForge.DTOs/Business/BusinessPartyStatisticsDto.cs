using System;

namespace EventForge.DTOs.Business;

/// <summary>
/// Statistiche aggregate per BusinessParty (pre-calcolate server-side)
/// </summary>
public class BusinessPartyStatisticsDto
{
    /// <summary>
    /// Numero totale contatti (attivi)
    /// </summary>
    public int TotalContacts { get; set; }
    
    /// <summary>
    /// Numero totale indirizzi (attivi)
    /// </summary>
    public int TotalAddresses { get; set; }
    
    /// <summary>
    /// Numero totale listini prezzi assegnati (attivi)
    /// </summary>
    public int TotalPriceLists { get; set; }
    
    /// <summary>
    /// Numero card fedeltà attive (0 se backend fidelity non implementato)
    /// </summary>
    public int ActiveFidelityCards { get; set; }
    
    /// <summary>
    /// Numero totale documenti associati
    /// </summary>
    public int TotalDocuments { get; set; }
    
    /// <summary>
    /// Data ultimo ordine/documento
    /// </summary>
    public DateTime? LastOrderDate { get; set; }
    
    /// <summary>
    /// Revenue totale anno corrente (solo documenti vendita)
    /// </summary>
    public decimal TotalRevenueYTD { get; set; }
}
