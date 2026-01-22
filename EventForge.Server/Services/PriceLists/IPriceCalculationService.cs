using EventForge.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for price calculation and precedence logic.
/// Handles applied price calculation, unit conversion, and price history.
/// </summary>
public interface IPriceCalculationService
{
    /// <summary>
    /// Ottiene il prezzo applicato per un prodotto considerando precedenza listini.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="eventId">ID evento</param>
    /// <param name="businessPartyId">ID partner commerciale (opzionale)</param>
    /// <param name="evaluationDate">Data valutazione (default: now)</param>
    /// <param name="quantity">Quantità (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prezzo applicato o null se non trovato</returns>
    Task<AppliedPriceDto?> GetAppliedPriceAsync(
        Guid productId,
        Guid eventId,
        Guid? businessPartyId = null,
        DateTime? evaluationDate = null,
        int quantity = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene il prezzo applicato con conversione unità di misura.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="eventId">ID evento</param>
    /// <param name="targetUnitId">ID unità di misura target</param>
    /// <param name="evaluationDate">Data valutazione</param>
    /// <param name="quantity">Quantità</param>
    /// <param name="businessPartyId">ID partner commerciale</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prezzo applicato con conversione</returns>
    Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(
        Guid productId,
        Guid eventId,
        Guid targetUnitId,
        DateTime? evaluationDate = null,
        int quantity = 1,
        Guid? businessPartyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene lo storico prezzi per un prodotto.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="eventId">ID evento</param>
    /// <param name="fromDate">Data inizio (opzionale)</param>
    /// <param name="toDate">Data fine (opzionale)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista storico prezzi</returns>
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(
        Guid productId,
        Guid eventId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene il prezzo per un prodotto con SearchPath dettagliato per debugging.
    /// </summary>
    /// <param name="request">Richiesta calcolo prezzo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato con path di ricerca</returns>
    Task<ProductPriceResultDto> GetProductPriceAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confronta prezzi di acquisto da diversi fornitori.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="quantity">Quantità (default: 1)</param>
    /// <param name="evaluationDate">Data valutazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista confronto prezzi fornitori</returns>
    Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(
        Guid productId,
        int quantity = 1,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default);
}
