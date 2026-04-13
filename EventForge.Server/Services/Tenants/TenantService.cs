using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;

namespace EventForge.Server.Services.Tenants;

/// <summary>
/// Implementation of tenant management operations.
/// </summary>
public class TenantService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    IPasswordService passwordService,
    ILogger<TenantService> logger) : ITenantService
{

    public async Task<TenantResponseDto> CreateTenantAsync(CreateTenantDto createDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di creazione tenant non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can create tenants.");
        }

        // Check if tenant code already exists
        var existingTenantByCode = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code.ToLower() == createDto.Code.ToLower());
        if (existingTenantByCode is not null)
        {
            logger.LogWarning("Tenant con codice '{TenantCode}' già esistente.", createDto.Code);
            throw new InvalidOperationException($"Tenant with code '{createDto.Code}' already exists.");
        }

        var existingTenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.Name.ToLower());
        if (existingTenant is not null)
        {
            logger.LogWarning("Tenant con nome '{TenantName}' già esistente.", createDto.Name);
            throw new InvalidOperationException($"Tenant with name '{createDto.Name}' already exists.");
        }

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = createDto.Name,
                Code = createDto.Code,
                DisplayName = createDto.DisplayName,
                Description = createDto.Description,
                Domain = createDto.Domain,
                ContactEmail = createDto.ContactEmail,
                MaxUsers = createDto.MaxUsers,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            tenant.TenantId = tenant.Id;

            _ = context.Tenants.Add(tenant);
            _ = await context.SaveChangesAsync();

            // Admin users are not automatically created during tenant creation
            // to avoid FK constraint violations. Admin users should be assigned
            // separately using AddTenantAdminAsync after user creation.

            await transaction.CommitAsync();

            // Audit log
            try
            {
                var currentUserIdForAudit = tenantContext.CurrentUserId;
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
                    _ = context.AuditTrails.Add(auditTrail);
                    _ = await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante la scrittura dell'audit trail per la creazione tenant.");
            }

            var response = TenantMapper.ToServerResponseDto(tenant);
            // Admin users are not automatically created or assigned during tenant creation
            // to prevent FK constraint violations. Use AddTenantAdminAsync to assign admins after user creation.
            response.AdminUser = null;

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Errore durante la creazione del tenant.");
            throw;
        }
    }

    public async Task<TenantResponseDto> CreateTenantWithAdminAsync(CreateTenantDto createDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            logger.LogWarning("Tentativo di creazione tenant con admin non autorizzato.");
            throw new UnauthorizedAccessException("Only super administrators can create tenants with admin users.");
        }

        if (createDto.AdminUser is null)
        {
            logger.LogWarning("Admin user information is required for tenant creation with admin.");
            throw new InvalidOperationException("Admin user information is required.");
        }

        // Check if tenant code already exists
        var existingTenantByCode = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code.ToLower() == createDto.Code.ToLower());
        if (existingTenantByCode is not null)
        {
            logger.LogWarning("Tenant con codice '{TenantCode}' già esistente.", createDto.Code);
            throw new InvalidOperationException($"Tenant with code '{createDto.Code}' already exists.");
        }

        var existingTenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.Name.ToLower());
        if (existingTenant is not null)
        {
            logger.LogWarning("Tenant con nome '{TenantName}' già esistente.", createDto.Name);
            throw new InvalidOperationException($"Tenant with name '{createDto.Name}' already exists.");
        }

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Create tenant first
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = createDto.Name,
                Code = createDto.Code,
                DisplayName = createDto.DisplayName,
                Description = createDto.Description,
                Domain = createDto.Domain,
                ContactEmail = createDto.ContactEmail,
                MaxUsers = createDto.MaxUsers,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            tenant.TenantId = tenant.Id;
            _ = context.Tenants.Add(tenant);
            _ = await context.SaveChangesAsync();

            // Check if username already exists in the tenant
            var existingUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == createDto.AdminUser.Username.ToLower()
                                     && u.TenantId == tenant.Id);
            if (existingUser is not null)
            {
                throw new InvalidOperationException($"Username '{createDto.AdminUser.Username}' already exists in this tenant.");
            }

            // Check if email already exists in the tenant
            var existingUserByEmail = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == createDto.AdminUser.Email.ToLower()
                                     && u.TenantId == tenant.Id);
            if (existingUserByEmail is not null)
            {
                throw new InvalidOperationException($"Email '{createDto.AdminUser.Email}' already exists in this tenant.");
            }

            // Generate random password for admin user
            var randomPassword = GenerateRandomPassword();
            var (hash, salt) = passwordService.HashPassword(randomPassword);

            // Create admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Username = createDto.AdminUser.Username,
                Email = createDto.AdminUser.Email,
                FirstName = createDto.AdminUser.FirstName,
                LastName = createDto.AdminUser.LastName,
                PasswordHash = hash,
                PasswordSalt = salt,
                MustChangePassword = true, // Force password change on first login
                IsActive = true,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            _ = context.Users.Add(adminUser);
            _ = await context.SaveChangesAsync();

            // Assign SuperAdmin role to the user
            var superAdminRole = await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

            if (superAdminRole is not null)
            {
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = superAdminRole.Id,
                    GrantedBy = "system",
                    GrantedAt = DateTime.UtcNow,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                };

                _ = context.UserRoles.Add(userRole);
                _ = await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Audit log
            try
            {
                var currentUserIdForAudit = tenantContext.CurrentUserId;
                if (currentUserIdForAudit.HasValue)
                {
                    var auditTrail = new AuditTrail
                    {
                        TenantId = tenant.Id,
                        OperationType = AuthAuditOperationType.TenantCreated,
                        PerformedByUserId = currentUserIdForAudit.Value,
                        TargetTenantId = tenant.Id,
                        Details = $"Tenant creato con admin: {tenant.Name} (Admin: {adminUser.Username})",
                        WasSuccessful = true,
                        PerformedAt = DateTime.UtcNow
                    };
                    _ = context.AuditTrails.Add(auditTrail);
                    _ = await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante la scrittura dell'audit trail per la creazione tenant con admin.");
            }

            var response = TenantMapper.ToServerResponseDto(tenant);
            response.AdminUser = new TenantAdminResponseDto
            {
                UserId = adminUser.Id,
                Username = adminUser.Username,
                Email = adminUser.Email,
                FirstName = adminUser.FirstName,
                LastName = adminUser.LastName,
                FullName = $"{adminUser.FirstName} {adminUser.LastName}".Trim(),
                MustChangePassword = true,
                GeneratedPassword = randomPassword // Include generated password in response
            };

            logger.LogInformation("Tenant '{TenantName}' creato con successo con admin user '{Username}' (Password: {Password})",
                tenant.Name, adminUser.Username, randomPassword);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Errore durante la creazione del tenant con admin.");
            throw;
        }
    }

    /// <summary>
    /// Generates a random password for new admin users.
    /// </summary>
    /// <returns>Random password string</returns>
    private static string GenerateRandomPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        const string symbols = "!@#$%&*";
        var random = new Random();

        var password = new char[12];

        // Ensure at least one uppercase, one lowercase, one digit, and one symbol
        password[0] = chars[random.Next(0, 25)]; // Uppercase
        password[1] = chars[random.Next(25, 50)]; // Lowercase
        password[2] = chars[random.Next(50, chars.Length)]; // Digit
        password[3] = symbols[random.Next(symbols.Length)]; // Symbol

        // Fill the rest randomly
        for (int i = 4; i < password.Length; i++)
        {
            var useSymbol = random.Next(10) == 0; // 10% chance for symbol
            if (useSymbol)
            {
                password[i] = symbols[random.Next(symbols.Length)];
            }
            else
            {
                password[i] = chars[random.Next(chars.Length)];
            }
        }

        // Shuffle the password to randomize the position of required characters
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    public async Task<TenantResponseDto?> GetTenantAsync(Guid tenantId)
    {
        try
        {
            var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                logger.LogWarning("Accesso negato al tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            return tenant is not null ? TenantMapper.ToServerResponseDto(tenant) : null;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<TenantResponseDto>> GetAllTenantsAsync()
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
                throw new UnauthorizedAccessException("Only super administrators can view all tenants.");

            var tenants = await context.Tenants
                .AsNoTracking()
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return TenantMapper.ToServerResponseDtoCollection(tenants);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<TenantResponseDto> UpdateTenantAsync(Guid tenantId, UpdateTenantDto updateDto)
    {
        try
        {
            var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                logger.LogWarning("Accesso negato all'aggiornamento del tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                logger.LogWarning("Tenant {TenantId} non trovato per aggiornamento.", tenantId);
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
                IsActive = tenant.IsActive,
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
            tenant.SubscriptionExpiresAt = updateDto.SubscriptionExpiresAt;
            tenant.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Tenant {TenantId}.", tenantId);
                throw new InvalidOperationException("Il tenant è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log
            try
            {
                var currentUserId = tenantContext.CurrentUserId;
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
                    _ = context.AuditTrails.Add(auditTrail);
                    _ = await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante la scrittura dell'audit trail per l'aggiornamento tenant.");
            }

            return TenantMapper.ToServerResponseDto(tenant);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task SetTenantStatusAsync(Guid tenantId, bool isEnabled, string reason)
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
            {
                logger.LogWarning("Tentativo di cambio stato tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can change tenant status.");
            }

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                logger.LogWarning("Tenant {TenantId} non trovato per cambio stato.", tenantId);
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
                IsActive = tenant.IsActive,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                CreatedAt = tenant.CreatedAt,
                CreatedBy = tenant.CreatedBy,
                ModifiedAt = tenant.ModifiedAt,
                ModifiedBy = tenant.ModifiedBy
            };

            tenant.IsActive = isEnabled;
            tenant.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync();

            // Audit log
            var currentUserId = tenantContext.CurrentUserId;
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
                _ = context.AuditTrails.Add(auditTrail);
                _ = await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<AdminTenantResponseDto> AddTenantAdminAsync(Guid tenantId, Guid userId, AdminAccessLevel accessLevel)
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
            {
                logger.LogWarning("Tentativo di aggiunta admin tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
            }

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                logger.LogWarning("Tenant {TenantId} non trovato per aggiunta admin.", tenantId);
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                logger.LogWarning("Utente {UserId} non trovato per aggiunta admin.", userId);
                throw new ArgumentException($"User {userId} not found.");
            }

            var existingMapping = await context.AdminTenants
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
            if (existingMapping is not null)
            {
                logger.LogWarning("Utente {UserId} gi� admin per tenant {TenantId}.", userId, tenantId);
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

            _ = context.AdminTenants.Add(adminTenant);
            _ = await context.SaveChangesAsync();

            // Audit log
            var currentUserId = tenantContext.CurrentUserId;
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
                _ = context.AuditTrails.Add(auditTrail);
                _ = await context.SaveChangesAsync();
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
            throw;
        }
    }

    public async Task RemoveTenantAdminAsync(Guid tenantId, Guid userId)
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
            {
                logger.LogWarning("Tentativo di rimozione admin tenant non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can manage tenant admins.");
            }

            var adminTenant = await context.AdminTenants
                .Include(at => at.User)
                .FirstOrDefaultAsync(at => at.UserId == userId && at.ManagedTenantId == tenantId);
            if (adminTenant is null)
            {
                logger.LogWarning("Admin mapping non trovato per utente {UserId} e tenant {TenantId}.", userId, tenantId);
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

            _ = context.AdminTenants.Remove(adminTenant);
            _ = await context.SaveChangesAsync();

            // Audit log
            var currentUserId = tenantContext.CurrentUserId;
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
                _ = context.AuditTrails.Add(auditTrail);
                _ = await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<AdminTenantResponseDto>> GetTenantAdminsAsync(Guid tenantId)
    {
        try
        {
            var canAccess = await tenantContext.CanAccessTenantAsync(tenantId);
            if (!canAccess)
            {
                logger.LogWarning("Accesso negato alla lista admin per tenant {TenantId}.", tenantId);
                throw new UnauthorizedAccessException($"Access denied to tenant {tenantId}.");
            }

            var adminTenants = await context.AdminTenants
                .AsNoTracking()
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
            throw;
        }
    }

    public async Task ForcePasswordChangeAsync(Guid userId)
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
            {
                logger.LogWarning("Tentativo di forzare cambio password non autorizzato.");
                throw new UnauthorizedAccessException("Only super administrators can force password changes.");
            }

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                logger.LogWarning("Utente {UserId} non trovato per forzatura cambio password.", userId);
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

            _ = await context.SaveChangesAsync();

            // Audit log
            var currentUserId = tenantContext.CurrentUserId;
            if (currentUserId.HasValue)
            {
                var auditTrail = new AuditTrail
                {
                    TenantId = user.TenantId, // Now non-nullable
                    OperationType = AuthAuditOperationType.ForcePasswordChange,
                    PerformedByUserId = currentUserId.Value,
                    TargetUserId = userId,
                    Details = $"Password change forced",
                    WasSuccessful = true,
                    PerformedAt = DateTime.UtcNow
                };
                _ = context.AuditTrails.Add(auditTrail);
                _ = await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>> GetAuditTrailAsync(
        Guid? tenantId = null,
        AuditOperationType? operationType = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        try
        {
            if (!tenantContext.IsSuperAdmin)
            {
                logger.LogWarning("Tentativo di accesso all'audit trail senza permessi.");
                throw new UnauthorizedAccessException("Only super administrators can view audit trails.");
            }

            var query = context.AuditTrails
                .AsNoTracking()
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
                .Select(at => new Prym.DTOs.SuperAdmin.AuditTrailResponseDto
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
                    Details = at.Details ?? string.Empty,
                    WasSuccessful = at.WasSuccessful,
                    ErrorMessage = at.ErrorMessage,
                    PerformedAt = at.PerformedAt
                })
                .ToListAsync();

            return new PagedResult<Prym.DTOs.SuperAdmin.AuditTrailResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<TenantStatisticsDto> GetTenantStatisticsAsync()
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant statistics.");
        }

        try
        {
            var totalTenants = await context.Tenants.AsNoTracking().CountAsync();
            var activeTenants = await context.Tenants.AsNoTracking().CountAsync(t => t.IsActive);
            var inactiveTenants = totalTenants - activeTenants;

            var totalUsers = await context.Users.AsNoTracking().CountAsync();
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
            var usersLastMonth = await context.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= oneMonthAgo);

            // Batch load user counts per tenant to avoid correlated subquery N+1
            var userCountsByTenant = await context.Users
                .AsNoTracking()
                .GroupBy(u => u.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

            var activeTenantsForLimit = await context.Tenants
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => new { t.Id, t.MaxUsers })
                .ToListAsync();

            var tenantsNearLimit = activeTenantsForLimit
                .Count(t => userCountsByTenant.TryGetValue(t.Id, out var count) && count >= t.MaxUsers * 0.9);

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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<PagedResult<TenantResponseDto>> SearchTenantsAsync(TenantSearchDto searchDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can search tenants.");
        }

        try
        {
            var query = context.Tenants.AsNoTracking().AsQueryable();

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
                query = query.Where(t => t.IsActive == isActive);
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

            // Apply NearUserLimit filter in-memory after fetching to avoid correlated subquery N+1
            IEnumerable<Tenant> filteredTenants = tenants;
            if (searchDto.NearUserLimit.HasValue && searchDto.NearUserLimit.Value)
            {
                var tenantIds = tenants.Select(t => t.Id).ToList();
                var userCountsByTenant = await context.Users
                    .AsNoTracking()
                    .Where(u => tenantIds.Contains(u.TenantId))
                    .GroupBy(u => u.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TenantId, x => x.Count);

                filteredTenants = tenants
                    .Where(t => userCountsByTenant.TryGetValue(t.Id, out var count) && count >= t.MaxUsers * 0.9);
            }

            var tenantDtos = filteredTenants.Select(t => new TenantResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                DisplayName = t.DisplayName,
                Description = t.Description,
                Domain = t.Domain,
                ContactEmail = t.ContactEmail,
                MaxUsers = t.MaxUsers,
                IsActive = t.IsActive,
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<TenantDetailDto?> GetTenantDetailsAsync(Guid tenantId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant details.");
        }

        try
        {
            var tenant = await context.Tenants.FindAsync(tenantId);
            if (tenant is null)
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
                IsActive = tenant.IsActive,
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
                    FullName = a.FullName ?? string.Empty,
                    MustChangePassword = false, // Default value since not available in AdminTenantResponseDto
                    GeneratedPassword = null // Only for creation
                }).ToList(),
                Limits = limits ?? new TenantLimitsDto { TenantId = tenantId },
                UsageStats = usageStats,
                RecentActivities = recentActivities
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid tenantId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant limits.");
        }

        try
        {
            var tenant = await context.Tenants.FindAsync(tenantId);
            if (tenant is null)
            {
                return null;
            }

            var currentUsers = await context.Users.CountAsync(u => u.TenantId == tenantId);
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
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid tenantId, UpdateTenantLimitsDto updateDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can update tenant limits.");
        }

        try
        {
            var tenant = await context.Tenants.FindAsync(tenantId);
            if (tenant is null)
            {
                throw new ArgumentException($"Tenant {tenantId} not found.");
            }

            tenant.MaxUsers = updateDto.MaxUsers;
            tenant.ModifiedAt = DateTime.UtcNow;
            tenant.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

            _ = await context.SaveChangesAsync();

            // Create audit trail entry
            var auditTrail = new AuditTrail
            {
                PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
                OperationType = AuthAuditOperationType.TenantStatusChanged,
                TargetTenantId = tenantId,
                Details = $"Tenant limits updated: MaxUsers={updateDto.MaxUsers}, MaxStorage={updateDto.MaxStorageBytes}, MaxEvents={updateDto.MaxEventsPerMonth}. Reason: {updateDto.Reason}",
                WasSuccessful = true,
                PerformedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenant.ModifiedBy
            };

            _ = context.AuditTrails.Add(auditTrail);
            _ = await context.SaveChangesAsync();

            return await GetTenantLimitsAsync(tenantId) ?? throw new InvalidOperationException("Failed to retrieve updated limits.");
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<TenantUsageStatsDto> GetTenantUsageStatsAsync(Guid tenantId)
    {
        var totalUsers = await context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId);
        var activeUsers = await context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.IsActive);
        var totalEvents = await context.Events.AsNoTracking().CountAsync(e => e.TenantId == tenantId);
        var eventsThisMonth = await CalculateEventsThisMonthAsync(tenantId);
        var storageUsed = await CalculateStorageUsageAsync(tenantId);
        var lastActivity = await context.AuditTrails
            .AsNoTracking()
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Select(a => a.PerformedAt)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;
        var loginAttemptsToday = await context.AuditTrails
            .AsNoTracking()
            .CountAsync(a => a.OperationType == AuthAuditOperationType.TenantSwitch &&
                           a.PerformedAt >= today &&
                           a.SourceTenantId == tenantId);

        var failedLoginsToday = await context.AuditTrails
            .AsNoTracking()
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
        var recentAudits = await context.AuditTrails
            .AsNoTracking()
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Take(10)
            .Select(a => $"{a.PerformedAt:yyyy-MM-dd HH:mm} - {a.OperationType}: {a.Details}")
            .ToListAsync();

        return recentAudits;
    }

    private async Task<long> CalculateStorageUsageAsync(Guid tenantId)
    {
        // Sum actual file sizes stored in the three file-bearing entities for this tenant.
        var attachmentBytes = await context.DocumentAttachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .SumAsync(a => (long)a.FileSizeBytes);

        var chatAttachmentBytes = await context.MessageAttachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .SumAsync(a => a.FileSize);

        var documentReferenceBytes = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .SumAsync(d => (long)d.FileSizeBytes);

        return attachmentBytes + chatAttachmentBytes + documentReferenceBytes;
    }

    private async Task<int> CalculateEventsThisMonthAsync(Guid tenantId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await context.Events
            .AsNoTracking()
            .CountAsync(e => e.TenantId == tenantId && e.CreatedAt >= startOfMonth);
    }

    public async Task SoftDeleteTenantAsync(Guid tenantId, string reason)
    {
        if (!tenantContext.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only super administrators can delete tenants.");

        try
        {
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
                throw new ArgumentException($"Tenant {tenantId} not found.");

            if (tenant.IsDeleted)
                throw new InvalidOperationException("Tenant is already deleted.");

            tenant.IsDeleted = true;
            tenant.IsActive = false;
            tenant.ModifiedAt = DateTime.UtcNow;

            _ = await context.SaveChangesAsync();

            // Audit log
            var currentUserId = tenantContext.CurrentUserId;
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
                _ = context.AuditTrails.Add(auditTrail);
                _ = await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all tenants with pagination (SuperAdmin only).
    /// </summary>
    public async Task<PagedResult<TenantResponseDto>> GetTenantsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            // NOTE: No tenant filter - SuperAdmin sees all tenants
            var query = context.Tenants
                .AsNoTracking()
                .Where(t => !t.IsDeleted);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(t => new TenantResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    Domain = t.Domain,
                    ContactEmail = t.ContactEmail,
                    MaxUsers = t.MaxUsers,
                    IsActive = t.IsActive,
                    SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.ModifiedAt ?? t.CreatedAt,
                    CreatedBy = t.CreatedBy,
                    ModifiedAt = t.ModifiedAt,
                    ModifiedBy = t.ModifiedBy
                })
                .ToListAsync(ct);

            return new PagedResult<TenantResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all active tenants with pagination (SuperAdmin only).
    /// </summary>
    public async Task<PagedResult<TenantResponseDto>> GetActiveTenantsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            var query = context.Tenants
                .AsNoTracking()
                .Where(t => !t.IsDeleted && t.IsActive);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(t => new TenantResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    Domain = t.Domain,
                    ContactEmail = t.ContactEmail,
                    MaxUsers = t.MaxUsers,
                    IsActive = t.IsActive,
                    SubscriptionExpiresAt = t.SubscriptionExpiresAt,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.ModifiedAt ?? t.CreatedAt,
                    CreatedBy = t.CreatedBy,
                    ModifiedAt = t.ModifiedAt,
                    ModifiedBy = t.ModifiedBy
                })
                .ToListAsync(ct);

            return new PagedResult<TenantResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
