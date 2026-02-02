using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class RolesModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<RolesModel> _logger;

    public int TotalRoles { get; set; }
    public int SystemRoles { get; set; }
    public int TotalPermissions { get; set; }

    public List<RoleListItem> Roles { get; set; } = new();

    public RolesModel(EventForgeDbContext context, ILogger<RolesModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        // Statistics
        TotalRoles = await _context.Roles.CountAsync();
        SystemRoles = await _context.Roles.CountAsync(r => r.IsSystemRole);
        TotalPermissions = await _context.Permissions.CountAsync();

        // Load roles
        Roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.DisplayName)
            .Select(r => new RoleListItem
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count
            })
            .ToListAsync();
    }

    public class RoleListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
    }
}
