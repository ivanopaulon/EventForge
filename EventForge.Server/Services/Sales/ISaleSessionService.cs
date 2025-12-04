using EventForge.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service interface for managing sales sessions.
/// </summary>
public interface ISaleSessionService
{
    /// <summary>
    /// Creates a new sale session.
    /// </summary>
    /// <param name="createDto">Sale session creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created sale session DTO</returns>
    Task<SaleSessionDto> CreateSessionAsync(CreateSaleSessionDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sale session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="updateDto">Sale session update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sale session (soft delete).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addItemDto">Item data to add</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if session not found</returns>
    Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto addItemDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an item in a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="itemId">Item ID</param>
    /// <param name="updateItemDto">Item update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateItemDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="itemId">Item ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a payment to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addPaymentDto">Payment data to add</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if session not found</returns>
    Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto addPaymentDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a payment from a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a note to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addNoteDto">Note data to add</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if session not found</returns>
    Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto addNoteDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates totals for a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO with recalculated totals or null if not found</returns>
    Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a sale session and optionally generates a document.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sale sessions for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sale sessions</returns>
    Task<List<SaleSessionDto>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sale sessions for a specific operator.
    /// </summary>
    /// <param name="operatorId">Operator ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operator's sale sessions</returns>
    Task<List<SaleSessionDto>> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a closed sale session, creating inverse stock movements and marking document as voided.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Voided sale session DTO or null if not found</returns>
    Task<SaleSessionDto?> VoidSessionAsync(Guid sessionId, string currentUser, CancellationToken cancellationToken = default);
}
