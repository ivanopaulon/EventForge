namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Service responsible for seeding store-related entities (PaymentMethods, POS, Operators).
/// </summary>
public interface IStoreSeeder
{
    /// <summary>
    /// Seeds default payment methods for a tenant.
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if seeding was successful</returns>
    Task<bool> SeedPaymentMethodsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds default POS terminal for a tenant.
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if seeding was successful</returns>
    Task<bool> SeedDefaultPosAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds default store operator for a tenant.
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if seeding was successful</returns>
    Task<bool> SeedDefaultOperatorAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Seeds all store base entities (PaymentMethods + POS + Operator).
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all seeding operations were successful</returns>
    Task<bool> SeedStoreBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
