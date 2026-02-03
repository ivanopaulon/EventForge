using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Services.Configuration;
using EventForge.DTOs.Configuration;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class BrandingModel : PageModel
{
    private readonly IBrandingService _brandingService;
    private readonly EventForgeDbContext _context;
    private readonly ILogger<BrandingModel> _logger;

    public BrandingConfigurationDto GlobalBranding { get; set; } = new();
    public List<TenantInfo> Tenants { get; set; } = new();
    public int TenantOverridesCount { get; set; }
    
    [BindProperty]
    public GlobalBrandingForm GlobalForm { get; set; } = new();
    
    public string? ErrorMessage { get; set; }

    public BrandingModel(
        IBrandingService brandingService,
        EventForgeDbContext context,
        ILogger<BrandingModel> logger)
    {
        _brandingService = brandingService;
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            // Load global branding
            GlobalBranding = await _brandingService.GetBrandingAsync(null);
            
            // Populate form
            GlobalForm.ApplicationName = GlobalBranding.ApplicationName;
            GlobalForm.LogoHeight = GlobalBranding.LogoHeight;
            GlobalForm.FaviconUrl = GlobalBranding.FaviconUrl;
            
            // Load tenants
            Tenants = await _context.Tenants
                .Where(t => !t.IsDeleted)
                .Select(t => new TenantInfo 
                { 
                    Id = t.Id, 
                    DisplayName = t.DisplayName,
                    Code = t.Code,
                    HasCustomBranding = !string.IsNullOrWhiteSpace(t.CustomLogoUrl)
                })
                .OrderBy(t => t.DisplayName)
                .ToListAsync();
            
            // Count tenant overrides
            TenantOverridesCount = Tenants.Count(t => t.HasCustomBranding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading branding configuration page");
            ErrorMessage = "Errore nel caricamento della configurazione";
        }
    }

    public async Task<IActionResult> OnPostUpdateGlobalAsync(IFormFile? logoFile)
    {
        try
        {
            var updateDto = new UpdateBrandingDto
            {
                ApplicationName = GlobalForm.ApplicationName,
                LogoHeight = GlobalForm.LogoHeight,
                FaviconUrl = GlobalForm.FaviconUrl
            };

            // Upload logo if provided
            if (logoFile != null && logoFile.Length > 0)
            {
                var logoUrl = await _brandingService.UploadLogoAsync(logoFile, null);
                updateDto.LogoUrl = logoUrl;
            }

            var username = User.Identity?.Name ?? "system";
            await _brandingService.UpdateGlobalBrandingAsync(updateDto, username);

            _logger.LogInformation("Global branding updated by {User}", username);
            TempData["SuccessMessage"] = "Configurazione globale salvata con successo!";
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global branding");
            ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateTenantAsync(Guid tenantId, IFormFile? tenantLogoFile, string? tenantApplicationName)
    {
        try
        {
            var updateDto = new UpdateBrandingDto
            {
                ApplicationName = tenantApplicationName
            };

            // Upload tenant logo if provided
            if (tenantLogoFile != null && tenantLogoFile.Length > 0)
            {
                var logoUrl = await _brandingService.UploadLogoAsync(tenantLogoFile, tenantId);
                updateDto.LogoUrl = logoUrl;
            }

            var username = User.Identity?.Name ?? "system";
            await _brandingService.UpdateTenantBrandingAsync(tenantId, updateDto, username);

            _logger.LogInformation("Tenant branding updated for {TenantId} by {User}", tenantId, username);
            TempData["SuccessMessage"] = "Branding tenant salvato con successo!";
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant branding for {TenantId}", tenantId);
            ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResetTenantAsync(Guid tenantId)
    {
        try
        {
            await _brandingService.DeleteTenantBrandingAsync(tenantId);
            
            _logger.LogInformation("Tenant branding reset for {TenantId}", tenantId);
            TempData["SuccessMessage"] = "Branding tenant resettato a globale!";
            
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting tenant branding for {TenantId}", tenantId);
            ErrorMessage = $"Errore durante il reset: {ex.Message}";
            await OnGetAsync();
            return Page();
        }
    }

    public class GlobalBrandingForm
    {
        [Required]
        [StringLength(100)]
        public string ApplicationName { get; set; } = "EventForge";

        [Range(20, 200)]
        public int LogoHeight { get; set; } = 40;

        [StringLength(500)]
        public string? FaviconUrl { get; set; }
    }

    public class TenantInfo
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool HasCustomBranding { get; set; }
    }
}
