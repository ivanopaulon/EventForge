using EventForge.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for managing BusinessParty assignments to price lists.
/// Handles assignment, unassignment, and configuration of partner-specific pricing.
/// </summary>
public interface IPriceListBusinessPartyService
{
    /// <summary>
    /// Assegna un BusinessParty a un listino con configurazione specifica.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="dto">Dati assegnazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Relazione creata</returns>
    Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(
        Guid priceListId,
        AssignBusinessPartyToPriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rimuove l'assegnazione di un BusinessParty da un listino.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="businessPartyId">ID partner commerciale</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True se rimosso con successo</returns>
    Task<bool> RemoveBusinessPartyAsync(
        Guid priceListId,
        Guid businessPartyId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i BusinessParty assegnati a un listino.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista BusinessParty assegnati</returns>
    Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i listini assegnati a un BusinessParty.
    /// </summary>
    /// <param name="businessPartyId">ID partner commerciale</param>
    /// <param name="type">Tipo listino (opzionale)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista listini assegnati</returns>
    Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(
        Guid businessPartyId,
        PriceListType? type = null,
        CancellationToken cancellationToken = default);
}
