namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Service responsible for seeding base entities for tenants.
/// </summary>
public interface IEntitySeeder
{
    /// <summary>
    /// Seeds base entities for a tenant (VAT natures, VAT rates, units of measure, warehouses, document types).
    /// </summary>
    Task<bool> SeedTenantBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
