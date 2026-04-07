namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Service responsible for seeding and managing licenses.
/// </summary>
public interface ILicenseSeeder
{
    /// <summary>
    /// Ensures the SuperAdmin license exists and is up to date.
    /// </summary>
    Task<License?> EnsureSuperAdminLicenseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a license to a tenant.
    /// </summary>
    Task<bool> AssignLicenseToTenantAsync(Guid tenantId, Guid licenseId, CancellationToken cancellationToken = default);
}
