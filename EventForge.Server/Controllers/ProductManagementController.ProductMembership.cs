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
    /// Ottiene tutti i listini prezzi in cui compare il prodotto.
    /// </summary>
    /// <param name="id">ID del prodotto</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{id:guid}/price-lists")]
    [ProducesResponseType(typeof(IEnumerable<ProductPriceListMembershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListsForProduct(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await priceListService.GetPriceListsForProductAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Ottiene tutte le promozioni in cui compare il prodotto (targeting esplicito o regole "tutti i prodotti").
    /// </summary>
    /// <param name="id">ID del prodotto</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("products/{id:guid}/promotions")]
    [ProducesResponseType(typeof(IEnumerable<ProductPromotionMembershipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPromotionsForProduct(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await promotionService.GetPromotionsForProductAsync(id, cancellationToken);
        return Ok(result);
    }

}
