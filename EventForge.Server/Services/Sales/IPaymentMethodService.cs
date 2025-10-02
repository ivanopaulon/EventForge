using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service interface for managing payment methods.
/// </summary>
public interface IPaymentMethodService
{
    /// <summary>
    /// Gets all payment methods with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of payment methods</returns>
    Task<PagedResult<PaymentMethodDto>> GetPaymentMethodsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active payment methods (for POS UI).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active payment methods ordered by display order</returns>
    Task<List<PaymentMethodDto>> GetActivePaymentMethodsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment method DTO or null if not found</returns>
    Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payment method by code.
    /// </summary>
    /// <param name="code">Payment method code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment method DTO or null if not found</returns>
    Task<PaymentMethodDto?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    /// <param name="createDto">Payment method creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment method DTO</returns>
    Task<PaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="updateDto">Payment method update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment method DTO or null if not found</returns>
    Task<PaymentMethodDto?> UpdatePaymentMethodAsync(Guid id, UpdatePaymentMethodDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payment method (soft delete).
    /// </summary>
    /// <param name="id">Payment method ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePaymentMethodAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment method code already exists.
    /// </summary>
    /// <param name="code">Payment method code</param>
    /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code exists, false otherwise</returns>
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
