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
    Task<SaleSessionDto?> CreateSessionAsync(CreateSaleSessionDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sale session by ID.
    /// </summary>
    Task<SaleSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sale session.
    /// </summary>
    Task<SaleSessionDto?> UpdateSessionAsync(Guid sessionId, UpdateSaleSessionDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sale session.
    /// </summary>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    Task<List<SaleSessionDto>?> GetActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sessions for a specific operator.
    /// </summary>
    Task<List<SaleSessionDto>?> GetOperatorSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddItemAsync(Guid sessionId, AddSaleItemDto itemDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an item in a sale session.
    /// </summary>
    Task<SaleSessionDto?> UpdateItemAsync(Guid sessionId, Guid itemId, UpdateSaleItemDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from a sale session.
    /// </summary>
    Task<SaleSessionDto?> RemoveItemAsync(Guid sessionId, Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a payment to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddPaymentAsync(Guid sessionId, AddSalePaymentDto paymentDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a payment from a sale session.
    /// </summary>
    Task<SaleSessionDto?> RemovePaymentAsync(Guid sessionId, Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a note to a sale session.
    /// </summary>
    Task<SaleSessionDto?> AddNoteAsync(Guid sessionId, AddSessionNoteDto noteDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a global discount percentage to all session items.
    /// </summary>
    Task<SaleSessionDto?> ApplyGlobalDiscountAsync(Guid sessionId, ApplyGlobalDiscountDto discountDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates totals for a sale session.
    /// </summary>
    Task<SaleSessionDto?> CalculateTotalsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes a sale session.
    /// </summary>
    Task<SaleSessionDto?> CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits a sale session.
    /// </summary>
    Task<SplitResultDto?> SplitSessionAsync(SplitSessionDto splitDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges multiple sale sessions.
    /// </summary>
    Task<SaleSessionDto?> MergeSessionsAsync(MergeSessionsDto mergeDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child sessions of a parent.
    /// </summary>
    Task<List<SaleSessionDto>> GetChildSessionsAsync(Guid parentSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a session can be split.
    /// </summary>
    Task<bool> CanSplitSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if sessions can be merged.
    /// </summary>
    Task<bool> CanMergeSessionsAsync(List<Guid> sessionIds, CancellationToken cancellationToken = default);
}
