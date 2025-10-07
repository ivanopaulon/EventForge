using EventForge.DTOs.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing payment methods.
/// </summary>
public class PaymentMethodService : IPaymentMethodService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PaymentMethodService> _logger;

    public PaymentMethodService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<PaymentMethodService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PaymentMethodDto>> GetPaymentMethodsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var query = _context.PaymentMethods
                .Where(pm => pm.TenantId == currentTenantId.Value && !pm.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var paymentMethods = await query
                .OrderBy(pm => pm.DisplayOrder)
                .ThenBy(pm => pm.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = paymentMethods.Select(MapToDto);

            return new PagedResult<PaymentMethodDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods.");
            throw;
        }
    }

    public async Task<List<PaymentMethodDto>> GetActivePaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethods = await _context.PaymentMethods
                .Where(pm => pm.TenantId == currentTenantId.Value && pm.IsActive && !pm.IsDeleted)
                .OrderBy(pm => pm.DisplayOrder)
                .ThenBy(pm => pm.Name)
                .ToListAsync(cancellationToken);

            return paymentMethods.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active payment methods.");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await _context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentMethod != null ? MapToDto(paymentMethod) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await _context.PaymentMethods
                .Where(pm => pm.Code == code && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentMethod != null ? MapToDto(paymentMethod) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method by code {Code}.", code);
            throw;
        }
    }

    public async Task<PaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            // Check for duplicate code
            if (await CodeExistsAsync(createDto.Code, null, cancellationToken))
            {
                throw new InvalidOperationException($"Payment method with code '{createDto.Code}' already exists.");
            }

            var paymentMethod = new Data.Entities.Sales.PaymentMethod
            {
                Id = Guid.NewGuid(),
                Code = createDto.Code,
                Name = createDto.Name,
                Description = createDto.Description,
                Icon = createDto.Icon,
                IsActive = createDto.IsActive,
                DisplayOrder = createDto.DisplayOrder,
                RequiresIntegration = createDto.RequiresIntegration,
                IntegrationConfig = createDto.IntegrationConfig,
                AllowsChange = createDto.AllowsChange,
                TenantId = currentTenantId.Value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = _context.PaymentMethods.Add(paymentMethod);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "All",
                operationType: "Create",
                oldValue: null,
                newValue: $"Created payment method: {paymentMethod.Name}",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Payment method {Name} created by {User}.", paymentMethod.Name, currentUser);

            return MapToDto(paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method.");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> UpdatePaymentMethodAsync(Guid id, UpdatePaymentMethodDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await _context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentMethod == null)
            {
                return null;
            }

            // Update properties
            paymentMethod.Name = updateDto.Name;
            paymentMethod.Description = updateDto.Description;
            paymentMethod.Icon = updateDto.Icon;
            paymentMethod.IsActive = updateDto.IsActive;
            paymentMethod.DisplayOrder = updateDto.DisplayOrder;
            paymentMethod.IntegrationConfig = updateDto.IntegrationConfig;
            paymentMethod.AllowsChange = updateDto.AllowsChange;
            paymentMethod.ModifiedAt = DateTime.UtcNow;
            paymentMethod.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "All",
                operationType: "Update",
                oldValue: null,
                newValue: $"Updated payment method: {paymentMethod.Name}",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Payment method {Name} updated by {User}.", paymentMethod.Name, currentUser);

            return MapToDto(paymentMethod);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await _context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentMethod == null)
            {
                return false;
            }

            // Soft delete
            paymentMethod.IsDeleted = true;
            paymentMethod.DeletedAt = DateTime.UtcNow;
            paymentMethod.DeletedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "IsDeleted",
                operationType: "Delete",
                oldValue: "false",
                newValue: "true",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Payment method {Name} deleted by {User}.", paymentMethod.Name, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var query = _context.PaymentMethods
                .Where(pm => pm.Code == code && pm.TenantId == currentTenantId.Value && !pm.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(pm => pm.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if payment method code exists.");
            throw;
        }
    }

    private static PaymentMethodDto MapToDto(Data.Entities.Sales.PaymentMethod entity)
    {
        return new PaymentMethodDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            Icon = entity.Icon,
            IsActive = entity.IsActive,
            DisplayOrder = entity.DisplayOrder,
            RequiresIntegration = entity.RequiresIntegration,
            AllowsChange = entity.AllowsChange
        };
    }
}
