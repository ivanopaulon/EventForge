using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin user management operations.
/// Provides comprehensive user management capabilities across all tenants with proper multi-tenant support.
/// </summary>
[Route("api/v1/user-management")]
[Authorize(Roles = "SuperAdmin")]
public partial class UserManagementController(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IAuditLogService auditLogService,
    IPasswordService passwordService,
    IHubContext<AppHub> hubContext,
    ILogger<UserManagementController> logger) : BaseApiController
{

}