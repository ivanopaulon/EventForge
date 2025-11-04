using EventForge.DTOs.Business;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service implementation for managing business parties and their accounting data.
/// </summary>
public class BusinessPartyService : IBusinessPartyService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BusinessPartyService> _logger;

    public BusinessPartyService(EventForgeDbContext context, IAuditLogService auditLogService, ITenantContext tenantContext, ILogger<BusinessPartyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region BusinessParty Operations

    public async Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in business party queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var query = _context.BusinessParties
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var businessParties = await query
                .OrderBy(bp => bp.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var businessPartyDtos = new List<BusinessPartyDto>();
            foreach (var businessParty in businessParties)
            {
                var addressCount = await _context.Addresses
                    .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == businessParty.Id && !a.IsDeleted && a.TenantId == currentTenantId.Value, cancellationToken);
                var contactCount = await _context.Contacts
                    .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == businessParty.Id && !c.IsDeleted && c.TenantId == currentTenantId.Value, cancellationToken);
                var referenceCount = await _context.References
                    .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == businessParty.Id && !r.IsDeleted && r.TenantId == currentTenantId.Value, cancellationToken);
                var hasAccountingData = await _context.BusinessPartyAccountings
                    .AnyAsync(bpa => bpa.BusinessPartyId == businessParty.Id && !bpa.IsDeleted && bpa.TenantId == currentTenantId.Value, cancellationToken);

                businessPartyDtos.Add(MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData));
            }

            return new PagedResult<BusinessPartyDto>
            {
                Items = businessPartyDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business parties");
            throw;
        }
    }

    public async Task<BusinessPartyDto?> GetBusinessPartyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var businessParty = await _context.BusinessParties
                .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessParty == null)
                return null;

            var addressCount = await _context.Addresses
                .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted && a.TenantId == currentTenantId.Value, cancellationToken);
            var contactCount = await _context.Contacts
                .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted && c.TenantId == currentTenantId.Value, cancellationToken);
            var referenceCount = await _context.References
                .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == id && !r.IsDeleted && r.TenantId == currentTenantId.Value, cancellationToken);
            var hasAccountingData = await _context.BusinessPartyAccountings
                .AnyAsync(bpa => bpa.BusinessPartyId == id && !bpa.IsDeleted && bpa.TenantId == currentTenantId.Value, cancellationToken);

            return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party with ID {BusinessPartyId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(DTOs.Common.BusinessPartyType partyType, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var businessParties = await _context.BusinessParties
                .Where(bp => bp.PartyType == (Data.Entities.Business.BusinessPartyType)partyType)
                .WhereActiveTenant(currentTenantId.Value)
                .OrderBy(bp => bp.Name)
                .ToListAsync(cancellationToken);

            var businessPartyDtos = new List<BusinessPartyDto>();
            foreach (var businessParty in businessParties)
            {
                var addressCount = await _context.Addresses
                    .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == businessParty.Id && !a.IsDeleted && a.TenantId == currentTenantId.Value, cancellationToken);
                var contactCount = await _context.Contacts
                    .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == businessParty.Id && !c.IsDeleted && c.TenantId == currentTenantId.Value, cancellationToken);
                var referenceCount = await _context.References
                    .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == businessParty.Id && !r.IsDeleted && r.TenantId == currentTenantId.Value, cancellationToken);
                var hasAccountingData = await _context.BusinessPartyAccountings
                    .AnyAsync(bpa => bpa.BusinessPartyId == businessParty.Id && !bpa.IsDeleted && bpa.TenantId == currentTenantId.Value, cancellationToken);

                businessPartyDtos.Add(MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData));
            }

            return businessPartyDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business parties by type {BusinessPartyType}", partyType);
            throw;
        }
    }

    public async Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var businessParty = new BusinessParty
            {
                TenantId = currentTenantId.Value,
                PartyType = (EventForge.Server.Data.Entities.Business.BusinessPartyType)createBusinessPartyDto.PartyType,
                Name = createBusinessPartyDto.Name,
                TaxCode = createBusinessPartyDto.TaxCode,
                VatNumber = createBusinessPartyDto.VatNumber,
                SdiCode = createBusinessPartyDto.SdiCode,
                Pec = createBusinessPartyDto.Pec,
                Notes = createBusinessPartyDto.Notes,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _ = _context.BusinessParties.Add(businessParty);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessParty, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Business party {BusinessPartyName} created with ID {BusinessPartyId} by {User}",
                businessParty.Name, businessParty.Id, currentUser);

            return MapToBusinessPartyDto(businessParty, 0, 0, 0, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business party");
            throw;
        }
    }

    public async Task<BusinessPartyDto?> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var originalBusinessParty = await _context.BusinessParties
                .AsNoTracking()
                .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBusinessParty == null)
                return null;

            var businessParty = await _context.BusinessParties
                .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessParty == null)
                return null;

            businessParty.PartyType = (EventForge.Server.Data.Entities.Business.BusinessPartyType)updateBusinessPartyDto.PartyType;
            businessParty.Name = updateBusinessPartyDto.Name;
            businessParty.TaxCode = updateBusinessPartyDto.TaxCode;
            businessParty.VatNumber = updateBusinessPartyDto.VatNumber;
            businessParty.SdiCode = updateBusinessPartyDto.SdiCode;
            businessParty.Pec = updateBusinessPartyDto.Pec;
            businessParty.Notes = updateBusinessPartyDto.Notes;
            businessParty.ModifiedAt = DateTime.UtcNow;
            businessParty.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessParty, "Update", currentUser, originalBusinessParty, cancellationToken);

            _logger.LogInformation("Business party {BusinessPartyId} updated by {User}", id, currentUser);

            var addressCount = await _context.Addresses
                .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted, cancellationToken);
            var contactCount = await _context.Contacts
                .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted, cancellationToken);
            var referenceCount = await _context.References
                .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == id && !r.IsDeleted, cancellationToken);
            var hasAccountingData = await _context.BusinessPartyAccountings
                .AnyAsync(bpa => bpa.BusinessPartyId == id && !bpa.IsDeleted, cancellationToken);

            return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business party with ID {BusinessPartyId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteBusinessPartyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var originalBusinessParty = await _context.BusinessParties
                .AsNoTracking()
                .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBusinessParty == null)
                return false;

            var businessParty = await _context.BusinessParties
                .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessParty == null)
                return false;

            businessParty.IsDeleted = true;
            businessParty.DeletedAt = DateTime.UtcNow;
            businessParty.DeletedBy = currentUser;
            businessParty.ModifiedAt = DateTime.UtcNow;
            businessParty.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessParty, "Delete", currentUser, originalBusinessParty, cancellationToken);

            _logger.LogInformation("Business party {BusinessPartyId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business party with ID {BusinessPartyId}", id);
            throw;
        }
    }

    #endregion

    #region BusinessPartyAccounting Operations

    public async Task<PagedResult<BusinessPartyAccountingDto>> GetBusinessPartyAccountingAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.BusinessPartyAccountings
                .Include(bpa => bpa.Bank)
                .Include(bpa => bpa.PaymentTerm)
                .Where(bpa => !bpa.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var businessPartyAccountings = await query
                .OrderBy(bpa => bpa.BusinessPartyId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var businessPartyAccountingDtos = new List<BusinessPartyAccountingDto>();
            foreach (var bpa in businessPartyAccountings)
            {
                var businessPartyName = await _context.BusinessParties
                    .Where(bp => bp.Id == bpa.BusinessPartyId && !bp.IsDeleted)
                    .Select(bp => bp.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                businessPartyAccountingDtos.Add(MapToBusinessPartyAccountingDto(bpa, businessPartyName));
            }

            return new PagedResult<BusinessPartyAccountingDto>
            {
                Items = businessPartyAccountingDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party accounting records");
            throw;
        }
    }

    public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessPartyAccounting = await _context.BusinessPartyAccountings
                .Include(bpa => bpa.Bank)
                .Include(bpa => bpa.PaymentTerm)
                .Where(bpa => bpa.Id == id && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessPartyAccounting == null)
                return null;

            var businessPartyName = await _context.BusinessParties
                .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return MapToBusinessPartyAccountingDto(businessPartyAccounting, businessPartyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party accounting with ID {BusinessPartyAccountingId}", id);
            throw;
        }
    }

    public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessPartyAccounting = await _context.BusinessPartyAccountings
                .Include(bpa => bpa.Bank)
                .Include(bpa => bpa.PaymentTerm)
                .Where(bpa => bpa.BusinessPartyId == businessPartyId && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessPartyAccounting == null)
                return null;

            var businessPartyName = await _context.BusinessParties
                .Where(bp => bp.Id == businessPartyId && !bp.IsDeleted)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return MapToBusinessPartyAccountingDto(businessPartyAccounting, businessPartyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business party accounting for business party {BusinessPartyId}", businessPartyId);
            throw;
        }
    }

    public async Task<BusinessPartyAccountingDto> CreateBusinessPartyAccountingAsync(CreateBusinessPartyAccountingDto createBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var businessPartyAccounting = new BusinessPartyAccounting
            {
                TenantId = currentTenantId.Value,
                BusinessPartyId = createBusinessPartyAccountingDto.BusinessPartyId,
                Iban = createBusinessPartyAccountingDto.Iban,
                BankId = createBusinessPartyAccountingDto.BankId,
                PaymentTermId = createBusinessPartyAccountingDto.PaymentTermId,
                CreditLimit = createBusinessPartyAccountingDto.CreditLimit,
                Notes = createBusinessPartyAccountingDto.Notes,
                CreatedBy = currentUser,
                ModifiedBy = currentUser
            };

            _ = _context.BusinessPartyAccountings.Add(businessPartyAccounting);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Business party accounting created with ID {BusinessPartyAccountingId} by {User}",
                businessPartyAccounting.Id, currentUser);

            // Reload with includes
            var createdBusinessPartyAccounting = await _context.BusinessPartyAccountings
                .Include(bpa => bpa.Bank)
                .Include(bpa => bpa.PaymentTerm)
                .FirstAsync(bpa => bpa.Id == businessPartyAccounting.Id, cancellationToken);

            var businessPartyName = await _context.BusinessParties
                .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return MapToBusinessPartyAccountingDto(createdBusinessPartyAccounting, businessPartyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating business party accounting");
            throw;
        }
    }

    public async Task<BusinessPartyAccountingDto?> UpdateBusinessPartyAccountingAsync(Guid id, UpdateBusinessPartyAccountingDto updateBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalBusinessPartyAccounting = await _context.BusinessPartyAccountings
                .AsNoTracking()
                .Where(bpa => bpa.Id == id && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBusinessPartyAccounting == null)
                return null;

            var businessPartyAccounting = await _context.BusinessPartyAccountings
                .Where(bpa => bpa.Id == id && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessPartyAccounting == null)
                return null;

            businessPartyAccounting.BusinessPartyId = updateBusinessPartyAccountingDto.BusinessPartyId;
            businessPartyAccounting.Iban = updateBusinessPartyAccountingDto.Iban;
            businessPartyAccounting.BankId = updateBusinessPartyAccountingDto.BankId;
            businessPartyAccounting.PaymentTermId = updateBusinessPartyAccountingDto.PaymentTermId;
            businessPartyAccounting.CreditLimit = updateBusinessPartyAccountingDto.CreditLimit;
            businessPartyAccounting.Notes = updateBusinessPartyAccountingDto.Notes;
            businessPartyAccounting.ModifiedAt = DateTime.UtcNow;
            businessPartyAccounting.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Update", currentUser, originalBusinessPartyAccounting, cancellationToken);

            _logger.LogInformation("Business party accounting {BusinessPartyAccountingId} updated by {User}", id, currentUser);

            // Reload with includes
            var updatedBusinessPartyAccounting = await _context.BusinessPartyAccountings
                .Include(bpa => bpa.Bank)
                .Include(bpa => bpa.PaymentTerm)
                .FirstAsync(bpa => bpa.Id == id, cancellationToken);

            var businessPartyName = await _context.BusinessParties
                .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(cancellationToken);

            return MapToBusinessPartyAccountingDto(updatedBusinessPartyAccounting, businessPartyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating business party accounting with ID {BusinessPartyAccountingId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteBusinessPartyAccountingAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalBusinessPartyAccounting = await _context.BusinessPartyAccountings
                .AsNoTracking()
                .Where(bpa => bpa.Id == id && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBusinessPartyAccounting == null)
                return false;

            var businessPartyAccounting = await _context.BusinessPartyAccountings
                .Where(bpa => bpa.Id == id && !bpa.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessPartyAccounting == null)
                return false;

            businessPartyAccounting.IsDeleted = true;
            businessPartyAccounting.DeletedAt = DateTime.UtcNow;
            businessPartyAccounting.DeletedBy = currentUser;
            businessPartyAccounting.ModifiedAt = DateTime.UtcNow;
            businessPartyAccounting.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Delete", currentUser, originalBusinessPartyAccounting, cancellationToken);

            _logger.LogInformation("Business party accounting {BusinessPartyAccountingId} deleted by {User}", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business party accounting with ID {BusinessPartyAccountingId}", id);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    public async Task<bool> BusinessPartyExistsAsync(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        return await _context.BusinessParties
            .AnyAsync(bp => bp.Id == businessPartyId && !bp.IsDeleted, cancellationToken);
    }

    private static BusinessPartyDto MapToBusinessPartyDto(BusinessParty businessParty, int addressCount, int contactCount, int referenceCount, bool hasAccountingData)
    {
        return new BusinessPartyDto
        {
            Id = businessParty.Id,
            PartyType = (EventForge.DTOs.Common.BusinessPartyType)businessParty.PartyType,
            Name = businessParty.Name,
            TaxCode = businessParty.TaxCode,
            VatNumber = businessParty.VatNumber,
            SdiCode = businessParty.SdiCode,
            Pec = businessParty.Pec,
            Notes = businessParty.Notes,
            AddressCount = addressCount,
            ContactCount = contactCount,
            ReferenceCount = referenceCount,
            HasAccountingData = hasAccountingData,
            IsActive = businessParty.IsActive,
            CreatedAt = businessParty.CreatedAt,
            CreatedBy = businessParty.CreatedBy,
            ModifiedAt = businessParty.ModifiedAt,
            ModifiedBy = businessParty.ModifiedBy
        };
    }

    private static BusinessPartyAccountingDto MapToBusinessPartyAccountingDto(BusinessPartyAccounting businessPartyAccounting, string? businessPartyName)
    {
        return new BusinessPartyAccountingDto
        {
            Id = businessPartyAccounting.Id,
            BusinessPartyId = businessPartyAccounting.BusinessPartyId,
            BusinessPartyName = businessPartyName,
            Iban = businessPartyAccounting.Iban,
            BankId = businessPartyAccounting.BankId,
            BankName = businessPartyAccounting.Bank?.Name,
            PaymentTermId = businessPartyAccounting.PaymentTermId,
            PaymentTermName = businessPartyAccounting.PaymentTerm?.Name,
            CreditLimit = businessPartyAccounting.CreditLimit,
            Notes = businessPartyAccounting.Notes,
            CreatedAt = businessPartyAccounting.CreatedAt,
            CreatedBy = businessPartyAccounting.CreatedBy,
            ModifiedAt = businessPartyAccounting.ModifiedAt,
            ModifiedBy = businessPartyAccounting.ModifiedBy
        };
    }

    #endregion
}