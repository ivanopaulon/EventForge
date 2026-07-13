using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant management operations.
/// </summary>
public partial class TenantService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IPasswordService passwordService,
    ILogger<TenantService> logger) : ITenantService
{
    /// <summary>
    /// Maximum allowed duration, in days, for a new AdminTenant grant's expiration
    /// (policy cap enforced at grant time; see TenantsController.AddTenantAdmin).
    /// </summary>
    public const int MaxAdminGrantDurationDays = 90;

}
