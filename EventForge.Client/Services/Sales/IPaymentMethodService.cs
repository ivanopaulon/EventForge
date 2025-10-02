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
    Task<List<PaymentMethodDto>?> GetAllAsync();

    /// <summary>
    /// Gets only active payment methods.
    /// </summary>
    Task<List<PaymentMethodDto>?> GetActiveAsync();

    /// <summary>
    /// Gets a payment method by ID.
    /// </summary>
    Task<PaymentMethodDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new payment method.
    /// </summary>
    Task<PaymentMethodDto?> CreateAsync(CreatePaymentMethodDto createDto);

    /// <summary>
    /// Updates an existing payment method.
    /// </summary>
    Task<PaymentMethodDto?> UpdateAsync(Guid id, UpdatePaymentMethodDto updateDto);

    /// <summary>
    /// Deletes a payment method.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
