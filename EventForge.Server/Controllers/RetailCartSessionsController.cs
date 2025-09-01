using EventForge.DTOs.RetailCart;
using EventForge.Server.Services.RetailCart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Server.Controllers
{
    /// <summary>
    /// REST API controller for retail cart session management.
    /// Provides CRUD operations for cart sessions with multi-tenant support.
    /// </summary>
    [Route("api/v1/[controller]")]
    [Authorize]
    public class RetailCartSessionsController : BaseApiController
    {
        private readonly IRetailCartSessionService _cartSessionService;
        private readonly ITenantContext _tenantContext;

        public RetailCartSessionsController(
            IRetailCartSessionService cartSessionService,
            ITenantContext tenantContext)
        {
            _cartSessionService = cartSessionService ?? throw new ArgumentNullException(nameof(cartSessionService));
            _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        }

        /// <summary>
        /// Creates a new cart session.
        /// </summary>
        /// <param name="createDto">Cart session creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created cart session</returns>
        /// <response code="200">Returns the created cart session</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpPost]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> CreateSession(
            [FromBody] CreateCartSessionDto createDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.CreateSessionAsync(createDto, cancellationToken);
                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return CreateValidationProblemDetails(ex.Message);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while creating the cart session.", ex);
            }
        }

        /// <summary>
        /// Gets a cart session by ID.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cart session information</returns>
        /// <response code="200">Returns the cart session</response>
        /// <response code="404">If the session is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> GetSession(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.GetSessionAsync(id, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while retrieving the cart session.", ex);
            }
        }

        /// <summary>
        /// Adds an item to the cart session.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="addItemDto">Item to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        /// <response code="200">Returns the updated cart session</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the session is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpPost("{id:guid}/items")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> AddItem(
            Guid id,
            [FromBody] AddCartItemDto addItemDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.AddItemAsync(id, addItemDto, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return CreateValidationProblemDetails(ex.Message);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while adding the item to the cart session.", ex);
            }
        }

        /// <summary>
        /// Updates item quantity in the cart session.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        /// <response code="200">Returns the updated cart session</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the session or item is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpPatch("{id:guid}/items/{itemId:guid}")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> UpdateItemQuantity(
            Guid id,
            Guid itemId,
            [FromBody] UpdateCartItemDto updateDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.UpdateItemQuantityAsync(id, itemId, updateDto, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return CreateValidationProblemDetails(ex.Message);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while updating the item in the cart session.", ex);
            }
        }

        /// <summary>
        /// Removes an item from the cart session.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="itemId">Item ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        /// <response code="200">Returns the updated cart session</response>
        /// <response code="404">If the session is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpDelete("{id:guid}/items/{itemId:guid}")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> RemoveItem(
            Guid id,
            Guid itemId,
            CancellationToken cancellationToken = default)
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.RemoveItemAsync(id, itemId, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while removing the item from the cart session.", ex);
            }
        }

        /// <summary>
        /// Applies coupons to the cart session.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="applyCouponsDto">Coupons to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session with applied promotions</returns>
        /// <response code="200">Returns the updated cart session</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the session is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpPost("{id:guid}/coupons")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> ApplyCoupons(
            Guid id,
            [FromBody] ApplyCouponsDto applyCouponsDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return CreateValidationProblemDetails();

            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.ApplyCouponsAsync(id, applyCouponsDto, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (ArgumentException ex)
            {
                return CreateValidationProblemDetails(ex.Message);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while applying coupons to the cart session.", ex);
            }
        }

        /// <summary>
        /// Clears all items from the cart session.
        /// </summary>
        /// <param name="id">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cleared cart session</returns>
        /// <response code="200">Returns the cleared cart session</response>
        /// <response code="404">If the session is not found</response>
        /// <response code="403">If the user doesn't have access to the current tenant</response>
        [HttpPost("{id:guid}/clear")]
        [ProducesResponseType(typeof(CartSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CartSessionDto>> ClearSession(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            // Validate tenant access
            var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
            if (tenantValidation != null)
                return tenantValidation;

            try
            {
                var session = await _cartSessionService.ClearAsync(id, cancellationToken);
                if (session == null)
                    return CreateNotFoundProblem($"Cart session with ID {id} not found.");

                return Ok(session);
            }
            catch (Exception ex)
            {
                return CreateInternalServerErrorProblem("An error occurred while clearing the cart session.", ex);
            }
        }
    }
}