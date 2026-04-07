namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Service responsible for seeding and managing default users.
/// </summary>
public interface IUserSeeder
{
    /// <summary>
    /// Creates the SuperAdmin user if it doesn't exist.
    /// </summary>
    Task<User?> CreateSuperAdminUserAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a default Manager user if it doesn't exist.
    /// </summary>
    Task<User?> CreateDefaultManagerUserAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
