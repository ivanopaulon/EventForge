using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;

namespace EventForge.DTOs.Business;

/// <summary>
/// DTO aggregato con tutti i dati necessari per visualizzare BusinessPartyDetail completo.
/// Ottimizzazione FASE 5: riduce N+1 queries da 6+ chiamate a 1 singola chiamata.
/// </summary>
public class BusinessPartyFullDetailDto
{
    /// <summary>
    /// Dati principali Business Party
    /// </summary>
    public BusinessPartyDto BusinessParty { get; set; } = null!;

    /// <summary>
    /// Lista contatti associati (ordinati per IsPrimary DESC, poi ContactType)
    /// </summary>
    public List<ContactDto> Contacts { get; set; } = new();

    /// <summary>
    /// Lista indirizzi associati (ordinati per AddressType)
    /// </summary>
    public List<AddressDto> Addresses { get; set; } = new();

    /// <summary>
    /// Listini prezzi assegnati (solo attivi, ordinati per IsDefault DESC, poi Priority)
    /// </summary>
    public List<PriceListDto> AssignedPriceLists { get; set; } = new();

    /// <summary>
    /// Statistiche aggregate per badge counts e dashboard
    /// </summary>
    public BusinessPartyStatisticsDto Statistics { get; set; } = new();
}
