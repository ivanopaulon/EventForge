using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class RolePermissionsModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<RolePermissionsModel> _logger;

    [BindProperty(SupportsGet = true)]
    public Guid RoleId { get; set; }

    [BindProperty]
    public List<Guid> SelectedPermissions { get; set; } = new();

    public RoleInfo Role { get; set; } = new();
    public List<PermissionInfo> AllPermissions { get; set; } = new();
    public List<UserInfo> UsersWithRole { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public RolePermissionsModel(
        EventForgeDbContext context,
        ILogger<RolePermissionsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => r.Id == RoleId);

        if (role == null)
            return NotFound();

        Role = new RoleInfo
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt
        };

        SelectedPermissions = role.RolePermissions.Select(rp => rp.PermissionId).ToList();

        AllPermissions = await _context.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionInfo
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Category = p.Category,
                Resource = p.Resource,
                Action = p.Action
            })
            .ToListAsync();

        UsersWithRole = role.UserRoles
            .Select(ur => new UserInfo
            {
                Username = ur.User.Username,
                FullName = $"{ur.User.FirstName} {ur.User.LastName}",
                IsActive = ur.User.IsActive
            })
            .OrderBy(u => u.Username)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == RoleId);

        if (role == null)
            return NotFound();

        try
        {
            // Remove all existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);
            await _context.SaveChangesAsync();

            // Add selected permissions
            foreach (var permissionId in SelectedPermissions)
            {
                var rolePermission = new RolePermission
                {
                    RoleId = RoleId,
                    PermissionId = permissionId,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow,
                    TenantId = Guid.Empty,
                    IsActive = true
                };

                _context.RolePermissions.Add(rolePermission);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Permissions updated for role {RoleId} by {User}. Selected: {Count}",
                RoleId, User.Identity?.Name, SelectedPermissions.Count);

            TempData["SuccessMessage"] = $"Permessi aggiornati con successo! {SelectedPermissions.Count} permessi assegnati.";

            return RedirectToPage("/Dashboard/Roles");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role permissions");
            ErrorMessage = $"Errore durante l'aggiornamento: {ex.Message}";
            
            // Reload data
            await OnGetAsync();
            return Page();
        }
    }

    public class RoleInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PermissionInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Resource { get; set; }
        public string Action { get; set; } = string.Empty;
    }

    public class UserInfo
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
