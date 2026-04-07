using EventForge.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for bulk operations on price lists.
/// Handles bulk import, export, price updates, and validation.
/// </summary>
public interface IPriceListBulkOperationsService
{
    /// <summary>
    /// Importa entries in bulk da una lista DTO.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="entries">Lista entries da importare</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="replaceExisting">Se sostituire entries esistenti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato import con conteggi</returns>
    Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(
        Guid priceListId,
        IEnumerable<CreatePriceListEntryDto> entries,
        string currentUser,
        bool replaceExisting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Esporta entries di un listino in formato esportabile.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="includeInactiveEntries">Includi entries inattive</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista entries esportabili</returns>
    Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(
        Guid priceListId,
        bool includeInactiveEntries = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiornamento massivo prezzi con preview.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview modifiche</returns>
    Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue aggiornamento massivo prezzi.
    /// </summary>
    /// <param name="priceListId">ID listino</param>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato aggiornamento</returns>
    Task<BulkUpdateResultDto> BulkUpdatePricesAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida la precedenza dei listini e identifica conflitti.
    /// </summary>
    /// <param name="eventId">ID evento</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato validazione con issues e warnings</returns>
    Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice.
    /// </summary>
    /// <param name="dto">Parametri applicazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato applicazione</returns>
    Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(
        ApplyPriceListToProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);
}
