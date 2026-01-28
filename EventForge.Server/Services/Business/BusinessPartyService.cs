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

                // Get primary address for location info
                var primaryAddress = await _context.Addresses
                    .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == businessParty.Id && !a.IsDeleted && a.TenantId == currentTenantId.Value)
                    .OrderBy(a => a.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // Get contacts for tooltip
                var contacts = await _context.Contacts
                    .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == businessParty.Id && !c.IsDeleted && c.TenantId == currentTenantId.Value)
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.ContactType)
                    .ToListAsync(cancellationToken);

                businessPartyDtos.Add(MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts));
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

            // Get primary address for location info
            var primaryAddress = await _context.Addresses
                .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted && a.TenantId == currentTenantId.Value)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Get contacts for tooltip
            var contacts = await _context.Contacts
                .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted && c.TenantId == currentTenantId.Value)
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.ContactType)
                .ToListAsync(cancellationToken);

            return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts);
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

                // Get primary address for location info
                var primaryAddress = await _context.Addresses
                    .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == businessParty.Id && !a.IsDeleted && a.TenantId == currentTenantId.Value)
                    .OrderBy(a => a.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // Get contacts for tooltip
                var contacts = await _context.Contacts
                    .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == businessParty.Id && !c.IsDeleted && c.TenantId == currentTenantId.Value)
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.ContactType)
                    .ToListAsync(cancellationToken);

                businessPartyDtos.Add(MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts));
            }

            return businessPartyDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business parties by type {BusinessPartyType}", partyType);
            throw;
        }
    }

    public async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, DTOs.Common.BusinessPartyType? partyType = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<BusinessPartyDto>();
            }

            var query = _context.BusinessParties
                .WhereActiveTenant(currentTenantId.Value);

            // Filter by party type if specified
            if (partyType.HasValue)
            {
                query = query.Where(bp =>
                    bp.PartyType == (Data.Entities.Business.BusinessPartyType)partyType.Value ||
                    bp.PartyType == Data.Entities.Business.BusinessPartyType.ClienteFornitore
                );
            }

            // Search by name or tax code (case-insensitive)
            var businessParties = await query
                .Where(bp =>
                    EF.Functions.Like(bp.Name, $"%{searchTerm}%") ||
                    (bp.TaxCode != null && EF.Functions.Like(bp.TaxCode, $"%{searchTerm}%"))
                )
                .OrderBy(bp => bp.Name)
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

                // Get primary address for location info
                var primaryAddress = await _context.Addresses
                    .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == businessParty.Id && !a.IsDeleted && a.TenantId == currentTenantId.Value)
                    .OrderBy(a => a.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // Get contacts for tooltip
                var contacts = await _context.Contacts
                    .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == businessParty.Id && !c.IsDeleted && c.TenantId == currentTenantId.Value)
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.ContactType)
                    .ToListAsync(cancellationToken);

                businessPartyDtos.Add(MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts));
            }

            return businessPartyDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching business parties with term {SearchTerm}", searchTerm);
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

            return MapToBusinessPartyDto(businessParty, 0, 0, 0, false, null, new List<Data.Entities.Common.Contact>());
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

            // Get primary address for location info
            var primaryAddress = await _context.Addresses
                .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Get contacts for tooltip
            var contacts = await _context.Contacts
                .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted)
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.ContactType)
                .ToListAsync(cancellationToken);

            return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts);
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

    private static BusinessPartyDto MapToBusinessPartyDto(BusinessParty businessParty, int addressCount, int contactCount, int referenceCount, bool hasAccountingData, Data.Entities.Common.Address? primaryAddress, List<Data.Entities.Common.Contact> contacts)
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
            City = primaryAddress?.City,
            Province = primaryAddress?.Province,
            Country = primaryAddress?.Country,
            Contacts = contacts.Select(c => new EventForge.DTOs.Common.ContactDto
            {
                Id = c.Id,
                OwnerId = c.OwnerId,
                OwnerType = c.OwnerType,
                ContactType = (EventForge.DTOs.Common.ContactType)c.ContactType,
                Value = c.Value,
                Purpose = (EventForge.DTOs.Common.ContactPurpose)c.Purpose,
                Relationship = c.Relationship,
                IsPrimary = c.IsPrimary,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                ModifiedAt = c.ModifiedAt,
                ModifiedBy = c.ModifiedBy
            }).ToList(),
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

    #region Business Party Documents

    public async Task<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>> GetBusinessPartyDocumentsAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? documentTypeId = null,
        string? searchNumber = null,
        DTOs.Common.ApprovalStatus? approvalStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            var query = _context.DocumentHeaders
                .Include(dh => dh.DocumentType)
                .Include(dh => dh.BusinessParty)
                .Where(dh => !dh.IsDeleted && dh.TenantId == currentTenantId.Value && dh.BusinessPartyId == businessPartyId);

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(dh => dh.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(dh => dh.Date <= toDate.Value);
            }

            if (documentTypeId.HasValue)
            {
                query = query.Where(dh => dh.DocumentTypeId == documentTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchNumber))
            {
                query = query.Where(dh => (dh.Number != null && dh.Number.Contains(searchNumber)) ||
                                         (dh.Series != null && dh.Series.Contains(searchNumber)));
            }

            if (approvalStatus.HasValue)
            {
                query = query.Where(dh => (int)dh.ApprovalStatus == (int)approvalStatus.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var documents = await query
                .OrderByDescending(dh => dh.Date)
                .ThenByDescending(dh => dh.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(dh => new EventForge.DTOs.Documents.DocumentHeaderDto
                {
                    Id = dh.Id,
                    DocumentTypeId = dh.DocumentTypeId,
                    DocumentTypeName = dh.DocumentType != null ? dh.DocumentType.Name : null,
                    Series = dh.Series,
                    Number = dh.Number,
                    Date = dh.Date,
                    BusinessPartyId = dh.BusinessPartyId,
                    BusinessPartyName = dh.BusinessParty != null ? dh.BusinessParty.Name : null,
                    TotalNetAmount = dh.TotalNetAmount,
                    TotalGrossAmount = dh.TotalGrossAmount,
                    VatAmount = dh.VatAmount,
                    ApprovalStatus = (DTOs.Common.ApprovalStatus)dh.ApprovalStatus,
                    Status = (DTOs.Common.DocumentStatus)dh.Status,
                    CreatedAt = dh.CreatedAt,
                    CreatedBy = dh.CreatedBy,
                    ModifiedAt = dh.ModifiedAt,
                    ModifiedBy = dh.ModifiedBy
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>
            {
                Items = documents,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for business party {BusinessPartyId}", businessPartyId);
            throw;
        }
    }

    #endregion

    #region Business Party Product Analysis

    public async Task<PagedResult<BusinessPartyProductAnalysisDto>> GetBusinessPartyProductAnalysisAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? type = null,
        int? topN = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            // Build base query with all document rows for this business party
            var rowsQuery = _context.DocumentRows
                .Include(r => r.DocumentHeader)
                    .ThenInclude(h => h!.DocumentType)
                .Include(r => r.Product)
                .Where(r => !r.IsDeleted &&
                           r.TenantId == currentTenantId.Value &&
                           r.DocumentHeader!.BusinessPartyId == businessPartyId &&
                           !r.DocumentHeader.IsDeleted &&
                           r.ProductId != null);

            // Apply date filters
            if (fromDate.HasValue)
            {
                rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.Date <= toDate.Value);
            }

            // Apply type filter (purchase/sale)
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (type.Equals("purchase", StringComparison.OrdinalIgnoreCase))
                {
                    rowsQuery = rowsQuery.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease);
                }
                else if (type.Equals("sale", StringComparison.OrdinalIgnoreCase))
                {
                    rowsQuery = rowsQuery.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease);
                }
            }

            // Materialize filtered rows first (to avoid EF translation issues with complex calculations)
            var rows = await rowsQuery.ToListAsync(cancellationToken);

            // Group and aggregate in memory
            var grouped = rows
                .GroupBy(r => new { r.ProductId, r.Product!.Code, r.Product.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId!.Value,
                    ProductCode = g.Key.Code,
                    ProductName = g.Key.Name,
                    // Purchase aggregations
                    QuantityPurchased = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Sum(r => r.BaseQuantity ?? r.Quantity),
                    ValuePurchased = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Sum(r => CalculateEffectiveLineTotal(r)),
                    LastPurchaseDate = g.Where(r => r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Max(r => (DateTime?)r.DocumentHeader!.Date),
                    // Sale aggregations
                    QuantitySold = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Sum(r => r.BaseQuantity ?? r.Quantity),
                    ValueSold = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Sum(r => CalculateEffectiveLineTotal(r)),
                    LastSaleDate = g.Where(r => !r.DocumentHeader!.DocumentType!.IsStockIncrease)
                        .Max(r => (DateTime?)r.DocumentHeader!.Date)
                })
                .ToList();

            // Calculate averages and create DTOs
            var analysisResults = grouped.Select(g => new BusinessPartyProductAnalysisDto
            {
                ProductId = g.ProductId,
                ProductCode = g.ProductCode,
                ProductName = g.ProductName,
                QuantityPurchased = g.QuantityPurchased,
                ValuePurchased = g.ValuePurchased,
                QuantitySold = g.QuantitySold,
                ValueSold = g.ValueSold,
                LastPurchaseDate = g.LastPurchaseDate,
                LastSaleDate = g.LastSaleDate,
                AvgPurchasePrice = g.QuantityPurchased > 0 ? g.ValuePurchased / g.QuantityPurchased : 0m,
                AvgSalePrice = g.QuantitySold > 0 ? g.ValueSold / g.QuantitySold : 0m
            }).ToList();

            // Apply sorting
            var sortByField = sortBy?.ToLowerInvariant() ?? "valuepurchased";
            analysisResults = sortByField switch
            {
                "valuesold" => sortDescending
                    ? analysisResults.OrderByDescending(a => a.ValueSold).ToList()
                    : analysisResults.OrderBy(a => a.ValueSold).ToList(),
                "quantitypurchased" => sortDescending
                    ? analysisResults.OrderByDescending(a => a.QuantityPurchased).ToList()
                    : analysisResults.OrderBy(a => a.QuantityPurchased).ToList(),
                "quantitysold" => sortDescending
                    ? analysisResults.OrderByDescending(a => a.QuantitySold).ToList()
                    : analysisResults.OrderBy(a => a.QuantitySold).ToList(),
                "productname" => sortDescending
                    ? analysisResults.OrderByDescending(a => a.ProductName).ToList()
                    : analysisResults.OrderBy(a => a.ProductName).ToList(),
                _ => sortDescending
                    ? analysisResults.OrderByDescending(a => a.ValuePurchased).ToList()
                    : analysisResults.OrderBy(a => a.ValuePurchased).ToList()
            };

            // Apply topN filter if specified
            if (topN.HasValue && topN.Value > 0)
            {
                analysisResults = analysisResults.Take(topN.Value).ToList();
            }

            var totalCount = analysisResults.Count;

            // Apply pagination
            var pagedResults = analysisResults
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<BusinessPartyProductAnalysisDto>
            {
                Items = pagedResults,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product analysis for business party {BusinessPartyId}", businessPartyId);
            throw;
        }
    }

    /// <summary>
    /// Calculates the effective line total (quantity * effective unit price after discounts).
    /// Implements the same logic as PriceTrend for consistency.
    /// </summary>
    private static decimal CalculateEffectiveLineTotal(DocumentRow row)
    {
        // Use normalized values (base unit if available)
        var unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        var weightQuantity = row.BaseQuantity ?? row.Quantity;

        // Calculate per-unit discount
        decimal unitDiscount;
        if (row.DiscountType == EventForge.DTOs.Common.DiscountType.Percentage)
        {
            unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        }
        else
        {
            // Absolute discount: divide by quantity
            unitDiscount = row.Quantity > 0 ? row.LineDiscountValue / row.Quantity : 0m;
        }

        // Clamp discount to not exceed unit price
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);

        // Calculate effective unit price
        var effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Return total
        return effectiveUnitPrice * weightQuantity;
    }

    #endregion

    #region Full Detail Aggregated Query

    /// <summary>
    /// Recupera tutti i dettagli completi di un BusinessParty in una singola query ottimizzata.
    /// Ottimizzazione FASE 5: riduce N+1 queries.
    /// </summary>
    public async Task<BusinessPartyFullDetailDto?> GetFullDetailAsync(
        Guid id, 
        bool includeInactive = false, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching full detail for BusinessParty {Id} (includeInactive: {IncludeInactive})", id, includeInactive);
            
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for business party operations.");
            }

            // ⚡ Single query con eager loading ottimizzato
            var businessParty = await _context.BusinessParties
                .Where(bp => bp.Id == id && !bp.IsDeleted && bp.TenantId == currentTenantId.Value)
                .Include(bp => bp.Contacts.Where(c => c.TenantId == currentTenantId.Value && (includeInactive || !c.IsDeleted)))
                .Include(bp => bp.Addresses.Where(a => a.TenantId == currentTenantId.Value && (includeInactive || !a.IsDeleted)))
                .AsSplitQuery() // ⭐ CRITICO: evita cartesian explosion con multiple includes
                .FirstOrDefaultAsync(cancellationToken);
            
            if (businessParty == null)
            {
                _logger.LogWarning("BusinessParty {Id} not found", id);
                return null;
            }

            // ⚡ Carica i listini prezzi associati separatamente per evitare problemi con Include filtrati
            var priceListsQuery = await _context.PriceListBusinessParties
                .Where(plbp => plbp.BusinessPartyId == id 
                            && !plbp.IsDeleted 
                            && plbp.TenantId == currentTenantId.Value
                            && !plbp.PriceList.IsDeleted
                            && plbp.PriceList.Status == Data.Entities.PriceList.PriceListStatus.Active)
                .Include(plbp => plbp.PriceList)
                .ToListAsync(cancellationToken);
            
            // Map a DTO aggregato
            var result = new BusinessPartyFullDetailDto
            {
                BusinessParty = MapToBusinessPartyDtoSimple(businessParty),
                
                Contacts = businessParty.Contacts
                    .Select(MapToContactDto)
                    .OrderByDescending(c => c.IsPrimary)
                    .ThenBy(c => c.ContactType)
                    .ToList(),
                
                Addresses = businessParty.Addresses
                    .Select(MapToAddressDto)
                    .OrderBy(a => a.AddressType)
                    .ToList(),
                
                AssignedPriceLists = priceListsQuery
                    .Select(plbp => MapToPriceListDto(plbp.PriceList))
                    .OrderByDescending(pl => pl.IsDefault)
                    .ThenBy(pl => pl.Priority)
                    .ToList(),
                
                Statistics = await CalculateStatisticsAsync(id, currentTenantId.Value, cancellationToken)
            };
            
            _logger.LogInformation(
                "Full detail loaded for BusinessParty {Id}: {ContactCount} contacts, {AddressCount} addresses, {PriceListCount} price lists",
                id, result.Contacts.Count, result.Addresses.Count, result.AssignedPriceLists.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving full detail for BusinessParty {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Calcola statistiche aggregate per BusinessParty (query parallele ottimizzate)
    /// </summary>
    private async Task<BusinessPartyStatisticsDto> CalculateStatisticsAsync(
        Guid businessPartyId, 
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        
        // ⚡ Query parallele per performance ottimali
        var contactsTask = _context.Contacts
            .CountAsync(c => c.OwnerId == businessPartyId && c.OwnerType == "BusinessParty" && !c.IsDeleted && c.TenantId == tenantId, cancellationToken);
        
        var addressesTask = _context.Addresses
            .CountAsync(a => a.OwnerId == businessPartyId && a.OwnerType == "BusinessParty" && !a.IsDeleted && a.TenantId == tenantId, cancellationToken);
        
        var priceListsTask = _context.PriceListBusinessParties
            .CountAsync(plbp => plbp.BusinessPartyId == businessPartyId && !plbp.IsDeleted && !plbp.PriceList.IsDeleted && plbp.TenantId == tenantId, cancellationToken);
        
        var documentsTask = _context.DocumentHeaders
            .CountAsync(d => d.BusinessPartyId == businessPartyId && !d.IsDeleted && d.TenantId == tenantId, cancellationToken);
        
        var lastOrderTask = _context.DocumentHeaders
            .Where(d => d.BusinessPartyId == businessPartyId && !d.IsDeleted && d.TenantId == tenantId)
            .OrderByDescending(d => d.Date)
            .Select(d => (DateTime?)d.Date)
            .FirstOrDefaultAsync(cancellationToken);
        
        var revenueTask = _context.DocumentHeaders
            .Where(d => d.BusinessPartyId == businessPartyId 
                     && !d.IsDeleted 
                     && d.TenantId == tenantId
                     && d.Date.Year == currentYear
                     && d.DocumentType != null
                     && !d.DocumentType.IsStockIncrease) // Solo vendite (non acquisti)
            .SumAsync(d => (decimal?)d.TotalGrossAmount, cancellationToken);
        
        await Task.WhenAll(contactsTask, addressesTask, priceListsTask, documentsTask, lastOrderTask, revenueTask);
        
        return new BusinessPartyStatisticsDto
        {
            TotalContacts = await contactsTask,
            TotalAddresses = await addressesTask,
            TotalPriceLists = await priceListsTask,
            ActiveFidelityCards = 0, // TODO: implementare quando backend fidelity sarà pronto
            TotalDocuments = await documentsTask,
            LastOrderDate = await lastOrderTask,
            TotalRevenueYTD = await revenueTask ?? 0m
        };
    }

    /// <summary>
    /// Maps BusinessParty entity to DTO (simplified version without counts)
    /// </summary>
    private static BusinessPartyDto MapToBusinessPartyDtoSimple(Data.Entities.Business.BusinessParty businessParty)
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
            AddressCount = 0, // Populated separately in Statistics
            ContactCount = 0, // Populated separately in Statistics
            ReferenceCount = 0,
            HasAccountingData = false,
            IsActive = businessParty.IsActive,
            CreatedAt = businessParty.CreatedAt,
            CreatedBy = businessParty.CreatedBy,
            ModifiedAt = businessParty.ModifiedAt,
            ModifiedBy = businessParty.ModifiedBy,
            DefaultSalesPriceListId = businessParty.DefaultSalesPriceListId,
            DefaultPurchasePriceListId = businessParty.DefaultPurchasePriceListId
        };
    }

    /// <summary>
    /// Maps Contact entity to DTO
    /// </summary>
    private static EventForge.DTOs.Common.ContactDto MapToContactDto(Data.Entities.Common.Contact contact)
    {
        return new EventForge.DTOs.Common.ContactDto
        {
            Id = contact.Id,
            OwnerId = contact.OwnerId,
            OwnerType = contact.OwnerType,
            ContactType = contact.ContactType.ToDto(),
            Value = contact.Value,
            Purpose = contact.Purpose,
            Relationship = contact.Relationship,
            IsPrimary = contact.IsPrimary,
            Notes = contact.Notes,
            CreatedAt = contact.CreatedAt,
            CreatedBy = contact.CreatedBy,
            ModifiedAt = contact.ModifiedAt,
            ModifiedBy = contact.ModifiedBy
        };
    }

    /// <summary>
    /// Maps Address entity to DTO
    /// </summary>
    private static EventForge.DTOs.Common.AddressDto MapToAddressDto(Data.Entities.Common.Address address)
    {
        return new EventForge.DTOs.Common.AddressDto
        {
            Id = address.Id,
            OwnerId = address.OwnerId,
            OwnerType = address.OwnerType,
            AddressType = address.AddressType.ToDto(),
            Street = address.Street,
            City = address.City,
            ZipCode = address.ZipCode,
            Province = address.Province,
            Country = address.Country,
            Notes = address.Notes,
            CreatedAt = address.CreatedAt,
            CreatedBy = address.CreatedBy,
            ModifiedAt = address.ModifiedAt,
            ModifiedBy = address.ModifiedBy
        };
    }

    /// <summary>
    /// Maps PriceList entity to DTO
    /// </summary>
    private static EventForge.DTOs.PriceLists.PriceListDto MapToPriceListDto(Data.Entities.PriceList.PriceList priceList)
    {
        return new EventForge.DTOs.PriceLists.PriceListDto
        {
            Id = priceList.Id,
            Name = priceList.Name,
            Code = priceList.Code,
            Description = priceList.Description,
            Type = priceList.Type,
            Direction = priceList.Direction,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Notes = priceList.Notes,
            Status = (EventForge.DTOs.Common.PriceListStatus)priceList.Status,
            IsDefault = priceList.IsDefault,
            Priority = priceList.Priority,
            EventId = priceList.EventId,
            EventName = priceList.Event?.Name,
            EntryCount = priceList.ProductPrices?.Count(ple => !ple.IsDeleted) ?? 0,
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy,
            ModifiedAt = priceList.ModifiedAt,
            ModifiedBy = priceList.ModifiedBy
        };
    }

    #endregion
}