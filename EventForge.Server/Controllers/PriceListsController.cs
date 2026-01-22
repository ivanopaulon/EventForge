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
    private readonly IPriceListService _priceListService;
    private readonly ILogger<PriceListsController> _logger;

    public PriceListsController(
        IPriceListService priceListService,
        ILogger<PriceListsController> logger)
    {
        _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
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
            var result = await _priceListService.PreviewBulkUpdateAsync(id, dto, cancellationToken);
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
            var result = await _priceListService.BulkUpdatePricesAsync(id, dto, currentUser, cancellationToken);

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
}
