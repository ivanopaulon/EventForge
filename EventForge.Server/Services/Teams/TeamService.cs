using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Teams;

namespace EventForge.Server.Services.Teams;

/// <summary>
/// Service implementation for managing teams and team members.
/// </summary>
public partial class TeamService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<TeamService> logger) : ITeamService
{

    // Team CRUD operations

}
