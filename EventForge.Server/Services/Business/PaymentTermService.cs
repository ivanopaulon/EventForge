using EventForge.DTOs.Business;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service implementation for managing payment terms.
/// </summary>
public class PaymentTermService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<PaymentTermService> logger) : IPaymentTermService
{

    public async Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment term operations.");
            }

            var query = context.PaymentTerms
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var paymentTerms = await query
                .OrderBy(pt => pt.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var paymentTermDtos = paymentTerms.Select(MapToPaymentTermDto);

            return new PagedResult<PaymentTermDto>
            {
                Items = paymentTermDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment terms.");
            throw;
        }
    }

    public async Task<PaymentTermDto?> GetPaymentTermByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment term operations.");
            }

            var paymentTerm = await context.PaymentTerms
                .AsNoTracking()
                .Where(pt => pt.Id == id && pt.TenantId == currentTenantId.Value && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentTerm is not null ? MapToPaymentTermDto(paymentTerm) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving payment term {PaymentTermId}.", id);
            throw;
        }
    }

    public async Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createPaymentTermDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPaymentTermDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment term operations.");
            }

            var paymentTerm = new PaymentTerm
            {
                TenantId = currentTenantId.Value,
                Name = createPaymentTermDto.Name,
                Description = createPaymentTermDto.Description,
                DueDays = createPaymentTermDto.DueDays,
                PaymentMethod = (EventForge.Server.Data.Entities.Business.PaymentMethod)createPaymentTermDto.PaymentMethod,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow
            };

            _ = context.PaymentTerms.Add(paymentTerm);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Audit log for the created payment term
            _ = await auditLogService.TrackEntityChangesAsync(paymentTerm, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Payment term created with ID {PaymentTermId} by user {User}.", paymentTerm.Id, currentUser);

            return MapToPaymentTermDto(paymentTerm);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment term for user {User}.", currentUser);
            throw;
        }
    }

    public async Task<PaymentTermDto?> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updatePaymentTermDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePaymentTermDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPaymentTerm = await context.PaymentTerms
                .AsNoTracking()
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPaymentTerm is null)
            {
                logger.LogWarning("Payment term with ID {PaymentTermId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var paymentTerm = await context.PaymentTerms
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentTerm is null)
            {
                logger.LogWarning("Payment term with ID {PaymentTermId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Update properties
            paymentTerm.Name = updatePaymentTermDto.Name;
            paymentTerm.Description = updatePaymentTermDto.Description;
            paymentTerm.DueDays = updatePaymentTermDto.DueDays;
            paymentTerm.PaymentMethod = (EventForge.Server.Data.Entities.Business.PaymentMethod)updatePaymentTermDto.PaymentMethod;
            paymentTerm.ModifiedBy = currentUser;
            paymentTerm.ModifiedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating PaymentTerm {PaymentTermId}.", id);
                throw new InvalidOperationException("Il termine di pagamento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log for the updated payment term
            _ = await auditLogService.TrackEntityChangesAsync(paymentTerm, "Update", currentUser, originalPaymentTerm, cancellationToken);

            logger.LogInformation("Payment term {PaymentTermId} updated by user {User}.", id, currentUser);

            return MapToPaymentTermDto(paymentTerm);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating payment term {PaymentTermId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> DeletePaymentTermAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPaymentTerm = await context.PaymentTerms
                .AsNoTracking()
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPaymentTerm is null)
            {
                logger.LogWarning("Payment term with ID {PaymentTermId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var paymentTerm = await context.PaymentTerms
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentTerm is null)
            {
                logger.LogWarning("Payment term with ID {PaymentTermId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Soft delete the payment term
            paymentTerm.IsDeleted = true;
            paymentTerm.DeletedBy = currentUser;
            paymentTerm.DeletedAt = DateTime.UtcNow;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting PaymentTerm {PaymentTermId}.", id);
                throw new InvalidOperationException("Il termine di pagamento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Audit log for the deleted payment term
            _ = await auditLogService.TrackEntityChangesAsync(paymentTerm, "Delete", currentUser, originalPaymentTerm, cancellationToken);

            logger.LogInformation("Payment term {PaymentTermId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting payment term {PaymentTermId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> PaymentTermExistsAsync(Guid paymentTermId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.PaymentTerms
                .AnyAsync(pt => pt.Id == paymentTermId && !pt.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if payment term {PaymentTermId} exists.", paymentTermId);
            throw;
        }
    }

    // Private mapping method

    private static PaymentTermDto MapToPaymentTermDto(PaymentTerm paymentTerm)
    {
        return new PaymentTermDto
        {
            Id = paymentTerm.Id,
            Name = paymentTerm.Name,
            Description = paymentTerm.Description,
            DueDays = paymentTerm.DueDays,
            PaymentMethod = (EventForge.DTOs.Common.PaymentMethod)paymentTerm.PaymentMethod,
            CreatedAt = paymentTerm.CreatedAt,
            CreatedBy = paymentTerm.CreatedBy,
            ModifiedAt = paymentTerm.ModifiedAt,
            ModifiedBy = paymentTerm.ModifiedBy
        };
    }

}
