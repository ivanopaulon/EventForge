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
    /// Gets recent product transactions for price suggestions.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="type">Transaction type: "purchase" or "sale" (default: "purchase")</param>
    /// <param name="partyId">Optional business party ID to filter results</param>
    /// <param name="top">Number of recent transactions to return (default: 3, max: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent product transactions</returns>
    /// <response code="200">Returns the list of recent transactions</response>
    /// <response code="400">If the parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the product is not found</response>
    [HttpGet("products/{productId}/recent-transactions")]
    [ProducesResponseType(typeof(IEnumerable<RecentProductTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RecentProductTransactionDto>>> GetRecentProductTransactions(
        Guid productId,
        [FromQuery] string type = "purchase",
        [FromQuery] Guid? partyId = null,
        [FromQuery] int top = 3,
        CancellationToken cancellationToken = default)
    {
        // Validate type parameter
        if (!type.Equals("purchase", StringComparison.OrdinalIgnoreCase) &&
            !type.Equals("sale", StringComparison.OrdinalIgnoreCase))
        {
            return CreateValidationProblemDetails("Type parameter must be either 'purchase' or 'sale'.");
        }

        // Validate top parameter
        if (top < 1 || top > 10)
        {
            return CreateValidationProblemDetails("Top parameter must be between 1 and 10.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            // Check if product exists
            var productExists = await productService.ProductExistsAsync(productId, cancellationToken);
            if (!productExists)
            {
                return CreateNotFoundProblem($"Product with ID {productId} not found.");
            }

            var transactions = await productService.GetRecentProductTransactionsAsync(
                productId,
                type,
                partyId,
                top,
                cancellationToken);

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving recent transactions.", ex);
        }
    }

}
