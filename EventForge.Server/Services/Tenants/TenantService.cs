using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant management operations.
/// </summary>
public class TenantService : ITenantService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;

    public TenantService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IPasswordService passwordService,
        IMapper mapper)
    {
        _context = context;
        _tenantContext = tenantContext;
        _passwordService = passwordService;
        _mapper = mapper;
    }

    public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can create tenants.");
        }

        // Check if tenant name already exists
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.Name.ToLower());
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Tenant with name '{createDto.Name}' already exists.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create tenant
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(), // Self-referencing for consistency
                Name = createDto.Name,
                DisplayName = createDto.DisplayName,
                Description = createDto.Description,
                Domain = createDto.Domain,
                ContactEmail = createDto.ContactEmail,
                MaxUsers = createDto.MaxUsers,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            // Set the tenant's own TenantId to itself for consistency
            tenant.TenantId = tenant.Id;

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Generate random password for admin user
            var generatedPassword = GenerateRandomPassword();
            var (passwordHash, salt) = _passwordService.HashPassword(generatedPassword);

            // Create admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Username = createDto.AdminUser.Username,
                Email = createDto.AdminUser.Email,
                FirstName = createDto.AdminUser.FirstName,
                LastName = createDto.AdminUser.LastName,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                MustChangePassword = true, // Force password change on first login
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // Add admin tenant mapping for the current super admin
            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var adminTenant = new AdminTenant
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id, // AdminTenant belongs to the new tenant
                    UserId = currentUserId.Value,
                    ManagedTenantId = tenant.Id,
                    AccessLevel = AdminAccessLevel.FullAccess,
                    GrantedAt = DateTime.UtcNow
                };

                _context.AdminTenants.Add(adminTenant);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Map response
            var response = _mapper.Map<TenantResponseDto>(tenant);
            response.AdminUser = new TenantAdminResponseDto
            {
                UserId = adminUser.Id,
                Username = adminUser.Username,
                Email = adminUser.Email,
                FirstName = adminUser.FirstName,
                LastName = adminUser.LastName,
                FullName = adminUser.FullName,
                MustChangePassword = adminUser.MustChangePassword,
                GeneratedPassword = generatedPassword // Only included in creation response
            };

            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TenantResponseDto?> GetTenantAsync(Guid tenantId)
    {
        var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant != null ? _mapper.Map<TenantResponseDto>(tenant) : null;
    }

    public async Task<IEnumerable<TenantResponseDto>> GetAllTenantsAsync()
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view all tenants.");
        }

        var tenants = await _context.Tenants
            .OrderBy(t => t.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<TenantResponseDto>>(tenants);
    }

    public async Task<TenantResponseDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto updateDto)
    {
        var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        // Update tenant properties
        tenant.DisplayName = updateDto.DisplayName;
        tenant.Description = updateDto.Description;
        tenant.Domain = updateDto.Domain;
        tenant.ContactEmail = updateDto.ContactEmail;
        tenant.MaxUsers = updateDto.MaxUsers;
        tenant.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<TenantResponseDto>(tenant);
    }

    public async Task SetTenantStatusAsync(Guid tenantId, bool isEnabled, string reason)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can change tenant status.");
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        tenant.IsEnabled = isEnabled;
        tenant.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create audit trail
        var currentUserId = _tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenant.Id,
                OperationType = AuditOperationType.TenantStatusChanged,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenant.Id,
                Details = $"Tenant {(isEnabled ? "enabled" : "disabled")}: {reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<AdminTenantResponseDto> AddTenantAdminAsync(Guid tenantId, Guid userId, AdminAccessLevel accessLevel)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
        }

        // Validate tenant exists
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        // Validate user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new ArgumentException($"User {userId} not found.");
        }

        // Check if mapping already exists
        var existingMapping = await _context.AdminTenants
            .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
        if (existingMapping != null)
        {
            throw new InvalidOperationException($"User {userId} is already an admin for tenant {tenantId}.");
        }

        // Create admin tenant mapping
        var adminTenant = new AdminTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId, // AdminTenant belongs to the managed tenant
            UserId = userId,
            ManagedTenantId = tenantId,
            AccessLevel = accessLevel,
            GrantedAt = DateTime.UtcNow
        };

        _context.AdminTenants.Add(adminTenant);
        await _context.SaveChangesAsync();

        // Create audit trail
        var currentUserId = _tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenantId,
                OperationType = AuditOperationType.AdminTenantGranted,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenantId,
                TargetUserId = userId,
                Details = $"Admin access level {accessLevel} granted",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }

        return new AdminTenantResponseDto
        {
            Id = adminTenant.Id,
            UserId = userId,
            ManagedTenantId = tenantId,
            AccessLevel = accessLevel,
            GrantedAt = adminTenant.GrantedAt,
            ExpiresAt = adminTenant.ExpiresAt,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            TenantName = tenant.Name
        };
    }

    public async Task RemoveTenantAdminAsync(Guid tenantId, Guid userId)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
        }

        var adminTenant = await _context.AdminTenants
            .Include(at => at.User)
            .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
        if (adminTenant == null)
        {
            throw new ArgumentException($"Admin mapping not found for user {userId} and tenant {tenantId}.");
        }

        _context.AdminTenants.Remove(adminTenant);
        await _context.SaveChangesAsync();

        // Create audit trail
        var currentUserId = _tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenantId,
                OperationType = AuditOperationType.AdminTenantRevoked,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenantId,
                TargetUserId = userId,
                Details = $"Admin access revoked for {adminTenant.User.Username}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AdminTenantResponseDto>> GetTenantAdminsAsync(Guid tenantId)
    {
        var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
        if (!canAccess)
        {
            throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
        }

        var adminTenants = await _context.AdminTenants
            .Include(at => at.User)
            .Include(at => at.ManagedTenant)
            .Where(at => at.ManagedTenantId == tenantId)
            .ToListAsync();

        return adminTenants.Select(at => new AdminTenantResponseDto
        {
            Id = at.Id,
            UserId = at.UserId,
            ManagedTenantId = at.ManagedTenantId,
            AccessLevel = at.AccessLevel,
            GrantedAt = at.GrantedAt,
            ExpiresAt = at.ExpiresAt,
            Username = at.User.Username,
            Email = at.User.Email,
            FullName = at.User.FullName,
            TenantName = at.ManagedTenant.Name
        });
    }

    public async Task ForcePasswordChangeAsync(Guid userId)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can force password changes.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new ArgumentException($"User {userId} not found.");
        }

        user.MustChangePassword = true;
        user.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<AuditTrailResponseDto>> GetAuditTrailAsync(
        Guid? tenantId = null,
        AuditOperationType? operationType = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view audit trails.");
        }

        var query = _context.AuditTrails
            .Include(at => at.PerformedByUser)
            .Include(at => at.SourceTenant)
            .Include(at => at.TargetTenant)
            .Include(at => at.TargetUser)
            .AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(at => at.SourceTenantId == tenantId.Value || at.TargetTenantId == tenantId.Value);
        }

        if (operationType.HasValue)
        {
            query = query.Where(at => at.OperationType == operationType.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(at => at.PerformedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(at => new AuditTrailResponseDto
            {
                Id = at.Id,
                OperationType = at.OperationType,
                PerformedByUserId = at.PerformedByUserId,
                PerformedByUsername = at.PerformedByUser.Username,
                SourceTenantId = at.SourceTenantId,
                SourceTenantName = at.SourceTenant != null ? at.SourceTenant.Name : null,
                TargetTenantId = at.TargetTenantId,
                TargetTenantName = at.TargetTenant != null ? at.TargetTenant.Name : null,
                TargetUserId = at.TargetUserId,
                TargetUsername = at.TargetUser != null ? at.TargetUser.Username : null,
                SessionId = at.SessionId,
                IpAddress = at.IpAddress,
                UserAgent = at.UserAgent,
                Details = at.Details,
                WasSuccessful = at.WasSuccessful,
                ErrorMessage = at.ErrorMessage,
                PerformedAt = at.PerformedAt
            })
            .ToListAsync();

        return new PaginatedResponse<AuditTrailResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const string specialChars = "!@#$%&*";

        var random = new Random();
        var password = new List<char>();

        // Add at least one character from each category
        password.Add(chars[random.Next(0, 26)]); // Uppercase
        password.Add(chars[random.Next(26, 52)]); // Lowercase  
        password.Add(chars[random.Next(52, chars.Length)]); // Number
        password.Add(specialChars[random.Next(specialChars.Length)]); // Special

        // Fill the rest randomly
        for (int i = 4; i < 12; i++)
        {
            var allChars = chars + specialChars;
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password
        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }
}