using Microsoft.EntityFrameworkCore;
using SalesPaymentMethod = EventForge.Server.Data.Entities.Sales.PaymentMethod;

namespace EventForge.Server.Services.Auth.Seeders;

/// <summary>
/// Implementation of store seeding service.
/// Seeds default payment methods, POS terminals, and store operators for tenants.
/// </summary>
public class StoreSeeder : IStoreSeeder
{
    private readonly EventForgeDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StoreSeeder> _logger;

    public StoreSeeder(
        EventForgeDbContext dbContext,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<StoreSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SeedPaymentMethodsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if payment methods already exist for this tenant
            var existingCount = await _dbContext.PaymentMethods
                .CountAsync(p => p.TenantId == tenantId, cancellationToken);

            if (existingCount > 0)
            {
                _logger.LogInformation("Payment methods already exist for tenant {TenantId} (Count: {Count}). Skipping seeding.",
                    tenantId, existingCount);
                return true;
            }

            _logger.LogInformation("Seeding default payment methods for tenant {TenantId}...", tenantId);

            var paymentMethods = new[]
            {
                new SalesPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Code = "CASH",
                    Name = "Contanti",
                    Description = "Pagamento in contanti",
                    IsActive = true,
                    RequiresIntegration = false,
                    AllowsChange = true,
                    DisplayOrder = 1,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new SalesPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Code = "CC",
                    Name = "Carta di Credito",
                    Description = "Pagamento con carta di credito",
                    IsActive = true,
                    RequiresIntegration = false,
                    AllowsChange = false,
                    DisplayOrder = 2,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new SalesPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Code = "DEBIT",
                    Name = "Bancomat",
                    Description = "Pagamento con carta di debito",
                    IsActive = true,
                    RequiresIntegration = false,
                    AllowsChange = false,
                    DisplayOrder = 3,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new SalesPaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Code = "CHECK",
                    Name = "Assegno",
                    Description = "Pagamento con assegno",
                    IsActive = true,
                    RequiresIntegration = false,
                    AllowsChange = false,
                    DisplayOrder = 4,
                    TenantId = tenantId,
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _dbContext.PaymentMethods.AddRangeAsync(paymentMethods, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded {Count} payment methods for tenant {TenantId}",
                paymentMethods.Length, tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding payment methods for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> SeedDefaultPosAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if POS already exists for this tenant
            var existingCount = await _dbContext.StorePoses
                .CountAsync(p => p.TenantId == tenantId, cancellationToken);

            if (existingCount > 0)
            {
                _logger.LogInformation("POS terminals already exist for tenant {TenantId} (Count: {Count}). Skipping seeding.",
                    tenantId, existingCount);
                return true;
            }

            _logger.LogInformation("Seeding default POS terminal for tenant {TenantId}...", tenantId);

            var defaultPos = new EventForge.Server.Data.Entities.Store.StorePos
            {
                Id = Guid.NewGuid(),
                Name = "Cassa Principale",
                Description = "Punto cassa principale",
                Status = EventForge.Server.Data.Entities.Store.CashRegisterStatus.Active,
                Location = "Front Desk",
                TerminalIdentifier = "POS001",
                IsOnline = false,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.StorePoses.AddAsync(defaultPos, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded default POS terminal for tenant {TenantId}", tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default POS for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> SeedDefaultOperatorAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if operator already exists for this tenant
            var existingOperator = await _dbContext.StoreUsers
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "operator1", cancellationToken);

            if (existingOperator != null)
            {
                _logger.LogInformation("Store operator 'operator1' already exists for tenant {TenantId}. Skipping seeding.", tenantId);
                return true;
            }

            _logger.LogInformation("Seeding default store operator for tenant {TenantId}...", tenantId);

            // Get operator password from configuration, environment variable, or use default
            var operatorPassword = Environment.GetEnvironmentVariable("EVENTFORGE_STORE_OPERATOR_PASSWORD")
                ?? _configuration["Bootstrap:StoreOperatorPassword"]
                ?? "Operator@2025!";

            // Hash the password
            var (passwordHash, passwordSalt) = _passwordService.HashPassword(operatorPassword);

            var defaultOperator = new EventForge.Server.Data.Entities.Store.StoreUser
            {
                Id = Guid.NewGuid(),
                Name = "Operatore Cassa",
                Username = "operator1",
                Email = "operator1@localhost",
                // Store hash and salt using a delimiter that won't appear in base64 strings
                // Format: hash|salt (using pipe as separator)
                PasswordHash = $"{passwordHash}|{passwordSalt}",
                Role = "Cashier",
                Status = EventForge.Server.Data.Entities.Store.CashierStatus.Active,
                IsOnShift = false,
                TwoFactorEnabled = false,
                PhotoConsent = false,
                TenantId = tenantId,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.StoreUsers.AddAsync(defaultOperator, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully seeded default store operator for tenant {TenantId}", tenantId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default operator for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<bool> SeedStoreBaseEntitiesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting store base entities seeding for tenant {TenantId}...", tenantId);

            var success = true;

            // Seed payment methods
            if (!await SeedPaymentMethodsAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed payment methods for tenant {TenantId}", tenantId);
                success = false;
            }

            // Seed default POS
            if (!await SeedDefaultPosAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed default POS for tenant {TenantId}", tenantId);
                success = false;
            }

            // Seed default operator
            if (!await SeedDefaultOperatorAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed default operator for tenant {TenantId}", tenantId);
                success = false;
            }

            if (success)
            {
                _logger.LogInformation("Successfully seeded all store base entities for tenant {TenantId}", tenantId);
            }
            else
            {
                _logger.LogWarning("Store base entities seeding completed with some failures for tenant {TenantId}", tenantId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding store base entities for tenant {TenantId}", tenantId);
            return false;
        }
    }
}
