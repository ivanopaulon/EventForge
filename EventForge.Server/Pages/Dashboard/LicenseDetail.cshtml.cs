using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class LicenseDetailModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<LicenseDetailModel> _logger;

    [BindProperty]
    public LicenseFormModel License { get; set; } = new();

    [BindProperty]
    public List<Guid> SelectedFeatures { get; set; } = new();

    public List<FeatureOption> AvailableFeatures { get; set; } = new();
    public List<TenantInfo> AssignedTenants { get; set; } = new();
    public bool IsEditMode => !string.IsNullOrEmpty(Request.Query["id"]);
    public int AssignedTenantCount { get; set; }
    public int FeatureCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public LicenseDetailModel(
        EventForgeDbContext context,
        ILogger<LicenseDetailModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        await LoadLookupsAsync();

        if (id.HasValue)
        {
            var license = await _context.Licenses
                .Include(l => l.LicenseFeatures)
                .Include(l => l.TenantLicenses)
                    .ThenInclude(tl => tl.Tenant)
                .FirstOrDefaultAsync(l => l.Id == id.Value);

            if (license == null)
                return NotFound();

            License = new LicenseFormModel
            {
                Id = license.Id,
                Name = license.Name,
                DisplayName = license.DisplayName,
                Description = license.Description,
                MaxUsers = license.MaxUsers,
                MaxApiCallsPerMonth = license.MaxApiCallsPerMonth,
                TierLevel = license.TierLevel,
                IsActive = license.IsActive,
                CreatedAt = license.CreatedAt,
                ModifiedAt = license.ModifiedAt
            };

            // Get feature template IDs that match the existing license features by name
            SelectedFeatures = await _context.FeatureTemplates
                .Where(ft => license.LicenseFeatures.Select(lf => lf.Name).Contains(ft.Name))
                .Select(ft => ft.Id)
                .ToListAsync();

            AssignedTenantCount = license.TenantLicenses.Count(tl => tl.IsActive);
            FeatureCount = license.LicenseFeatures.Count;

            AssignedTenants = license.TenantLicenses
                .Where(tl => tl.IsActive)
                .Select(tl => new TenantInfo
                {
                    DisplayName = tl.Tenant.DisplayName,
                    Code = tl.Tenant.Code,
                    ExpiresAt = tl.ExpiresAt
                })
                .OrderBy(t => t.DisplayName)
                .ToList();
        }
        else
        {
            License = new LicenseFormModel
            {
                IsActive = true,
                MaxUsers = 10,
                MaxApiCallsPerMonth = 10000,
                TierLevel = 1
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Correggi gli errori nel form";
            return Page();
        }

        try
        {
            if (License.Id == Guid.Empty)
            {
                return await CreateLicenseAsync();
            }
            else
            {
                return await UpdateLicenseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving license");
            ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            return Page();
        }
    }

    private async Task<IActionResult> CreateLicenseAsync()
    {
        if (await _context.Licenses.AnyAsync(l => l.Name.ToLower() == License.Name.ToLower()))
        {
            ErrorMessage = $"Esiste già una licenza con nome '{License.Name}'";
            return Page();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var license = new License
            {
                Id = Guid.NewGuid(),
                Name = License.Name,
                DisplayName = License.DisplayName,
                Description = License.Description,
                MaxUsers = License.MaxUsers,
                MaxApiCallsPerMonth = License.MaxApiCallsPerMonth,
                TierLevel = License.TierLevel,
                IsActive = License.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                TenantId = Guid.Empty // Licenses are system-level
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            // Add selected features from templates
            var selectedTemplates = await _context.FeatureTemplates
                .Where(ft => SelectedFeatures.Contains(ft.Id))
                .ToListAsync();

            foreach (var template in selectedTemplates)
            {
                var licenseFeature = new LicenseFeature
                {
                    Id = Guid.NewGuid(),
                    LicenseId = license.Id,
                    Name = template.Name,
                    DisplayName = template.DisplayName,
                    Description = template.Description,
                    Category = template.Category,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "system",
                    TenantId = Guid.Empty,
                    IsActive = true
                };

                _context.LicenseFeatures.Add(licenseFeature);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "License {LicenseName} created by {User}",
                license.Name, User.Identity?.Name);

            TempData["SuccessMessage"] = "Licenza creata con successo!";

            return RedirectToPage("/Dashboard/Licenses");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<IActionResult> UpdateLicenseAsync()
    {
        var license = await _context.Licenses
            .Include(l => l.LicenseFeatures)
            .FirstOrDefaultAsync(l => l.Id == License.Id);

        if (license == null)
            return NotFound();

        license.DisplayName = License.DisplayName;
        license.Description = License.Description;
        license.MaxUsers = License.MaxUsers;
        license.MaxApiCallsPerMonth = License.MaxApiCallsPerMonth;
        license.TierLevel = License.TierLevel;
        license.IsActive = License.IsActive;
        license.ModifiedAt = DateTime.UtcNow;
        license.ModifiedBy = User.Identity?.Name ?? "system";

        // Update features - remove old ones and add new ones
        var selectedTemplates = await _context.FeatureTemplates
            .Where(ft => SelectedFeatures.Contains(ft.Id))
            .ToListAsync();

        var selectedFeatureNames = selectedTemplates.Select(ft => ft.Name).ToList();
        var existingFeatureNames = license.LicenseFeatures.Select(lf => lf.Name).ToList();

        // Remove features no longer selected
        var featuresToRemove = license.LicenseFeatures
            .Where(lf => !selectedFeatureNames.Contains(lf.Name))
            .ToList();

        foreach (var feature in featuresToRemove)
        {
            _context.LicenseFeatures.Remove(feature);
        }

        // Add new features
        var featuresToAdd = selectedTemplates
            .Where(ft => !existingFeatureNames.Contains(ft.Name))
            .ToList();

        foreach (var template in featuresToAdd)
        {
            var licenseFeature = new LicenseFeature
            {
                Id = Guid.NewGuid(),
                LicenseId = license.Id,
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Category = template.Category,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system",
                TenantId = Guid.Empty,
                IsActive = true
            };

            _context.LicenseFeatures.Add(licenseFeature);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("License {LicenseId} updated by {User}", license.Id, User.Identity?.Name);

        TempData["SuccessMessage"] = "Licenza aggiornata con successo!";
        return RedirectToPage("/Dashboard/Licenses");
    }

    private async Task LoadLookupsAsync()
    {
        AvailableFeatures = await _context.FeatureTemplates
            .Where(ft => ft.IsAvailable)
            .OrderBy(ft => ft.Category)
            .ThenBy(ft => ft.SortOrder)
            .ThenBy(ft => ft.DisplayName)
            .Select(ft => new FeatureOption
            {
                Id = ft.Id,
                Name = ft.Name,
                DisplayName = ft.DisplayName,
                Description = ft.Description,
                Category = ft.Category,
                MinimumTierLevel = ft.MinimumTierLevel,
                SortOrder = ft.SortOrder
            })
            .ToListAsync();
    }

    public class LicenseFormModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Nome obbligatorio")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome visualizzato obbligatorio")]
        [StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 100000, ErrorMessage = "Massimo utenti deve essere tra 1 e 100000")]
        public int MaxUsers { get; set; }

        [Required]
        [Range(0, 10000000, ErrorMessage = "Max API calls deve essere tra 0 e 10000000")]
        public int MaxApiCallsPerMonth { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Tier level deve essere tra 1 e 10")]
        public int TierLevel { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class FeatureOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public int MinimumTierLevel { get; set; }
        public int SortOrder { get; set; }
    }

    public class TenantInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }
}
