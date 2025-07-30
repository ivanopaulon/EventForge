using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = EventForge.DTOs.Common.AuditOperationType;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant management operations.
/// </summary>
public class TenantService : ITenantService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IPasswordService passwordService,
        ILogger<TenantService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _passwordService = passwordService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            _logger.LogWarning("Tentativo di creazione tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can create tenants.");
        }

        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.Name.ToLower());
        if (existingTenant != null)
        {
            _logger.LogWarning("Tenant con nome '{TenantName}' gi� esistente.", createDto.Name);
            throw new InvalidOperationException($"Tenant with name '{createDto.Name}' already exists.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = createDto.Name,
                DisplayName = createDto.DisplayName,
                Description = createDto.Description,
                Domain = createDto.Domain,
                ContactEmail = createDto.ContactEmail,
                MaxUsers = createDto.MaxUsers,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            tenant.TenantId = tenant.Id;

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var generatedPassword = GenerateRandomPassword();
            var (passwordHash, salt) = _passwordService.HashPassword(generatedPassword);

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
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var adminTenant = new AdminTenant
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UserId = currentUserId.Value,
                    ManagedTenantId = tenant.Id,
                    AccessLevel = AdminAccessLevel.FullAccess,
                    GrantedAt = DateTime.UtcNow
                };

                _context.AdminTenants.Add(adminTenant);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Audit log
            try
            {
                var currentUserIdForAudit = _tenantContext.CurrentUserId;
                if (currentUserIdForAudit.HasValue)
                {
                    var auditTrail = new AuditTrail
                    {
                        TenantId = tenant.Id,
                        OperationType = AuthAuditOperationType.TenantCreated,
                        PerformedByUserId = currentUserIdForAudit.Value,
                        TargetTenantId = tenant.Id,
                        Details = $"Tenant creato: {tenant.Name}",
                        WasSuccessful = true,
                        PerformedAt = DateTime.UtcNow
                    };
                    _context.AuditTrails.Add(auditTrail);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la scrittura dell'audit trail per la creazione tenant.");
            }

            var response = TenantMapper.ToServerResponseDto(tenant);
            response.AdminUser = new TenantAdminResponseDto
            {
                UserId = adminUser.Id,
                Username = adminUser.Username,
                Email = adminUser.Email,
                FirstName = adminUser.FirstName,
                LastName = adminUser.LastName,
                FullName = adminUser.FullName,
                MustChangePassword = adminUser.MustChangePassword,
                GeneratedPassword = generatedPassword
            };

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Errore durante la creazione del tenant.");
            throw;
        }
    }

    public async Task<TenantResponseDto?> GetTenantAsync(Guid tenantId)
    {
        try
        {
            var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                _logger.LogWarning("Accesso negato al tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            return tenant != null ? TenantMapper.ToServerResponseDto(tenant) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del tenant {TenantId}.", tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<TenantResponseDto>> GetAllTenantsAsync()
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only super administrators can view all tenants.");

            var tenants = await _context.Tenants
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return TenantMapper.ToServerResponseDtoCollection(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero di tutti i tenant.");
            throw;
        }
    }

    public async Task<TenantResponseDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto updateDto)
    {
        try
        {
            var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                _logger.LogWarning("Accesso negato all'aggiornamento del tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} non trovato per aggiornamento.", tenantId);
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            // Audit: copia originale
            var originalTenant = new Tenant
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                Domain = tenant.Domain,
                ContactEmail = tenant.ContactEmail,
                MaxUsers = tenant.MaxUsers,
                IsEnabled = tenant.IsEnabled,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                CreatedAt = tenant.CreatedAt,
                CreatedBy = tenant.CreatedBy,
                ModifiedAt = tenant.ModifiedAt,
                ModifiedBy = tenant.ModifiedBy
            };

            tenant.DisplayName = updateDto.DisplayName;
            tenant.Description = updateDto.Description;
            tenant.Domain = updateDto.Domain;
            tenant.ContactEmail = updateDto.ContactEmail;
            tenant.MaxUsers = updateDto.MaxUsers;
            tenant.IsEnabled = updateDto.IsEnabled;
            tenant.SubscriptionExpiresAt = updateDto.SubscriptionExpiresAt;
            tenant.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            try
            {
                var currentUserId = _tenantContext.CurrentUserId;
                if (currentUserId.HasValue)
                {
                    var auditTrail = new AuditTrail
                    {
                        TenantId = tenant.Id,
                        OperationType = AuthAuditOperationType.TenantUpdated,
                        PerformedByUserId = currentUserId.Value,
                        TargetTenantId = tenant.Id,
                        Details = $"Tenant aggiornato: {tenant.Name}",
                        WasSuccessful = true,
                        PerformedAt = DateTime.UtcNow
                    };
                    _context.AuditTrails.Add(auditTrail);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la scrittura dell'audit trail per l'aggiornamento tenant.");
            }

            return TenantMapper.ToServerResponseDto(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento del tenant {TenantId}.", tenantId);
            throw;
        }
    }

    public async Task SetTenantStatusAsync(Guid tenantId, bool isEnabled, string reason)
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di cambio stato tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can change tenant status.");
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} non trovato per cambio stato.", tenantId);
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            // Create copy for audit purposes
            var originalTenant = new Tenant
            {
                Id = tenant.Id,
                Name = tenant.Name,
                DisplayName = tenant.DisplayName,
                Description = tenant.Description,
                Domain = tenant.Domain,
                ContactEmail = tenant.ContactEmail,
                MaxUsers = tenant.MaxUsers,
                IsEnabled = tenant.IsEnabled,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                CreatedAt = tenant.CreatedAt,
                CreatedBy = tenant.CreatedBy,
                ModifiedAt = tenant.ModifiedAt,
                ModifiedBy = tenant.ModifiedBy
            };

            tenant.IsEnabled = isEnabled;
            tenant.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var auditTrail = new AuditTrail
                {
                    TenantId = tenant.Id,
                    OperationType = AuthAuditOperationType.TenantStatusChanged,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il cambio stato del tenant {TenantId}.", tenantId);
            throw;
        }
    }

    public async Task<AdminTenantResponseDto> AddTenantAdminAsync(Guid tenantId, Guid userId, AdminAccessLevel accessLevel)
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di aggiunta admin tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
            }

            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} non trovato per aggiunta admin.", tenantId);
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato per aggiunta admin.", userId);
                throw new ArgumentException($"User {userId} not found.");
            }

            var existingMapping = await _context.AdminTenants
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
            if (existingMapping != null)
            {
                _logger.LogWarning("Utente {UserId} gi� admin per tenant {TenantId}.", userId, tenantId);
                throw new InvalidOperationException($"User {userId} is already an admin for tenant {tenantId}.");
            }

            var adminTenant = new AdminTenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                ManagedTenantId = tenantId,
                AccessLevel = accessLevel,
                GrantedAt = DateTime.UtcNow
            };

            _context.AdminTenants.Add(adminTenant);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var auditTrail = new AuditTrail
                {
                    TenantId = tenantId,
                    OperationType = AuthAuditOperationType.AdminTenantGranted,
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
                AccessLevel = accessLevel.ToString(),
                GrantedAt = adminTenant.GrantedAt,
                ExpiresAt = adminTenant.ExpiresAt,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                TenantName = tenant.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta di un admin tenant {TenantId} - {UserId}.", tenantId, userId);
            throw;
        }
    }

    public async Task RemoveTenantAdminAsync(Guid tenantId, Guid userId)
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di rimozione admin tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
            }

            var adminTenant = await _context.AdminTenants
                .Include(at => at.User)
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
            if (adminTenant == null)
            {
                _logger.LogWarning("Admin mapping non trovato per utente {UserId} e tenant {TenantId}.", userId, tenantId);
                throw new ArgumentException($"Admin mapping not found for user {userId} and tenant {tenantId}.");
            }

            // Create copy for audit purposes
            var originalAdminTenant = new AdminTenant
            {
                Id = adminTenant.Id,
                UserId = adminTenant.UserId,
                ManagedTenantId = adminTenant.ManagedTenantId,
                AccessLevel = adminTenant.AccessLevel,
                GrantedAt = adminTenant.GrantedAt,
                ExpiresAt = adminTenant.ExpiresAt,
                CreatedAt = adminTenant.CreatedAt,
                CreatedBy = adminTenant.CreatedBy,
                ModifiedAt = adminTenant.ModifiedAt,
                ModifiedBy = adminTenant.ModifiedBy
            };

            _context.AdminTenants.Remove(adminTenant);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var auditTrail = new AuditTrail
                {
                    TenantId = tenantId,
                    OperationType = AuthAuditOperationType.AdminTenantRevoked,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la rimozione di un admin tenant {TenantId} - {UserId}.", tenantId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<AdminTenantResponseDto>> GetTenantAdminsAsync(Guid tenantId)
    {
        try
        {
            var canAccess = await _tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                _logger.LogWarning("Accesso negato alla lista admin per tenant {TenantId}.", tenantId);
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
                AccessLevel = at.AccessLevel.ToString(),
                GrantedAt = at.GrantedAt,
                ExpiresAt = at.ExpiresAt,
                Username = at.User.Username,
                Email = at.User.Email,
                FullName = at.User.FullName,
                TenantName = at.ManagedTenant.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero degli admin tenant {TenantId}.", tenantId);
            throw;
        }
    }

    public async Task ForcePasswordChangeAsync(Guid userId)
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di forzare cambio password non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can force password changes.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning("Utente {UserId} non trovato per forzatura cambio password.", userId);
                throw new ArgumentException($"User {userId} not found.");
            }

            // Create copy for audit purposes
            var originalUser = new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
                MustChangePassword = user.MustChangePassword,
                PasswordChangedAt = user.PasswordChangedAt,
                FailedLoginAttempts = user.FailedLoginAttempts,
                LockedUntil = user.LockedUntil,
                LastLoginAt = user.LastLoginAt,
                LastFailedLoginAt = user.LastFailedLoginAt,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                ModifiedAt = user.ModifiedAt,
                ModifiedBy = user.ModifiedBy
            };

            user.MustChangePassword = true;
            user.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = _tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var auditTrail = new AuditTrail
                {
                    TenantId = user.TenantId,
                    OperationType = AuthAuditOperationType.ForcePasswordChange,
                    PerformedByUserId = currentUserId.Value,
                    TargetUserId = userId,
                    Details = $"Password change forced",
                    WasSuccessful = true,
                    PerformedAt = DateTime.UtcNow
                };
                _context.AuditTrails.Add(auditTrail);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la forzatura del cambio password per utente {UserId}.", userId);
            throw;
        }
    }

    public async Task<PagedResult<AuditTrailResponseDto>> GetAuditTrailAsync(
        Guid? tenantId = null,
        AuditOperationType? operationType = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        try
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("Tentativo di accesso all'audit trail senza permessi.");
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

            return new PagedResult<AuditTrailResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dell'audit trail.");
            throw;
        }
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        const string specialChars = "!@#$%&*";

        var random = new Random();
        var password = new List<char>();

        password.Add(chars[random.Next(0, 26)]);
        password.Add(chars[random.Next(26, 52)]);
        password.Add(chars[random.Next(52, chars.Length)]);
        password.Add(specialChars[random.Next(specialChars.Length)]);

        for (int i = 4; i < 12; i++)
        {
            var allChars = chars + specialChars;
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        for (int i = password.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password.ToArray());
    }

    public async Task<TenantStatisticsDto> GetTenantStatisticsAsync()
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant statistics.");
        }

        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsEnabled);
        var inactiveTenants = totalTenants - activeTenants;

        var totalUsers = await _context.Users.CountAsync();
        var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        var usersLastMonth = await _context.Users.CountAsync(u => u.CreatedAt >= oneMonthAgo);

        var tenantsNearLimit = await _context.Tenants
            .Where(t => t.IsEnabled)
            .CountAsync(t => _context.Users.Count(u => u.TenantId == t.Id) >= t.MaxUsers * 0.9);

        return new TenantStatisticsDto
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            InactiveTenants = inactiveTenants,
            TotalUsers = totalUsers,
            UsersLastMonth = usersLastMonth,
            TenantsNearLimit = tenantsNearLimit
        };
    }

    public async Task<PagedResult<TenantResponseDto>> SearchTenantsAsync(TenantSearchDto searchDto)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can search tenants.");
        }

        var query = _context.Tenants.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchDto.SearchTerm))
        {
            var term = searchDto.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.DisplayName.ToLower().Contains(term) ||
                (t.Domain != null && t.Domain.ToLower().Contains(term)));
        }

        if (!string.IsNullOrEmpty(searchDto.Status) && searchDto.Status != "all")
        {
            var isActive = searchDto.Status == "active";
            query = query.Where(t => t.IsEnabled == isActive);
        }

        if (searchDto.MaxUsers.HasValue)
        {
            query = query.Where(t => t.MaxUsers <= searchDto.MaxUsers.Value);
        }

        if (searchDto.CreatedAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= searchDto.CreatedAfter.Value);
        }

        if (searchDto.CreatedBefore.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= searchDto.CreatedBefore.Value);
        }

        if (searchDto.NearUserLimit.HasValue && searchDto.NearUserLimit.Value)
        {
            query = query.Where(t => _context.Users.Count(u => u.TenantId == t.Id) >= t.MaxUsers * 0.9);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(searchDto.SortBy))
        {
            var isDesc = searchDto.SortOrder?.ToLower() == "desc";
            query = searchDto.SortBy.ToLower() switch
            {
                "name" => isDesc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "displayname" => isDesc ? query.OrderByDescending(t => t.DisplayName) : query.OrderBy(t => t.DisplayName),
                "createdat" => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
                "maxusers" => isDesc ? query.OrderByDescending(t => t.MaxUsers) : query.OrderBy(t => t.MaxUsers),
                _ => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        var totalCount = await query.CountAsync();
        var tenants = await query
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync();

        var tenantDtos = tenants.Select(t => new TenantResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            DisplayName = t.DisplayName,
            Description = t.Description,
            Domain = t.Domain,
            ContactEmail = t.ContactEmail,
            MaxUsers = t.MaxUsers,
            IsEnabled = t.IsEnabled,
            SubscriptionExpiresAt = t.SubscriptionExpiresAt,
            CreatedAt = t.CreatedAt,
            CreatedBy = t.CreatedBy,
            ModifiedAt = t.ModifiedAt,
            ModifiedBy = t.ModifiedBy
        }).ToList();

        return new PagedResult<TenantResponseDto>
        {
            Items = tenantDtos,
            TotalCount = totalCount,
            Page = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };
    }

    public async Task<TenantDetailDto?> GetTenantDetailsAsync(Guid tenantId)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant details.");
        }

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            return null;
        }

        var admins = await GetTenantAdminsAsync(tenantId);
        var limits = await GetTenantLimitsAsync(tenantId);
        var usageStats = await GetTenantUsageStatsAsync(tenantId);
        var recentActivities = await GetRecentActivitiesAsync(tenantId);

        return new TenantDetailDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsEnabled = tenant.IsEnabled,
            SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            ModifiedAt = tenant.ModifiedAt,
            ModifiedBy = tenant.ModifiedBy,
            Admins = admins.Select(a => new TenantAdminResponseDto
            {
                UserId = a.UserId,
                Username = a.Username,
                Email = a.Email,
                FullName = a.FullName,
                MustChangePassword = false, // Default value since not available in AdminTenantResponseDto
                GeneratedPassword = null // Only for creation
            }).ToList(),
            Limits = limits ?? new TenantLimitsDto { TenantId = tenantId },
            UsageStats = usageStats,
            RecentActivities = recentActivities
        };
    }

    public async Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid tenantId)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant limits.");
        }

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            return null;
        }

        var currentUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId);
        var currentStorage = await CalculateStorageUsageAsync(tenantId);
        var currentEventsThisMonth = await CalculateEventsThisMonthAsync(tenantId);

        return new TenantLimitsDto
        {
            TenantId = tenantId,
            MaxUsers = tenant.MaxUsers,
            CurrentUsers = currentUsers,
            MaxStorageBytes = 1073741824, // Default 1GB - should be configurable
            CurrentStorageBytes = currentStorage,
            MaxEventsPerMonth = 1000, // Default - should be configurable
            CurrentEventsThisMonth = currentEventsThisMonth
        };
    }

    public async Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid tenantId, UpdateTenantLimitsDto updateDto)
    {
        if (!_tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can update tenant limits.");
        }

        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        tenant.MaxUsers = updateDto.MaxUsers;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = _tenantContext.CurrentUserId?.ToString() ?? "System";

        await _context.SaveChangesAsync();

        // Create audit trail entry
        var auditTrail = new AuditTrail
        {
            PerformedByUserId = _tenantContext.CurrentUserId ?? Guid.Empty,
            OperationType = AuthAuditOperationType.TenantStatusChanged,
            TargetTenantId = tenantId,
            Details = $"Tenant limits updated: MaxUsers={updateDto.MaxUsers}, MaxStorage={updateDto.MaxStorageBytes}, MaxEvents={updateDto.MaxEventsPerMonth}. Reason: {updateDto.Reason}",
            WasSuccessful = true,
            PerformedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenant.ModifiedBy
        };

        _context.AuditTrails.Add(auditTrail);
        await _context.SaveChangesAsync();

        return await GetTenantLimitsAsync(tenantId) ?? throw new InvalidOperationException("Failed to retrieve updated limits.");
    }

    private async Task<TenantUsageStatsDto> GetTenantUsageStatsAsync(Guid tenantId)
    {
        var totalUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId);
        var activeUsers = await _context.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive);
        var totalEvents = await _context.Events.CountAsync(e => e.TenantId == tenantId);
        var eventsThisMonth = await CalculateEventsThisMonthAsync(tenantId);
        var storageUsed = await CalculateStorageUsageAsync(tenantId);
        var lastActivity = await _context.AuditTrails
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Select(a => a.PerformedAt)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;
        var loginAttemptsToday = await _context.AuditTrails
            .CountAsync(a => a.OperationType == AuthAuditOperationType.TenantSwitch &&
                           a.PerformedAt >= today &&
                           a.SourceTenantId == tenantId);

        var failedLoginsToday = await _context.AuditTrails
            .CountAsync(a => a.OperationType == AuthAuditOperationType.TenantStatusChanged &&
                           a.PerformedAt >= today &&
                           a.SourceTenantId == tenantId);

        return new TenantUsageStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalEvents = totalEvents,
            EventsThisMonth = eventsThisMonth,
            StorageUsedBytes = storageUsed,
            LastActivity = lastActivity,
            LoginAttemptsToday = loginAttemptsToday,
            FailedLoginsToday = failedLoginsToday
        };
    }

    private async Task<List<string>> GetRecentActivitiesAsync(Guid tenantId)
    {
        var recentAudits = await _context.AuditTrails
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Take(10)
            .Select(a => $"{a.PerformedAt:yyyy-MM-dd HH:mm} - {a.OperationType}: {a.Details}")
            .ToListAsync();

        return recentAudits;
    }

    private async Task<long> CalculateStorageUsageAsync(Guid tenantId)
    {
        // This is a placeholder implementation
        // In a real application, you would calculate actual storage usage
        // from file uploads, documents, etc.
        var userCount = await _context.Users.CountAsync(u => u.TenantId == tenantId);
        var eventCount = await _context.Events.CountAsync(e => e.TenantId == tenantId);

        // Rough estimation: 1KB per user + 10KB per event
        return (userCount * 1024) + (eventCount * 10240);
    }

    private async Task<int> CalculateEventsThisMonthAsync(Guid tenantId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _context.Events
            .CountAsync(e => e.TenantId == tenantId && e.CreatedAt >= startOfMonth);
    }

    public async Task SoftDeleteTenantAsync(Guid tenantId, string reason)
    {
        if (!_tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only super administrators can delete tenants.");

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
            throw new ArgumentException($"Tenant {tenantId} not found.");

        if (tenant.IsDeleted)
            throw new InvalidOperationException("Tenant is already deleted.");

        tenant.IsDeleted = true;
        tenant.IsEnabled = false;
        tenant.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var currentUserId = _tenantContext.CurrentUserId;
        if (currentUserId.HasValue)
        {
            var auditTrail = new AuditTrail
            {
                TenantId = tenant.Id,
                OperationType = AuthAuditOperationType.TenantStatusChanged,
                PerformedByUserId = currentUserId.Value,
                TargetTenantId = tenant.Id,
                Details = $"Tenant soft deleted: {reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow
            };
            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }
    }
}