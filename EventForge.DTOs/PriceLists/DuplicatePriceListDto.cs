using EventForge.DTOs.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.PriceLists;

/// <summary>
/// DTO per duplicare un listino esistente
/// </summary>
public record DuplicatePriceListDto
{
    /// <summary>
    /// Nome del nuovo listino
    /// </summary>
    [Required, MaxLength(100)]
    public required string Name { get; init; }
    
    /// <summary>
    /// Descrizione del nuovo listino
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; init; }
    
    /// <summary>
    /// Codice univoco del nuovo listino (opzionale, generato automaticamente se null)
    /// </summary>
    [MaxLength(50)]
    public string? Code { get; init; }
    
    /// <summary>
    /// Se true, copia tutte le voci di prezzo dal listino originale
    /// </summary>
    public bool CopyPrices { get; init; } = true;
    
    /// <summary>
    /// Se true, copia anche le assegnazioni ai BusinessParty
    /// </summary>
    public bool CopyBusinessParties { get; init; } = false;
    
    /// <summary>
    /// Maggiorazione/sconto percentuale da applicare ai prezzi copiati
    /// Esempio: 10.0 = +10%, -5.0 = -5%
    /// </summary>
    public decimal? ApplyMarkupPercentage { get; init; }
    
    /// <summary>
    /// Strategia di arrotondamento da applicare ai prezzi
    /// </summary>
    public RoundingStrategy? RoundingStrategy { get; init; }
    
    /// <summary>
    /// Nuova data inizio validità
    /// </summary>
    public DateTime? NewValidFrom { get; init; }
    
    /// <summary>
    /// Nuova data fine validità
    /// </summary>
    public DateTime? NewValidTo { get; init; }
    
    /// <summary>
    /// Nuova priorità del listino
    /// </summary>
    public int? NewPriority { get; init; }
    
    /// <summary>
    /// Nuovo evento a cui assegnare il listino (opzionale)
    /// </summary>
    public Guid? NewEventId { get; init; }
    
    /// <summary>
    /// Nuovo tipo del listino (opzionale, default = stesso del listino originale)
    /// </summary>
    public PriceListType? NewType { get; init; }
    
    /// <summary>
    /// Nuova direzione (opzionale, default = stesso del listino originale)
    /// </summary>
    public PriceListDirection? NewDirection { get; init; }
    
    /// <summary>
    /// Status iniziale del nuovo listino (default = Active)
    /// </summary>
    public PriceListStatus NewStatus { get; init; } = PriceListStatus.Active;
    
    // === FILTRI (per copia parziale) ===
    
    /// <summary>
    /// Se specificato, copia SOLO i prodotti in questo elenco
    /// </summary>
    public List<Guid>? FilterByProductIds { get; init; }
    
    /// <summary>
    /// Se specificato, copia SOLO i prodotti di queste categorie
    /// </summary>
    public List<Guid>? FilterByCategoryIds { get; init; }
    
    /// <summary>
    /// Se true, copia solo i prodotti attivi
    /// </summary>
    public bool OnlyActiveProducts { get; init; } = true;
}
