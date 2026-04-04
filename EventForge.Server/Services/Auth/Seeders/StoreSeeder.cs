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
            var existingCount = await _dbContext.PaymentMethods
                .CountAsync(p => p.TenantId == tenantId, cancellationToken);

            if (existingCount > 0)
                return true;

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
            var existingCount = await _dbContext.StorePoses
                .CountAsync(p => p.TenantId == tenantId, cancellationToken);

            if (existingCount > 0)
                return true;

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

            _dbContext.StorePoses.Add(defaultPos);
            await _dbContext.SaveChangesAsync(cancellationToken);
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
            var existingOperator = await _dbContext.StoreUsers
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Username == "operator1", cancellationToken);

            if (existingOperator != null)
                return true;

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

            _dbContext.StoreUsers.Add(defaultOperator);
            await _dbContext.SaveChangesAsync(cancellationToken);
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
            var success = true;

            if (!await SeedPaymentMethodsAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed payment methods for tenant {TenantId}", tenantId);
                success = false;
            }

            if (!await SeedDefaultPosAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed default POS for tenant {TenantId}", tenantId);
                success = false;
            }

            if (!await SeedDefaultOperatorAsync(tenantId, cancellationToken))
            {
                _logger.LogWarning("Failed to seed default operator for tenant {TenantId}", tenantId);
                success = false;
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
