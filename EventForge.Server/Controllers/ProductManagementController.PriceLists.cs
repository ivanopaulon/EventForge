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
    /// Gets all price lists with pagination and optional filtering.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="direction">Optional filter by direction (Input/Output)</param>
    /// <param name="status">Optional filter by status (Active/Suspended/Deleted)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of price lists</returns>
    /// <response code="200">Returns the paginated list of price lists</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("price-lists")]
    [ProducesResponseType(typeof(PagedResult<PriceListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PriceListDto>>> GetPriceLists(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] PriceListDirection? direction = null,
        [FromQuery] Prym.DTOs.Common.PriceListStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var result = await priceListService.GetPriceListsAsync(pagination, direction, status, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving price lists.", ex);
        }
    }

    /// <summary>
    /// Gets a price list by ID.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price list information</returns>
    /// <response code="200">Returns the price list</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("price-lists/{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> GetPriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var priceList = await priceListService.GetPriceListByIdAsync(id, cancellationToken);
            if (priceList is null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the price list.", ex);
        }
    }

    /// <summary>
    /// Creates a new price list.
    /// </summary>
    /// <param name="createPriceListDto">Price list creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created price list information</returns>
    /// <response code="201">Price list created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("price-lists")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> CreatePriceList(
        [FromBody] CreatePriceListDto createPriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await priceListService.CreatePriceListAsync(createPriceListDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetPriceList), new { id = priceList.Id }, priceList);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the price list.", ex);
        }
    }

    /// <summary>
    /// Updates an existing price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="updatePriceListDto">Price list update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated price list information</returns>
    /// <response code="200">Price list updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("price-lists/{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PriceListDto>> UpdatePriceList(
        Guid id,
        [FromBody] UpdatePriceListDto updatePriceListDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var priceList = await priceListService.UpdatePriceListAsync(id, updatePriceListDto, currentUser, cancellationToken);
            if (priceList is null)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return Ok(priceList);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the price list.", ex);
        }
    }

    /// <summary>
    /// Deletes a price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Price list deleted successfully</response>
    /// <response code="404">If the price list is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("price-lists/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePriceList(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await priceListService.DeletePriceListAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Price list with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the price list.", ex);
        }
    }

    /// <summary>
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    /// <param name="id">ID del listino da duplicare</param>
    /// <param name="dto">Opzioni di duplicazione</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dettagli del listino duplicato</returns>
    /// <response code="201">Listino duplicato con successo</response>
    /// <response code="400">Se i parametri di duplicazione non sono validi</response>
    /// <response code="404">Se il listino sorgente non esiste</response>
    /// <response code="403">Se l'utente non ha accesso al tenant corrente</response>
    [HttpPost("price-lists/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(DuplicatePriceListResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DuplicatePriceList(
        Guid id,
        [FromBody] DuplicatePriceListDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var result = await priceListGenerationService.DuplicatePriceListAsync(
                id, dto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetPriceList),
                new { id = result.NewPriceList.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation during price list duplication");
            return CreateNotFoundProblem(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error during price list duplication");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while duplicating the price list.", ex);
        }
    }

}
