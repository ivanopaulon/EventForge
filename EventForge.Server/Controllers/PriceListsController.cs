using EventForge.DTOs.PriceLists;
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
public class PriceListsController : BaseApiController
{
    private readonly IPriceListGenerationService _generationService;
    private readonly IPriceListBulkOperationsService _bulkOperationsService;
    private readonly ILogger<PriceListsController> _logger;

    public PriceListsController(
        IPriceListGenerationService generationService,
        IPriceListBulkOperationsService bulkOperationsService,
        ILogger<PriceListsController> logger)
    {
        _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        _bulkOperationsService = bulkOperationsService ?? throw new ArgumentNullException(nameof(bulkOperationsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            var result = await _bulkOperationsService.PreviewBulkUpdateAsync(id, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Price list {PriceListId} not found for bulk update preview", id);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing bulk update for price list {PriceListId}", id);
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
            var result = await _bulkOperationsService.BulkUpdatePricesAsync(id, dto, currentUser, cancellationToken);

            if (result.FailedCount > 0)
            {
                _logger.LogWarning(
                    "Bulk update for price list {PriceListId} completed with {FailedCount} failures. Updated: {UpdatedCount}",
                    id, result.FailedCount, result.UpdatedCount);
            }
            else
            {
                _logger.LogInformation(
                    "Bulk update for price list {PriceListId} completed successfully. Updated: {UpdatedCount}",
                    id, result.UpdatedCount);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Price list {PriceListId} not found for bulk update", id);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk update for price list {PriceListId}", id);
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
            var priceListId = await _generationService.GenerateFromProductPricesAsync(
                dto,
                currentUser,
                cancellationToken);

            _logger.LogInformation(
                "Price list {PriceListId} generated from products by user {User}",
                priceListId, currentUser);

            return Ok(priceListId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to generate price list from products: {Message}", ex.Message);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating price list from products");
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
            return BadRequest("PriceListId mismatch");
        }

        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var result = await _bulkOperationsService.ApplyPriceListToProductsAsync(
                dto,
                currentUser,
                cancellationToken);

            _logger.LogInformation(
                "Price list {PriceListId} applied to products. Updated: {Updated}, Skipped: {Skipped}, Not Found: {NotFound}",
                id, result.ProductsUpdated, result.ProductsSkipped, result.ProductsNotFound);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to apply price list {PriceListId}: {Message}", id, ex.Message);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying price list {PriceListId} to products", id);
            return CreateInternalServerErrorProblem("An error occurred while applying the price list to products.", ex);
        }
    }
}
