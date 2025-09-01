using EventForge.DTOs.RetailCart;

namespace EventForge.Server.Services.RetailCart
{
    /// <summary>
    /// Service interface for managing retail cart sessions.
    /// Provides tenant-aware, user-scoped cart storage with promotion integration.
    /// </summary>
    public interface IRetailCartSessionService
    {
        /// <summary>
        /// Creates a new cart session.
        /// </summary>
        /// <param name="createDto">Cart session creation data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created cart session</returns>
        Task<CartSessionDto> CreateSessionAsync(CreateCartSessionDto createDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cart session by ID.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cart session or null if not found</returns>
        Task<CartSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an item to the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="addItemDto">Item to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        Task<CartSessionDto?> AddItemAsync(Guid sessionId, AddCartItemDto addItemDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="itemId">Item ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        Task<CartSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates item quantity in the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="itemId">Item ID</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session</returns>
        Task<CartSessionDto?> UpdateItemQuantityAsync(Guid sessionId, Guid itemId, UpdateCartItemDto updateDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies coupons to the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="applyCouponsDto">Coupons to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated cart session with applied promotions</returns>
        Task<CartSessionDto?> ApplyCouponsAsync(Guid sessionId, ApplyCouponsDto applyCouponsDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all items from the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cleared cart session</returns>
        Task<CartSessionDto?> ClearAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets calculated totals for the cart session.
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cart session with current totals</returns>
        Task<CartSessionDto?> GetTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }
}