using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class TenantsModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<TenantsModel> _logger;

    // Statistics
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TotalUsers { get; set; }
    public int TenantsWithLicense { get; set; }

    // List
    public List<TenantListItem> Tenants { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public TenantsModel(EventForgeDbContext context, ILogger<TenantsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        // Statistics
        TotalTenants = await _context.Tenants.CountAsync();
        ActiveTenants = await _context.Tenants.CountAsync(t => t.IsActive);
        TotalUsers = await _context.Users.CountAsync();
        TenantsWithLicense = await _context.TenantLicenses
            .Where(tl => tl.IsActive)
            .Select(tl => tl.TenantId)
            .Distinct()
            .CountAsync();

        // Query tenants
        var query = _context.Tenants
            .Include(t => t.Users)
            .Include(t => t.TenantLicenses)
                .ThenInclude(tl => tl.License)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.ToLower();
            query = query.Where(t => 
                t.Name.ToLower().Contains(search) || 
                t.Code.ToLower().Contains(search) ||
                t.DisplayName.ToLower().Contains(search) ||
                (t.Domain != null && t.Domain.ToLower().Contains(search)));
        }

        // Status filter
        if (StatusFilter == "active")
            query = query.Where(t => t.IsActive);
        else if (StatusFilter == "inactive")
            query = query.Where(t => !t.IsActive);

        // Load and map
        Tenants = await query
            .OrderBy(t => t.Name)
            .Select(t => new TenantListItem
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                DisplayName = t.DisplayName,
                Domain = t.Domain,
                IsActive = t.IsActive,
                MaxUsers = t.MaxUsers,
                UserCount = t.Users.Count,
                SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                License = t.TenantLicenses
                    .Where(tl => tl.IsActive)
                    .Select(tl => new LicenseInfo 
                    { 
                        Name = tl.License.Name 
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDisableAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound();

        tenant.IsActive = false;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = User.Identity?.Name ?? "system";

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Tenant {TenantId} disabled by {User}", id, User.Identity?.Name);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEnableAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound();

        tenant.IsActive = true;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = User.Identity?.Name ?? "system";

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Tenant {TenantId} enabled by {User}", id, User.Identity?.Name);

        return RedirectToPage();
    }

    // DTOs
    public class TenantListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public bool IsActive { get; set; }
        public int MaxUsers { get; set; }
        public int UserCount { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public LicenseInfo? License { get; set; }
    }

    public class LicenseInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
