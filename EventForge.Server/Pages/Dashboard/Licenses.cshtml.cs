using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class LicensesModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<LicensesModel> _logger;

    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int LicensesInUse { get; set; }

    public List<LicenseListItem> Licenses { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public LicensesModel(EventForgeDbContext context, ILogger<LicensesModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        // Statistics
        TotalLicenses = await _context.Licenses.CountAsync();
        ActiveLicenses = await _context.Licenses.CountAsync(l => l.IsActive);
        LicensesInUse = await _context.TenantLicenses
            .Where(tl => tl.IsActive)
            .Select(tl => tl.LicenseId)
            .Distinct()
            .CountAsync();

        // Query licenses
        var query = _context.Licenses
            .Include(l => l.LicenseFeatures)
            .Include(l => l.TenantLicenses)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.ToLower();
            query = query.Where(l => 
                l.Name.ToLower().Contains(search) || 
                l.DisplayName.ToLower().Contains(search) ||
                (l.Description != null && l.Description.ToLower().Contains(search)));
        }

        // Load
        Licenses = await query
            .OrderBy(l => l.TierLevel)
            .ThenBy(l => l.DisplayName)
            .Select(l => new LicenseListItem
            {
                Id = l.Id,
                Name = l.Name,
                DisplayName = l.DisplayName,
                Description = l.Description,
                MaxUsers = l.MaxUsers,
                MaxApiCallsPerMonth = l.MaxApiCallsPerMonth,
                TierLevel = l.TierLevel,
                FeatureCount = l.LicenseFeatures.Count,
                AssignedTenantCount = l.TenantLicenses.Count(tl => tl.IsActive)
            })
            .ToListAsync();
    }

    public class LicenseListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxUsers { get; set; }
        public int MaxApiCallsPerMonth { get; set; }
        public int TierLevel { get; set; }
        public int FeatureCount { get; set; }
        public int AssignedTenantCount { get; set; }
    }
}
