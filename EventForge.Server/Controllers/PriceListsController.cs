using Prym.DTOs.PriceLists;
using EventForge.Server.Services.PriceLists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for price list management and bulk operations.
/// Provides endpoints for price list CRUD operations and bulk price updates.
/// </summary>
[Route("api/v1/pricelists")]
[Authorize]
[ApiController]
public class PriceListsController(
    IPriceListGenerationService generationService,
    IPriceListBulkOperationsService bulkOperationsService,
    IPriceResolutionService priceResolutionService,
    ILogger<PriceListsController> logger) : BaseApiController
{

    /// <summary>
    /// Anteprima aggiornamento massivo prezzi.
    /// Restituisce un'anteprima dei cambiamenti senza salvare le modifiche.
    /// </summary>
    /// <param name="id">ID del listino prezzi</param>
    /// <param name="dto">Parametri dell'aggiornamento massivo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anteprima delle modifiche che verrebbero applicate</returns>
    /// <response code="200">Restituisce l'anteprima delle modifiche</response>
    /// <response code="400">Se i parametri della richiesta non sono validi</response>
    /// <response code="404">Se il listino non è stato trovato</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpPost("{id}/bulk-update-preview")]
    [ProducesResponseType(typeof(BulkUpdatePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkUpdatePreviewDto>> PreviewBulkUpdate(
        Guid id,
        [FromBody] BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var result = await bulkOperationsService.PreviewBulkUpdateAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Price list {PriceListId} not found for bulk update preview", id);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while previewing the bulk update.", ex);
        }
    }

    /// <summary>
    /// Esegue aggiornamento massivo prezzi.
    /// Applica le modifiche ai prezzi in modo persistente.
    /// </summary>
    /// <param name="id">ID del listino prezzi</param>
    /// <param name="dto">Parametri dell'aggiornamento massivo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato dell'operazione di aggiornamento massivo</returns>
    /// <response code="200">Restituisce il risultato dell'aggiornamento con conteggi e errori</response>
    /// <response code="400">Se i parametri della richiesta non sono validi</response>
    /// <response code="404">Se il listino non è stato trovato</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpPost("{id}/bulk-update")]
    [ProducesResponseType(typeof(BulkUpdateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkUpdateResultDto>> BulkUpdate(
        Guid id,
        [FromBody] BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var result = await bulkOperationsService.BulkUpdatePricesAsync(id, dto, currentUser, cancellationToken);

            if (result.FailedCount > 0)
            {
                logger.LogWarning(
                    "Bulk update for price list {PriceListId} completed with {FailedCount} failures. Updated: {UpdatedCount}",
                    id, result.FailedCount, result.UpdatedCount);
            }
            else
            {
                logger.LogInformation(
                    "Bulk update for price list {PriceListId} completed successfully. Updated: {UpdatedCount}",
                    id, result.UpdatedCount);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Price list {PriceListId} not found for bulk update", id);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while performing the bulk update.", ex);
        }
    }

    /// <summary>
    /// Genera un nuovo listino dai prezzi DefaultPrice dei prodotti.
    /// </summary>
    /// <param name="dto">Parametri per la generazione del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID del listino creato</returns>
    /// <response code="200">Restituisce l'ID del listino creato</response>
    /// <response code="400">Se i parametri della richiesta non sono validi</response>
    /// <response code="404">Se l'evento o le categorie specificate non sono state trovate</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpPost("generate-from-products")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> GenerateFromProductPrices(
        [FromBody] GeneratePriceListFromProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var priceListId = await generationService.GenerateFromProductPricesAsync(
                dto,
                currentUser,
                cancellationToken);

            logger.LogInformation(
                "Price list {PriceListId} generated from products by user {User}",
                priceListId, currentUser);

            return Ok(priceListId);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to generate price list from products: {Message}", ex.Message);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while generating the price list from products.", ex);
        }
    }

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice.
    /// </summary>
    /// <param name="id">ID del listino da applicare</param>
    /// <param name="dto">Parametri per l'applicazione del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato dell'applicazione con dettagli delle modifiche</returns>
    /// <response code="200">Restituisce il risultato dell'applicazione</response>
    /// <response code="400">Se i parametri della richiesta non sono validi o l'ID non corrisponde</response>
    /// <response code="404">Se il listino non è stato trovato</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpPost("{id:guid}/apply-to-products")]
    [ProducesResponseType(typeof(ApplyPriceListResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplyPriceListResultDto>> ApplyPriceListToProducts(
        Guid id,
        [FromBody] ApplyPriceListToProductsDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validazione: id deve corrispondere a dto.PriceListId
        if (id != dto.PriceListId)
        {
            return CreateValidationProblemDetails("PriceListId in the route does not match the body.");
        }

        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var result = await bulkOperationsService.ApplyPriceListToProductsAsync(
                dto,
                currentUser,
                cancellationToken);

            logger.LogInformation(
                "Price list {PriceListId} applied to products. Updated: {Updated}, Skipped: {Skipped}, Not Found: {NotFound}",
                id, result.ProductsUpdated, result.ProductsSkipped, result.ProductsNotFound);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to apply price list {PriceListId}: {Message}", id, ex.Message);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while applying the price list to products.", ex);
        }
    }

    /// <summary>
    /// Risolve il prezzo per un prodotto basandosi sulla priorità dei listini:
    /// 1. Listino forzato nel parametro (forcedPriceListId)
    /// 2. Listino forzato nel documento (DocumentHeader.PriceListId)
    /// 3. Listino predefinito del Business Party (Cliente/Fornitore)
    /// 4. Listino generale attivo per direzione (Vendita/Acquisto)
    /// 5. Fallback: Product.DefaultPrice
    /// </summary>
    /// <param name="productId">ID del prodotto</param>
    /// <param name="documentHeaderId">ID dell'intestazione documento (opzionale)</param>
    /// <param name="businessPartyId">ID del Business Party (opzionale)</param>
    /// <param name="forcedPriceListId">ID del listino forzato (opzionale)</param>
    /// <param name="direction">Direzione del listino (Input=acquisto, Output=vendita)</param>
    /// <param name="quantity">Quantità per il filtro fascia MinQuantity/MaxQuantity (default 1)</param>
    /// <param name="unitOfMeasureId">Unità di misura per filtrare le voci di listino per UoM (opzionale). Se specificata, preferisce le voci con quella UoM; fallback alle voci senza UoM</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato della risoluzione del prezzo con metadati</returns>
    /// <response code="200">Restituisce il prezzo risolto con metadati</response>
    /// <response code="400">Se i parametri della richiesta non sono validi</response>
    /// <response code="404">Se il prodotto non è stato trovato</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpGet("resolve-price")]
    [ProducesResponseType(typeof(PriceResolutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PriceResolutionResult>> ResolvePriceAsync(
        [FromQuery] Guid productId,
        [FromQuery] Guid? documentHeaderId = null,
        [FromQuery] Guid? businessPartyId = null,
        [FromQuery] Guid? forcedPriceListId = null,
        [FromQuery] PriceListDirection? direction = null,
        [FromQuery] decimal quantity = 1m,
        [FromQuery] Guid? unitOfMeasureId = null,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            return CreateValidationProblemDetails("Product ID is required.");
        }

        try
        {
            var result = await priceResolutionService.ResolvePriceAsync(
                productId,
                documentHeaderId,
                businessPartyId,
                forcedPriceListId,
                direction,
                quantity,
                unitOfMeasureId,
                cancellationToken);

            logger.LogInformation(
                "Price resolved for product {ProductId}: {Price} from {Source}",
                productId, result.Price, result.Source);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Product {ProductId} not found for price resolution", productId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while resolving the price.", ex);
        }
    }

    /// <summary>
    /// Risolve i prezzi per più prodotti in una singola chiamata batch.
    /// Ogni item può avere parametri di contesto diversi (documento, business party, listino forzato).
    /// </summary>
    /// <param name="request">Request con lista di item da risolvere (max 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultati con prezzo risolto per ogni item (key), errori separati</returns>
    /// <response code="200">Restituisce i prezzi risolti con metadati</response>
    /// <response code="400">Se i parametri della richiesta non sono validi</response>
    /// <response code="500">Se si verifica un errore interno del server</response>
    [HttpPost("resolve-prices")]
    [ProducesResponseType(typeof(BatchPriceResolutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BatchPriceResolutionResponse>> ResolvePricesBatchAsync(
        [FromBody] BatchPriceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var result = await priceResolutionService.ResolvePricesBatchAsync(request, cancellationToken);

            logger.LogInformation(
                "Batch price resolution: {Total} items processed, {Succeeded} succeeded, {Failed} failed",
                result.TotalProcessed, result.TotalSucceeded, result.TotalFailed);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred during batch price resolution.", ex);
        }
    }
}
