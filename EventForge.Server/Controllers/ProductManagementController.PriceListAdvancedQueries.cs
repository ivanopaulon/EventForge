using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class ProductManagementController
{

    /// <summary>
    /// Ottiene i listini filtrati per tipo (Sales/Purchase).
    /// </summary>
    /// <param name="type">Tipo listino (Sales o Purchase)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("price-lists/by-type/{type}")]
    [ProducesResponseType(typeof(IEnumerable<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListsByType(
        PriceListType type,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListService.GetPriceListsByTypeAsync(type, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Ottiene tutti i listini assegnati a un BusinessParty.
    /// </summary>
    /// <param name="id">ID del BusinessParty</param>
    /// <param name="type">Tipo listino (opzionale)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("business-parties/{id}/price-lists")]
    [ProducesResponseType(typeof(IEnumerable<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListsByBusinessParty(
        Guid id,
        [FromQuery] PriceListType? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListBusinessPartyService.GetPriceListsByBusinessPartyAsync(id, type, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Preview generazione listino da documenti di acquisto
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Preview con statistiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If supplier not found</response>
    [HttpPost("price-lists/generate-from-purchases/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewGenerateFromPurchases(
        [FromBody] GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await priceListGenerationService.PreviewGenerateFromPurchasesAsync(dto, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    /// <param name="dto">Parametri generazione</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>ID del listino creato</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If supplier not found</response>
    [HttpPost("price-lists/generate-from-purchases")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> GenerateFromPurchases(
        [FromBody] GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";
            var priceListId = await priceListGenerationService.GenerateFromPurchasesAsync(dto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = priceListId },
                priceListId);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Preview aggiornamento listino esistente da documenti
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Preview con statistiche modifiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If price list not found</response>
    [HttpPost("price-lists/update-from-purchases/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewUpdateFromPurchases(
        [FromBody] UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var preview = await priceListGenerationService.PreviewUpdateFromPurchasesAsync(dto, cancellationToken);
            return Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    /// <param name="dto">Parametri aggiornamento</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Risultato aggiornamento con statistiche</returns>
    /// <response code="200">Price list updated successfully</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If price list not found</response>
    [HttpPost("price-lists/update-from-purchases")]
    [ProducesResponseType(typeof(UpdatePriceListResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdatePriceListResultDto>> UpdateFromPurchases(
        [FromBody] UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";
            var result = await priceListGenerationService.UpdateFromPurchasesAsync(dto, currentUser, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Preview generation of price list from product default prices
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview con statistiche</returns>
    /// <response code="200">Returns preview statistics</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("price-lists/generate-from-defaults/preview")]
    [ProducesResponseType(typeof(GeneratePriceListPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GeneratePriceListPreviewDto>> PreviewGenerateFromDefaults(
        [FromBody] GenerateFromDefaultPricesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Map from GenerateFromDefaultPricesDto to GeneratePriceListFromProductsDto
            var productsDto = new GeneratePriceListFromProductsDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Code = dto.Code,
                Type = PriceListType.Sales,
                Direction = PriceListDirection.Output,
                Priority = dto.Priority,
                IsDefault = dto.IsDefault,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                EventId = null,
                MarkupPercentage = dto.MarkupPercentage,
                RoundingStrategy = dto.RoundingStrategy ?? Prym.DTOs.Common.RoundingStrategy.None,
                OnlyActiveProducts = dto.OnlyActiveProducts,
                OnlyProductsWithPrice = true,
                MinimumPrice = dto.MinimumPrice,
                FilterByCategoryIds = null,
                BusinessPartyIds = null
            };

            var result = await priceListService.PreviewGenerateFromProductPricesAsync(productsDto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

    /// <summary>
    /// Generate price list from product default prices
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID del listino creato</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost("price-lists/generate-from-defaults")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> GenerateFromDefaults(
        [FromBody] GenerateFromDefaultPricesDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "system";

            // Map from GenerateFromDefaultPricesDto to GeneratePriceListFromProductsDto
            var productsDto = new GeneratePriceListFromProductsDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Code = dto.Code,
                Type = PriceListType.Sales,
                Direction = PriceListDirection.Output,
                Priority = dto.Priority,
                IsDefault = dto.IsDefault,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                EventId = null,
                MarkupPercentage = dto.MarkupPercentage,
                RoundingStrategy = dto.RoundingStrategy ?? Prym.DTOs.Common.RoundingStrategy.None,
                OnlyActiveProducts = dto.OnlyActiveProducts,
                OnlyProductsWithPrice = true,
                MinimumPrice = dto.MinimumPrice,
                FilterByCategoryIds = null,
                BusinessPartyIds = null
            };

            var priceListId = await priceListService.GenerateFromProductPricesAsync(productsDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = priceListId },
                priceListId);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
    }

}
