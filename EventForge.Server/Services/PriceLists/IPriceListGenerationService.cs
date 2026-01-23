using EventForge.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for generating price lists from various sources.
/// Handles price list generation from purchase documents, product default prices, and other sources.
/// </summary>
public interface IPriceListGenerationService
{
    /// <summary>
    /// Genera un nuovo listino prezzi dai prezzi DefaultPrice dei prodotti.
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID del listino creato</returns>
    Task<Guid> GenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un nuovo listino prezzi analizzando documenti di acquisto.
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID del listino creato</returns>
    Task<Guid> GenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview della generazione da documenti di acquisto (senza salvare).
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview del listino che verrebbe generato</returns>
    Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview aggiornamento listino esistente.
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview aggiornamento</returns>
    Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiorna un listino esistente con prezzi da documenti di acquisto.
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato aggiornamento con conteggi</returns>
    Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplica un listino esistente con opzioni di trasformazione.
    /// </summary>
    /// <param name="priceListId">ID listino da duplicare</param>
    /// <param name="dto">Parametri duplicazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato duplicazione con ID nuovo listino</returns>
    Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(
        Guid priceListId,
        DuplicatePriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);
}
