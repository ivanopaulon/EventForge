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
    /// Assegna un BusinessParty a un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="dto">Dati assegnazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Relazione creata</returns>
    [HttpPost("price-lists/{id}/business-parties")]
    [ProducesResponseType(typeof(PriceListBusinessPartyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignBusinessPartyToPriceList(
        Guid id,
        [FromBody] AssignBusinessPartyToPriceListDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var result = await priceListBusinessPartyService.AssignBusinessPartyAsync(id, dto, currentUser, cancellationToken);
            return CreatedAtAction(
                nameof(GetBusinessPartiesForPriceList),
                new { id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rimuove un BusinessParty da un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="businessPartyId">ID del BusinessParty</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("price-lists/{id}/business-parties/{businessPartyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveBusinessPartyFromPriceList(
        Guid id,
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await priceListBusinessPartyService.RemoveBusinessPartyAsync(id, businessPartyId, currentUser, cancellationToken);

        if (!result)
            return NotFound(new { error = "Business party assignment not found" });

        return NoContent();
    }

    /// <summary>
    /// Ottiene tutti i BusinessParty assegnati a un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("price-lists/{id}/business-parties")]
    [ProducesResponseType(typeof(IEnumerable<PriceListBusinessPartyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBusinessPartiesForPriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await priceListBusinessPartyService.GetBusinessPartiesForPriceListAsync(id, cancellationToken);
        return Ok(result);
    }

}
