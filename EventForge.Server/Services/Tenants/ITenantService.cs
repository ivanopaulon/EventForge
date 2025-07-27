using EventForge.Server.DTOs.SuperAdmin;
using EventForge.Server.DTOs.Tenants;
using EventForge.Server.Data.Entities.Auth;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Interface for tenant management operations.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant with an auto-generated admin user.
    /// </summary>
    /// <param name="createDto">Tenant creation data</param>
    /// <returns>Created tenant with admin user details</returns>
    Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto);

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant details</returns>
    Task<TenantResponseDto?> GetTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets all tenants (super admin only).
    /// </summary>
    /// <returns>List of all tenants</returns>
    Task<IEnumerable<TenantResponseDto>> GetAllTenantsAsync();

    /// <summary>
    /// Updates tenant information.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="updateDto">Updated tenant data</param>
    /// <returns>Updated tenant details</returns>
    Task<TenantResponseDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto updateDto);

    /// <summary>
    /// Enables or disables a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="isEnabled">Whether the tenant should be enabled</param>
    /// <param name="reason">Reason for the status change</param>
    Task SetTenantStatusAsync(Guid tenantId, bool isEnabled, string reason);

    /// <summary>
    /// Adds an admin to a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="userId">User ID to make admin</param>
    /// <param name="accessLevel">Admin access level</param>
    /// <returns>Admin tenant mapping details</returns>
    Task<AdminTenantResponseDto> AddTenantAdminAsync(Guid tenantId, Guid userId, AdminAccessLevel accessLevel);

    /// <summary>
    /// Removes an admin from a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="userId">User ID to remove as admin</param>
    Task RemoveTenantAdminAsync(Guid tenantId, Guid userId);

    /// <summary>
    /// Gets all admins for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>List of tenant admins</returns>
    Task<IEnumerable<AdminTenantResponseDto>> GetTenantAdminsAsync(Guid tenantId);

    /// <summary>
    /// Forces a user to change their password on next login.
    /// </summary>
    /// <param name="userId">User ID</param>
    Task ForcePasswordChangeAsync(Guid userId);

    /// <summary>
    /// Gets audit trail for tenant operations.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter</param>
    /// <param name="operationType">Optional operation type filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>Paginated audit trail entries</returns>
    Task<PaginatedResponse<AuditTrailResponseDto>> GetAuditTrailAsync(
        Guid? tenantId = null,
        AuditOperationType? operationType = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Gets tenant statistics for the dashboard.
    /// </summary>
    /// <returns>Tenant statistics</returns>
    Task<TenantStatisticsDto> GetTenantStatisticsAsync();

    /// <summary>
    /// Searches tenants with advanced filtering.
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated tenant results</returns>
    Task<PaginatedResponse<TenantResponseDto>> SearchTenantsAsync(TenantSearchDto searchDto);

    /// <summary>
    /// Gets detailed information for a tenant including limits and usage.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Detailed tenant information</returns>
    Task<TenantDetailDto?> GetTenantDetailsAsync(Guid tenantId);

    /// <summary>
    /// Gets tenant limits and usage information.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant limits information</returns>
    Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid tenantId);

    /// <summary>
    /// Updates tenant limits.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="updateDto">Updated limits data</param>
    /// <returns>Updated limits information</returns>
    Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid tenantId, EventForge.Server.DTOs.SuperAdmin.UpdateTenantLimitsDto updateDto);
}

/// <summary>
/// Paginated response wrapper for collections.
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => PageNumber < TotalPages;
    public bool HasPrevious => PageNumber > 1;
}