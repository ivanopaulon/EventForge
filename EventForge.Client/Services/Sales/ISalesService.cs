using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing sale sessions.
/// </summary>
public interface ISalesService
{
    /// <summary>
    /// Creates a new sale session.
    /// </summary>
    Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto);

    /// <summary>
    /// Gets a sale session by ID.
    /// </summary>
    Task<SaleSessionDto?> GetSessionAsync(Guid sessionId);

    /// <summary>
    /// Updates an existing sale session.
    /// </summary>
    Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto);

    /// <summary>
    /// Deletes a sale session.
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId);

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    Task<List<SaleSessionDto>?> GetActiveSessionsAsync();

    /// <summary>
    /// Gets sessions for a specific operator.
    /// </summary>
    Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId);

    /// <summary>
    /// Adds an item to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto);

    /// <summary>
    /// Updates an item in a sale session.
    /// </summary>
    Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto);

    /// <summary>
    /// Removes an item from a sale session.
    /// </summary>
    Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId);

    /// <summary>
    /// Adds a payment to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto);

    /// <summary>
    /// Removes a payment from a sale session.
    /// </summary>
    Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId);

    /// <summary>
    /// Adds a note to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto);

    /// <summary>
    /// Recalculates totals for a sale session.
    /// </summary>
    Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId);

    /// <summary>
    /// Closes a sale session.
    /// </summary>
    Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId);
}
