using EventForge.DTOs.Sales;
using EventForge.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service implementation for managing payment methods.
/// </summary>
public class PaymentMethodService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<PaymentMethodService> logger,
    ICacheService cacheService) : IPaymentMethodService
{

    private const string CACHE_KEY_ALL = "PaymentMethods_All";

    public async Task<PagedResult<PaymentMethodDto>> GetPaymentMethodsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            logger.LogDebug("Querying payment methods for tenant {TenantId}", currentTenantId.Value);

            // Cache all PaymentMethods for 15 minutes
            var allPaymentMethods = await cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                currentTenantId.Value,
                async (ct) =>
                {
                    return await context.PaymentMethods
                        .AsNoTracking()
                        .Where(pm => pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                        .OrderBy(pm => pm.DisplayOrder)
                        .ThenBy(pm => pm.Name)
                        .Select(pm => MapToDto(pm))
                        .ToListAsync(ct);
                },
                absoluteExpiration: TimeSpan.FromMinutes(15),
                ct: cancellationToken
            );

            // Paginate in memory (PaymentMethods are typically few - usually < 20 per tenant)
            // Note: If a tenant has a very large number of payment methods, consider per-page caching
            var totalCount = allPaymentMethods.Count;
            var items = allPaymentMethods
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToList();

            logger.LogDebug("Found {Count} payment methods for tenant {TenantId}", totalCount, currentTenantId.Value);

            return new PagedResult<PaymentMethodDto>
            {
                Items = items,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment methods for tenant {TenantId}", tenantContext.CurrentTenantId);
            throw;
        }
    }

    public async Task<PagedResult<PaymentMethodDto>> GetActivePaymentMethodsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var query = context.PaymentMethods
                .AsNoTracking()
                .Where(pm => pm.TenantId == currentTenantId.Value && pm.IsActive && !pm.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);

            var paymentMethods = await query
                .OrderBy(pm => pm.DisplayOrder)
                .ThenBy(pm => pm.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<PaymentMethodDto>
            {
                Items = paymentMethods.Select(MapToDto),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active payment methods.");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentMethod is not null ? MapToDto(paymentMethod) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<PaymentMethodDto?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await context.PaymentMethods
                .Where(pm => pm.Code == code && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentMethod is not null ? MapToDto(paymentMethod) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment method by code {Code}.", code);
            throw;
        }
    }

    public async Task<PaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
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
                FiscalCode = createDto.FiscalCode,
                TenantId = currentTenantId.Value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.PaymentMethods.Add(paymentMethod);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "All",
                operationType: "Create",
                oldValue: null,
                newValue: $"Created payment method: {paymentMethod.Name}",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            logger.LogInformation("Payment method {Name} created by {User}.", paymentMethod.Name, currentUser);

            return MapToDto(paymentMethod);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment method.");
            throw;
        }
    }

    public async Task<PaymentMethodDto?> UpdatePaymentMethodAsync(Guid id, UpdatePaymentMethodDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentMethod is null)
            {
                return null;
            }

            // Update properties
            paymentMethod.Name = updateDto.Name;
            paymentMethod.Description = updateDto.Description;
            paymentMethod.Icon = updateDto.Icon;
            paymentMethod.IsActive = updateDto.IsActive;
            paymentMethod.DisplayOrder = updateDto.DisplayOrder;
            paymentMethod.RequiresIntegration = updateDto.RequiresIntegration;
            paymentMethod.IntegrationConfig = updateDto.IntegrationConfig;
            paymentMethod.AllowsChange = updateDto.AllowsChange;
            paymentMethod.FiscalCode = updateDto.FiscalCode;
            paymentMethod.ModifiedAt = DateTime.UtcNow;
            paymentMethod.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating PaymentMethod {PaymentMethodId}.", id);
                throw new InvalidOperationException("Il metodo di pagamento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "All",
                operationType: "Update",
                oldValue: null,
                newValue: $"Updated payment method: {paymentMethod.Name}",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            logger.LogInformation("Payment method {Name} updated by {User}.", paymentMethod.Name, currentUser);

            return MapToDto(paymentMethod);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var paymentMethod = await context.PaymentMethods
                .Where(pm => pm.Id == id && pm.TenantId == currentTenantId.Value && !pm.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentMethod is null)
            {
                return false;
            }

            // Soft delete
            paymentMethod.IsDeleted = true;
            paymentMethod.DeletedAt = DateTime.UtcNow;
            paymentMethod.DeletedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting PaymentMethod {PaymentMethodId}.", id);
                throw new InvalidOperationException("Il metodo di pagamento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.LogEntityChangeAsync(
                entityName: "PaymentMethod",
                entityId: paymentMethod.Id,
                propertyName: "IsDeleted",
                operationType: "Delete",
                oldValue: "false",
                newValue: "true",
                changedBy: currentUser,
                entityDisplayName: paymentMethod.Name,
                cancellationToken: cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, currentTenantId.Value);

            logger.LogInformation("Payment method {Name} deleted by {User}.", paymentMethod.Name, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting payment method {PaymentMethodId}.", id);
            throw;
        }
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment method operations.");
            }

            var query = context.PaymentMethods
                .Where(pm => pm.Code == code && pm.TenantId == currentTenantId.Value && !pm.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(pm => pm.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if payment method code exists.");
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
            AllowsChange = entity.AllowsChange,
            FiscalCode = entity.FiscalCode
        };
    }

}
