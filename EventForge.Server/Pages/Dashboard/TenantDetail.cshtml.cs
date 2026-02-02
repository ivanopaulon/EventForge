using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Services.Auth;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class TenantDetailModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<TenantDetailModel> _logger;

    [BindProperty]
    public TenantFormModel Tenant { get; set; } = new();

    [BindProperty]
    public AdminUserFormModel AdminUser { get; set; } = new();

    public bool IsEditMode => !string.IsNullOrEmpty(Request.Query["id"]);
    public int CurrentUserCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public TenantDetailModel(
        EventForgeDbContext context,
        IPasswordService passwordService,
        ILogger<TenantDetailModel> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id.Value);

            if (tenant == null)
                return NotFound();

            Tenant = new TenantFormModel
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Code = tenant.Code,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                Domain = tenant.Domain,
                ContactEmail = tenant.ContactEmail,
                MaxUsers = tenant.MaxUsers,
                IsActive = tenant.IsActive,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                CreatedAt = tenant.CreatedAt,
                ModifiedAt = tenant.ModifiedAt
            };

            CurrentUserCount = await _context.Users
                .CountAsync(u => u.TenantId == tenant.Id);
        }
        else
        {
            Tenant = new TenantFormModel
            {
                IsActive = true,
                MaxUsers = 10
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Correggi gli errori nel form";
            return Page();
        }

        try
        {
            if (Tenant.Id == Guid.Empty)
            {
                return await CreateTenantAsync();
            }
            else
            {
                return await UpdateTenantAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tenant");
            ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            return Page();
        }
    }

    private async Task<IActionResult> CreateTenantAsync()
    {
        if (await _context.Tenants.AnyAsync(t => t.Name.ToLower() == Tenant.Name.ToLower()))
        {
            ErrorMessage = $"Esiste già un tenant con nome '{Tenant.Name}'";
            return Page();
        }

        if (await _context.Tenants.AnyAsync(t => t.Code.ToLower() == Tenant.Code.ToLower()))
        {
            ErrorMessage = $"Esiste già un tenant con codice '{Tenant.Code}'";
            return Page();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = Tenant.Name,
                Code = Tenant.Code.ToUpper(),
                DisplayName = Tenant.DisplayName,
                Description = Tenant.Description,
                Domain = Tenant.Domain,
                ContactEmail = Tenant.ContactEmail,
                MaxUsers = Tenant.MaxUsers,
                IsActive = Tenant.IsActive,
                SubscriptionExpiresAt = Tenant.SubscriptionExpiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name ?? "system"
            };
            tenant.TenantId = tenant.Id;

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var password = GenerateRandomPassword();
            var (hash, salt) = _passwordService.HashPassword(password);

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Username = AdminUser.Username,
                Email = AdminUser.Email,
                FirstName = AdminUser.FirstName,
                LastName = AdminUser.LastName,
                PasswordHash = hash,
                PasswordSalt = salt,
                MustChangePassword = true,
                IsActive = true,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            var superAdminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

            if (superAdminRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = superAdminRole.Id,
                    GrantedBy = User.Identity?.Name ?? "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Tenant {TenantName} created by {User} with admin user {AdminUsername}",
                tenant.Name, User.Identity?.Name, adminUser.Username);

            TempData["SuccessMessage"] = $"Tenant creato con successo! Password admin: {password}";

            return RedirectToPage("/Dashboard/Tenants");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<IActionResult> UpdateTenantAsync()
    {
        var tenant = await _context.Tenants.FindAsync(Tenant.Id);
        if (tenant == null)
            return NotFound();

        tenant.DisplayName = Tenant.DisplayName;
        tenant.Description = Tenant.Description;
        tenant.Domain = Tenant.Domain;
        tenant.ContactEmail = Tenant.ContactEmail;
        tenant.MaxUsers = Tenant.MaxUsers;
        tenant.IsActive = Tenant.IsActive;
        tenant.SubscriptionExpiresAt = Tenant.SubscriptionExpiresAt;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = User.Identity?.Name ?? "system";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} updated by {User}", tenant.Id, User.Identity?.Name);

        TempData["SuccessMessage"] = "Tenant aggiornato con successo!";
        return RedirectToPage("/Dashboard/Tenants");
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        const string symbols = "!@#$%&*";
        var password = new char[12];

        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var randomBytes = new byte[password.Length];
        rng.GetBytes(randomBytes);

        // Ensure at least one uppercase, lowercase, digit, and symbol
        password[0] = chars[randomBytes[0] % 25];  // Uppercase
        password[1] = chars[25 + (randomBytes[1] % 25)];  // Lowercase
        password[2] = chars[50 + (randomBytes[2] % (chars.Length - 50))];  // Digit
        password[3] = symbols[randomBytes[3] % symbols.Length];  // Symbol

        // Fill remaining positions
        for (int i = 4; i < password.Length; i++)
        {
            var useSymbol = randomBytes[i] % 10 == 0;
            password[i] = useSymbol 
                ? symbols[randomBytes[i] % symbols.Length] 
                : chars[randomBytes[i] % chars.Length];
        }

        // Shuffle the password
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = randomBytes[i] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    public class TenantFormModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Nome obbligatorio")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Codice obbligatorio")]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome visualizzato obbligatorio")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? Domain { get; set; }

        [Required(ErrorMessage = "Email contatto obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000, ErrorMessage = "Massimo utenti deve essere tra 1 e 10000")]
        public int MaxUsers { get; set; }

        public bool IsActive { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class AdminUserFormModel
    {
        [Required(ErrorMessage = "Username obbligatorio")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome obbligatorio")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cognome obbligatorio")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;
    }
}
