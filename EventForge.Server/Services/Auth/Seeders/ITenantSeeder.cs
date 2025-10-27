namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Service responsible for seeding and managing tenants.
/// </summary>
public interface ITenantSeeder
{
    /// <summary>
    /// Creates the default tenant.
    /// </summary>
    Task<Tenant?> CreateDefaultTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an AdminTenant record granting access to manage a tenant.
    /// </summary>
    Task<bool> CreateAdminTenantRecordAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
