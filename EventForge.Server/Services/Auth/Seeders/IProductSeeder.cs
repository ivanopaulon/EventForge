namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Interface for product seeding service.
/// </summary>
public interface IProductSeeder
{
    /// <summary>
    /// Seeds demo products for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if seeding was successful</returns>
    Task<bool> SeedDemoProductsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
