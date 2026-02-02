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
public class UserDetailModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UserDetailModel> _logger;

    [BindProperty]
    public UserFormModel User { get; set; } = new();

    [BindProperty]
    public List<Guid> SelectedRoles { get; set; } = new();

    public List<RoleOption> AvailableRoles { get; set; } = new();
    public List<TenantOption> AvailableTenants { get; set; } = new();
    public bool IsEditMode => !string.IsNullOrEmpty(Request.Query["id"]);
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public UserDetailModel(
        EventForgeDbContext context,
        IPasswordService passwordService,
        ILogger<UserDetailModel> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        await LoadLookupsAsync();

        if (id.HasValue)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id.Value);

            if (user == null)
                return NotFound();

            User = new UserFormModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantId = user.TenantId,
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                CreatedAt = user.CreatedAt,
                ModifiedAt = user.ModifiedAt,
                PasswordChangedAt = user.PasswordChangedAt,
                LastLoginAt = user.LastLoginAt
            };

            SelectedRoles = user.UserRoles.Select(ur => ur.RoleId).ToList();
        }
        else
        {
            User = new UserFormModel
            {
                IsActive = true,
                MustChangePassword = true
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
            if (User.Id == Guid.Empty)
            {
                return await CreateUserAsync();
            }
            else
            {
                return await UpdateUserAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user");
            ErrorMessage = $"Errore durante il salvataggio: {ex.Message}";
            return Page();
        }
    }

    private async Task<IActionResult> CreateUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == User.Username.ToLower()))
        {
            ErrorMessage = $"Esiste già un utente con username '{User.Username}'";
            return Page();
        }

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == User.Email.ToLower()))
        {
            ErrorMessage = $"Esiste già un utente con email '{User.Email}'";
            return Page();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var password = GenerateRandomPassword();
            var (hash, salt) = _passwordService.HashPassword(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = User.TenantId,
                Username = User.Username,
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                PasswordHash = hash,
                PasswordSalt = salt,
                MustChangePassword = User.MustChangePassword,
                IsActive = User.IsActive,
                CreatedBy = this.User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign roles
            foreach (var roleId in SelectedRoles)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    GrantedBy = this.User.Identity?.Name ?? "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = this.User.Identity?.Name ?? "system",
                    CreatedAt = DateTime.UtcNow,
                    TenantId = user.TenantId
                };

                _context.UserRoles.Add(userRole);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "User {Username} created by {Admin} with password: {Password}",
                user.Username, this.User.Identity?.Name, password);

            TempData["SuccessMessage"] = $"Utente creato con successo! Password: {password}";

            return RedirectToPage("/Dashboard/Users");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<IActionResult> UpdateUserAsync()
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == User.Id);

        if (user == null)
            return NotFound();

        user.Email = User.Email;
        user.FirstName = User.FirstName;
        user.LastName = User.LastName;
        user.TenantId = User.TenantId;
        user.IsActive = User.IsActive;
        user.MustChangePassword = User.MustChangePassword;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = this.User.Identity?.Name ?? "system";

        // Update roles - remove old ones and add new ones
        var existingRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var rolesToRemove = user.UserRoles.Where(ur => !SelectedRoles.Contains(ur.RoleId)).ToList();
        var rolesToAdd = SelectedRoles.Where(r => !existingRoleIds.Contains(r)).ToList();

        // Remove roles
        foreach (var userRole in rolesToRemove)
        {
            _context.UserRoles.Remove(userRole);
        }

        // Add new roles
        foreach (var roleId in rolesToAdd)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                GrantedBy = this.User.Identity?.Name ?? "system",
                GrantedAt = DateTime.UtcNow,
                CreatedBy = this.User.Identity?.Name ?? "system",
                CreatedAt = DateTime.UtcNow,
                TenantId = user.TenantId
            };

            _context.UserRoles.Add(userRole);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated by {Admin}", user.Id, this.User.Identity?.Name);

        TempData["SuccessMessage"] = "Utente aggiornato con successo!";
        return RedirectToPage("/Dashboard/Users");
    }

    private async Task LoadLookupsAsync()
    {
        AvailableRoles = await _context.Roles
            .OrderBy(r => r.DisplayName)
            .Select(r => new RoleOption
            {
                Id = r.Id,
                Name = r.Name,
                DisplayName = r.DisplayName,
                Description = r.Description
            })
            .ToListAsync();

        AvailableTenants = await _context.Tenants
            .OrderBy(t => t.DisplayName)
            .Select(t => new TenantOption
            {
                Id = t.Id,
                Code = t.Code,
                DisplayName = t.DisplayName
            })
            .ToListAsync();
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

    public class UserFormModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Username obbligatorio")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email obbligatoria")]
        [EmailAddress(ErrorMessage = "Email non valida")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome obbligatorio")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cognome obbligatorio")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tenant obbligatorio")]
        public Guid TenantId { get; set; }

        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime? PasswordChangedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class RoleOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class TenantOption
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
