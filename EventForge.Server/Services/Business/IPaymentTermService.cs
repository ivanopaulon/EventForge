using EventForge.DTOs.Business;
using EventForge.DTOs.Common;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service interface for managing payment terms.
/// </summary>
public interface IPaymentTermService
{
    /// <summary>
    /// Gets all payment terms with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of payment terms</returns>
    Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment term by ID.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment term DTO or null if not found</returns>
    Task<PaymentTermDto?> GetPaymentTermByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new payment term.
    /// </summary>
    /// <param name="createPaymentTermDto">Payment term creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment term DTO</returns>
    Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createPaymentTermDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment term.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="updatePaymentTermDto">Payment term update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment term DTO or null if not found</returns>
    Task<PaymentTermDto?> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updatePaymentTermDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payment term (soft delete).
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePaymentTermAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment term exists.
    /// </summary>
    /// <param name="paymentTermId">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> PaymentTermExistsAsync(Guid paymentTermId, CancellationToken cancellationToken = default);
}