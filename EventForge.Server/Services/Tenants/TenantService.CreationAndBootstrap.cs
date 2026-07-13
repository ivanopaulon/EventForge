using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
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

}
