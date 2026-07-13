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
    /// Calcola il prezzo applicato per un prodotto considerando listini e BusinessParty.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="eventId">ID evento</param>
    /// <param name="businessPartyId">ID BusinessParty (opzionale)</param>
    /// <param name="quantity">Quantità</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId}/applied-price")]
    [ProducesResponseType(typeof(AppliedPriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppliedPrice(
        Guid productId,
        [FromQuery] Guid eventId,
        [FromQuery] Guid? businessPartyId = null,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await priceCalculationService.GetAppliedPriceAsync(
            productId,
            eventId,
            businessPartyId,
            null,
            quantity,
            cancellationToken);

        if (result is null)
            return NotFound(new { error = "No applicable price found for this product" });

        return Ok(result);
    }

    /// <summary>
    /// Confronta i prezzi di acquisto per un prodotto da tutti i fornitori.
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="quantity">Quantità</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{productId}/purchase-price-comparison")]
    [ProducesResponseType(typeof(List<PurchasePriceComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchasePriceComparison(
        Guid productId,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await priceCalculationService.GetPurchasePriceComparisonAsync(
            productId,
            quantity,
            null,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// Supporta modalità: Automatico, Listino Forzato, Manuale, Ibrido.
    /// </summary>
    /// <param name="request">Parametri per il calcolo del prezzo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dettagli del prezzo applicato</returns>
    [HttpPost("products/price")]
    [ProducesResponseType(typeof(ProductPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductPrice(
        [FromBody] GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await priceListService.GetProductPriceAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}
