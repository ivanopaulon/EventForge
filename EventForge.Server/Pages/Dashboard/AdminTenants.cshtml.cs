using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Common;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// SuperAdmin platform-wide view of AdminTenant grants (cross-tenant admin access mappings).
/// Grant/revoke operations go through <see cref="ITenantService"/> so that authorization checks
/// and the audit trail (AdminTenantGranted / AdminTenantRevoked) are preserved — no direct
/// database mutation is used for these operations. Statistics/list queries use
/// <see cref="EventForgeDbContext"/> directly for read-only reporting, consistent with the other
/// pages in this SuperAdmin panel.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class AdminTenantsModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AdminTenantsModel> _logger;

    /// <summary>
    /// Maximum allowed duration, in days, for a new grant's expiration (mirrors the policy
    /// enforced in <see cref="TenantService.MaxAdminGrantDurationDays"/>).
    /// </summary>
    public int MaxGrantDurationDays => TenantService.MaxAdminGrantDurationDays;

    /// <summary>
    /// Default proposed duration, in days, pre-filled in the new grant form (modifiable up to
    /// <see cref="MaxGrantDurationDays"/>).
    /// </summary>
    private const int DefaultGrantDurationDays = 30;

    // Statistics
    public int TotalActiveGrants { get; set; }
    public int ExpiringWithin7Days { get; set; }
    public int LegacyWithoutExpiration { get; set; }

    public List<AdminTenantListItem> Grants { get; set; } = new();
    public List<TenantOption> AvailableTenants { get; set; } = new();
    public List<UserOption> AvailableUsers { get; set; } = new();

    [BindProperty]
    public GrantFormModel Grant { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public AdminTenantsModel(
        EventForgeDbContext context,
        ITenantService tenantService,
        ILogger<AdminTenantsModel> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;

        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGrantAsync()
    {
        await LoadDropdownsAsync();

        if (!ModelState.IsValid)
        {
            await LoadGrantsAsync();
            return Page();
        }

        try
        {
            _ = await _tenantService.AddTenantAdminAsync(
                Grant.TenantId,
                Grant.UserId,
                Grant.AccessLevel,
                Grant.Reason,
                Grant.ExpiresAt);

            TempData["SuccessMessage"] = "Accesso admin concesso con successo.";
            return RedirectToPage();
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = "Accesso negato: " + ex.Message;
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadGrantsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid tenantId, Guid userId)
    {
        try
        {
            await _tenantService.RemoveTenantAdminAsync(tenantId, userId);
            TempData["SuccessMessage"] = "Accesso admin revocato con successo.";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = "Accesso negato: " + ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        await LoadDropdownsAsync();
        await LoadGrantsAsync();

        Grant.ExpiresAt = DateTime.UtcNow.AddDays(DefaultGrantDurationDays);
        Grant.AccessLevel = AdminAccessLevel.TenantAdmin;
    }

    private async Task LoadDropdownsAsync()
    {
        AvailableTenants = await _context.Tenants
            .OrderBy(t => t.DisplayName)
            .Select(t => new TenantOption { Id = t.Id, DisplayName = t.DisplayName })
            .ToListAsync();

        AvailableUsers = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserOption { Id = u.Id, Username = u.Username, Email = u.Email })
            .ToListAsync();
    }

    private async Task LoadGrantsAsync()
    {
        var now = DateTime.UtcNow;
        var in7Days = now.AddDays(7);

        var adminTenants = await _context.AdminTenants
            .AsNoTracking()
            .Include(at => at.User)
            .Include(at => at.ManagedTenant)
            .OrderBy(at => at.ExpiresAt == null ? 0 : 1)
            .ThenBy(at => at.ExpiresAt)
            .ToListAsync();

        Grants = adminTenants
            .Select(at => new AdminTenantListItem
            {
                Id = at.Id,
                TenantId = at.ManagedTenantId,
                TenantName = at.ManagedTenant.DisplayName,
                UserId = at.UserId,
                Username = at.User.Username,
                Email = at.User.Email,
                AccessLevel = at.AccessLevel.ToString(),
                Reason = at.Reason,
                GrantedAt = at.GrantedAt,
                ExpiresAt = at.ExpiresAt,
                IsLegacyNoExpiration = at.ExpiresAt is null,
                IsExpiringSoon = at.ExpiresAt is not null && at.ExpiresAt.Value <= in7Days
            })
            .ToList();

        TotalActiveGrants = Grants.Count;
        ExpiringWithin7Days = Grants.Count(g => g.IsExpiringSoon);
        LegacyWithoutExpiration = Grants.Count(g => g.IsLegacyNoExpiration);
    }

    // View models

    public class AdminTenantListItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsLegacyNoExpiration { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class TenantOption
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class UserOption
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class GrantFormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Seleziona un tenant.")]
        public Guid TenantId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Seleziona un utente.")]
        public Guid UserId { get; set; }

        public AdminAccessLevel AccessLevel { get; set; } = AdminAccessLevel.TenantAdmin;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "La motivazione è obbligatoria.")]
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "La scadenza è obbligatoria.")]
        public DateTime ExpiresAt { get; set; }
    }
}
