using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service implementation for managing payment terms.
/// </summary>
public class PaymentTermService : IPaymentTermService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PaymentTermService> _logger;

    public PaymentTermService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<PaymentTermService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<PaymentTermDto>> GetPaymentTermsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment term operations.");
            }

            var query = _context.PaymentTerms
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
            _logger.LogError(ex, "Error retrieving payment terms.");
            throw;
        }
    }

    public async Task<PaymentTermDto?> GetPaymentTermByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for payment term operations.");
            }

            var paymentTerm = await _context.PaymentTerms
                .Where(pt => pt.Id == id && pt.TenantId == currentTenantId.Value && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return paymentTerm != null ? MapToPaymentTermDto(paymentTerm) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment term {PaymentTermId}.", id);
            throw;
        }
    }

    public async Task<PaymentTermDto> CreatePaymentTermAsync(CreatePaymentTermDto createPaymentTermDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createPaymentTermDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = _tenantContext.CurrentTenantId;
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

            _ = _context.PaymentTerms.Add(paymentTerm);
            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the created payment term
            _ = await _auditLogService.TrackEntityChangesAsync(paymentTerm, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Payment term created with ID {PaymentTermId} by user {User}.", paymentTerm.Id, currentUser);

            return MapToPaymentTermDto(paymentTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment term for user {User}.", currentUser);
            throw;
        }
    }

    public async Task<PaymentTermDto?> UpdatePaymentTermAsync(Guid id, UpdatePaymentTermDto updatePaymentTermDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updatePaymentTermDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPaymentTerm = await _context.PaymentTerms
                .AsNoTracking()
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPaymentTerm == null)
            {
                _logger.LogWarning("Payment term with ID {PaymentTermId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var paymentTerm = await _context.PaymentTerms
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentTerm == null)
            {
                _logger.LogWarning("Payment term with ID {PaymentTermId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Update properties
            paymentTerm.Name = updatePaymentTermDto.Name;
            paymentTerm.Description = updatePaymentTermDto.Description;
            paymentTerm.DueDays = updatePaymentTermDto.DueDays;
            paymentTerm.PaymentMethod = (EventForge.Server.Data.Entities.Business.PaymentMethod)updatePaymentTermDto.PaymentMethod;
            paymentTerm.ModifiedBy = currentUser;
            paymentTerm.ModifiedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the updated payment term
            _ = await _auditLogService.TrackEntityChangesAsync(paymentTerm, "Update", currentUser, originalPaymentTerm, cancellationToken);

            _logger.LogInformation("Payment term {PaymentTermId} updated by user {User}.", id, currentUser);

            return MapToPaymentTermDto(paymentTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment term {PaymentTermId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> DeletePaymentTermAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalPaymentTerm = await _context.PaymentTerms
                .AsNoTracking()
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalPaymentTerm == null)
            {
                _logger.LogWarning("Payment term with ID {PaymentTermId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var paymentTerm = await _context.PaymentTerms
                .Where(pt => pt.Id == id && !pt.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentTerm == null)
            {
                _logger.LogWarning("Payment term with ID {PaymentTermId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Soft delete the payment term
            paymentTerm.IsDeleted = true;
            paymentTerm.DeletedBy = currentUser;
            paymentTerm.DeletedAt = DateTime.UtcNow;

            _ = await _context.SaveChangesAsync(cancellationToken);

            // Audit log for the deleted payment term
            _ = await _auditLogService.TrackEntityChangesAsync(paymentTerm, "Delete", currentUser, originalPaymentTerm, cancellationToken);

            _logger.LogInformation("Payment term {PaymentTermId} deleted by user {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment term {PaymentTermId} for user {User}.", id, currentUser);
            throw;
        }
    }

    public async Task<bool> PaymentTermExistsAsync(Guid paymentTermId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.PaymentTerms
                .AnyAsync(pt => pt.Id == paymentTermId && !pt.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if payment term {PaymentTermId} exists.", paymentTermId);
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