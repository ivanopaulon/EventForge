using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing payment methods.
/// </summary>
public interface IPaymentMethodService
{
    /// <summary>
    /// Gets all payment methods.
    /// </summary>
    Task<List<PaymentMethodDto>?> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets paged payment methods.
    /// </summary>
    Task<PagedResult<PaymentMethodDto>> GetPagedAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets only active payment methods.
    /// </summary>
    Task<List<PaymentMethodDto>?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    Task<PaymentMethodDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    Task<PaymentMethodDto?> CreateAsync(CreatePaymentMethodDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    Task<PaymentMethodDto?> UpdateAsync(Guid id, UpdatePaymentMethodDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a payment method.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
