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
    /// Ottiene tutte le voci di un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("price-lists/{id:guid}/entries")]
    [ProducesResponseType(typeof(IEnumerable<PriceListEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPriceListEntries(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await priceListService.GetPriceListEntriesAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Aggiunge una voce a un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="dto">Dati della voce da aggiungere</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("price-lists/{id:guid}/entries")]
    [ProducesResponseType(typeof(PriceListEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPriceListEntry(
        Guid id,
        [FromBody] CreatePriceListEntryDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.PriceListId != id)
            return BadRequest(new { error = "The price list ID in the route does not match the price list ID in the request body." });

        try
        {
            var currentUser = GetCurrentUser();
            var result = await priceListService.AddPriceListEntryAsync(dto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPriceListEntries), new { id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aggiorna una voce di PriceList.
    /// </summary>
    /// <param name="entryId">ID della voce</param>
    /// <param name="dto">Dati aggiornati della voce</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("price-lists/entries/{entryId:guid}")]
    [ProducesResponseType(typeof(PriceListEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePriceListEntry(
        Guid entryId,
        [FromBody] UpdatePriceListEntryDto dto,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await priceListService.UpdatePriceListEntryAsync(entryId, dto, currentUser, cancellationToken);
        if (result == null)
            return NotFound(new { error = "Price list entry not found" });
        return Ok(result);
    }

    /// <summary>
    /// Elimina una voce di PriceList.
    /// </summary>
    /// <param name="entryId">ID della voce</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("price-lists/entries/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePriceListEntry(Guid entryId, CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await priceListService.RemovePriceListEntryAsync(entryId, currentUser, cancellationToken);
        if (!result)
            return NotFound(new { error = "Price list entry not found" });
        return NoContent();
    }

    /// <summary>
    /// Import bulk di voci in un PriceList.
    /// </summary>
    /// <param name="id">ID del listino</param>
    /// <param name="entries">Voci da importare</param>
    /// <param name="replaceExisting">Se sostituire le voci esistenti per gli stessi prodotti</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("price-lists/{id:guid}/entries/bulk")]
    [ProducesResponseType(typeof(BulkImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkImportPriceListEntries(
        Guid id,
        [FromBody] List<CreatePriceListEntryDto> entries,
        [FromQuery] bool replaceExisting = false,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await priceListService.BulkImportPriceListEntriesAsync(id, entries, currentUser, replaceExisting, cancellationToken);
        return Ok(result);
    }

}
