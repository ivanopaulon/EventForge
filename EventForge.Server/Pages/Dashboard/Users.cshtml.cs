using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EventForge.Server.Data;
using EventForge.Server.Services.Auth;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class UsersModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UsersModel> _logger;

    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuperAdminCount { get; set; }
    public int MustChangePasswordCount { get; set; }

    public List<UserListItem> Users { get; set; } = new();
    public List<TenantOption> AvailableTenants { get; set; } = new();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public Guid? TenantFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public UsersModel(
        EventForgeDbContext context,
        IPasswordService passwordService,
        ILogger<UsersModel> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        // Statistics
        TotalUsers = await _context.Users.CountAsync();
        ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
        SuperAdminCount = await _context.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "SuperAdmin"))
            .CountAsync();
        MustChangePasswordCount = await _context.Users.CountAsync(u => u.MustChangePassword);

        // Tenants for filter
        AvailableTenants = await _context.Tenants
            .OrderBy(t => t.DisplayName)
            .Select(t => new TenantOption
            {
                Id = t.Id,
                DisplayName = t.DisplayName
            })
            .ToListAsync();

        // Query users
        var query = _context.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(search) || 
                u.Email.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        // Tenant filter
        if (TenantFilter.HasValue)
            query = query.Where(u => u.TenantId == TenantFilter.Value);

        // Status filter
        if (StatusFilter == "active")
            query = query.Where(u => u.IsActive);
        else if (StatusFilter == "inactive")
            query = query.Where(u => !u.IsActive);

        // Load
        Users = await query
            .OrderBy(u => u.Tenant.DisplayName)
            .ThenBy(u => u.Username)
            .Select(u => new UserListItem
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                TenantCode = u.Tenant.Code,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LastLoginAt = u.LastLoginAt,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        var newPassword = GenerateRandomPassword();
        var (hash, salt) = _passwordService.HashPassword(newPassword);

        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = User.Identity?.Name ?? "system";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Password reset for user {UserId} by {Admin}. New password: {Password}", 
            id, User.Identity?.Name, newPassword);

        TempData["SuccessMessage"] = $"Password resettata per {user.Username}. Nuova password: {newPassword}";
        
        return RedirectToPage();
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        const string symbols = "!@#$%&*";
        var random = new Random();
        var password = new char[12];

        password[0] = chars[random.Next(0, 25)];
        password[1] = chars[random.Next(25, 50)];
        password[2] = chars[random.Next(50, chars.Length)];
        password[3] = symbols[random.Next(symbols.Length)];

        for (int i = 4; i < password.Length; i++)
        {
            var useSymbol = random.Next(10) == 0;
            password[i] = useSymbol 
                ? symbols[random.Next(symbols.Length)] 
                : chars[random.Next(chars.Length)];
        }

        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    public class UserListItem
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class TenantOption
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
