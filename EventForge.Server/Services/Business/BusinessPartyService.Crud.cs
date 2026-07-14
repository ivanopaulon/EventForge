using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        // NOTE: Tenant isolation test coverage should be expanded in future test iterations
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var query = context.BusinessParties
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var businessParties = await query
            .Include(bp => bp.DefaultSalesPriceList)
            .Include(bp => bp.DefaultPurchasePriceList)
            .Include(bp => bp.ForcedPriceList)
            .OrderBy(bp => bp.Name)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        if (businessParties.Count == 0)
        {
            return new PagedResult<BusinessPartyDto>
            {
                Items = new List<BusinessPartyDto>(),
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }

        // Batch all related-entity counts in parallel to eliminate N+1 query pattern
        var businessPartyDtos = await EnrichBusinessPartiesAsync(businessParties, currentTenantId.Value, cancellationToken);

        return new PagedResult<BusinessPartyDto>
        {
            Items = businessPartyDtos,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BusinessPartyDto?> GetBusinessPartyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var businessParty = await context.BusinessParties
            .AsNoTracking()
            .Include(bp => bp.DefaultSalesPriceList)
            .Include(bp => bp.DefaultPurchasePriceList)
            .Include(bp => bp.ForcedPriceList)
            .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessParty is null)
            return null;

        var addressCount = await context.Addresses
            .AsNoTracking()
            .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted && a.TenantId == currentTenantId.Value, cancellationToken);
        var contactCount = await context.Contacts
            .AsNoTracking()
            .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted && c.TenantId == currentTenantId.Value, cancellationToken);
        var referenceCount = await context.References
            .AsNoTracking()
            .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == id && !r.IsDeleted && r.TenantId == currentTenantId.Value, cancellationToken);
        var hasAccountingData = await context.BusinessPartyAccountings
            .AsNoTracking()
            .AnyAsync(bpa => bpa.BusinessPartyId == id && !bpa.IsDeleted && bpa.TenantId == currentTenantId.Value, cancellationToken);

        // Get primary address for location info
        var primaryAddress = await context.Addresses
            .AsNoTracking()
            .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted && a.TenantId == currentTenantId.Value)
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Get contacts for tooltip
        var contacts = await context.Contacts
            .AsNoTracking()
            .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted && c.TenantId == currentTenantId.Value)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.ContactType)
            .ToListAsync(cancellationToken);

        return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts);
    }

    public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(Prym.DTOs.Common.BusinessPartyType partyType, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var businessParties = await context.BusinessParties
            .AsNoTracking()
            .Include(bp => bp.DefaultSalesPriceList)
            .Include(bp => bp.DefaultPurchasePriceList)
            .Include(bp => bp.ForcedPriceList)
            .Where(bp => bp.PartyType == BusinessPartyTypeMapper.ToEntity(partyType))
            .WhereActiveTenant(currentTenantId.Value)
            .OrderBy(bp => bp.Name)
            .ToListAsync(cancellationToken);

        return await EnrichBusinessPartiesAsync(businessParties, currentTenantId.Value, cancellationToken);
    }

    public async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, Prym.DTOs.Common.BusinessPartyType? partyType = null, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        var query = context.BusinessParties
            .AsNoTracking()
            .WhereActiveTenant(currentTenantId.Value);

        // Filter by party type if specified
        if (partyType.HasValue)
        {
            query = query.Where(bp =>
                bp.PartyType == BusinessPartyTypeMapper.ToEntity(partyType.Value) ||
                bp.PartyType == Data.Entities.Business.BusinessPartyType.ClienteFornitore
            );
        }

        // Search by name, tax code or VAT number (case-insensitive, multi-word AND logic)
        var searchWords = searchTerm.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var word in searchWords)
        {
            var w = word;
            query = query.Where(bp =>
                EF.Functions.Like(bp.Name, $"%{w}%") ||
                (bp.TaxCode != null && EF.Functions.Like(bp.TaxCode, $"%{w}%")) ||
                (bp.VatNumber != null && EF.Functions.Like(bp.VatNumber, $"%{w}%"))
            );
        }

        var businessParties = await query
            .Include(bp => bp.DefaultSalesPriceList)
            .Include(bp => bp.DefaultPurchasePriceList)
            .Include(bp => bp.ForcedPriceList)
            .OrderBy(bp => bp.Name)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return await EnrichBusinessPartiesAsync(businessParties, currentTenantId.Value, cancellationToken);
    }

    public async Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var businessParty = new BusinessParty
        {
            TenantId = currentTenantId.Value,
            PartyType = BusinessPartyTypeMapper.ToEntity(createBusinessPartyDto.PartyType),
            Name = createBusinessPartyDto.Name,
            TaxCode = createBusinessPartyDto.TaxCode,
            VatNumber = createBusinessPartyDto.VatNumber,
            SdiCode = createBusinessPartyDto.SdiCode,
            Pec = createBusinessPartyDto.Pec,
            Notes = createBusinessPartyDto.Notes,
            DateOfBirth = createBusinessPartyDto.DateOfBirth,
            DefaultSalesPriceListId = createBusinessPartyDto.DefaultSalesPriceListId,
            DefaultPurchasePriceListId = createBusinessPartyDto.DefaultPurchasePriceListId,
            DefaultPriceApplicationMode = createBusinessPartyDto.DefaultPriceApplicationMode,
            ForcedPriceListId = createBusinessPartyDto.ForcedPriceListId,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        _ = context.BusinessParties.Add(businessParty);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(businessParty, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Business party {BusinessPartyName} created with ID {BusinessPartyId} by {User}",
            businessParty.Name, businessParty.Id, currentUser);

        return MapToBusinessPartyDto(businessParty, 0, 0, 0, false, null, []);
    }

    public async Task<BusinessPartyDto?> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var originalBusinessParty = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalBusinessParty is null)
            return null;

        var businessParty = await context.BusinessParties
            .AsNoTracking()
            .Include(bp => bp.DefaultSalesPriceList)
            .Include(bp => bp.DefaultPurchasePriceList)
            .Include(bp => bp.ForcedPriceList)
            .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessParty is null)
            return null;

        businessParty.PartyType = BusinessPartyTypeMapper.ToEntity(updateBusinessPartyDto.PartyType);
        businessParty.Name = updateBusinessPartyDto.Name;
        businessParty.TaxCode = updateBusinessPartyDto.TaxCode;
        businessParty.VatNumber = updateBusinessPartyDto.VatNumber;
        businessParty.SdiCode = updateBusinessPartyDto.SdiCode;
        businessParty.Pec = updateBusinessPartyDto.Pec;
        businessParty.Notes = updateBusinessPartyDto.Notes;
        businessParty.DateOfBirth = updateBusinessPartyDto.DateOfBirth;
        businessParty.DefaultSalesPriceListId = updateBusinessPartyDto.DefaultSalesPriceListId;
        businessParty.DefaultPurchasePriceListId = updateBusinessPartyDto.DefaultPurchasePriceListId;
        businessParty.DefaultPriceApplicationMode = updateBusinessPartyDto.DefaultPriceApplicationMode;
        businessParty.ForcedPriceListId = updateBusinessPartyDto.ForcedPriceListId;
        businessParty.ModifiedAt = DateTime.UtcNow;
        businessParty.ModifiedBy = currentUser;

        // Apply optimistic concurrency: if client provided a RowVersion, use it as the
        // expected original value so EF Core detects concurrent modifications.
        if (updateBusinessPartyDto.RowVersion is not null && updateBusinessPartyDto.RowVersion.Length > 0)
            context.Entry(businessParty).Property(bp => bp.RowVersion).OriginalValue = updateBusinessPartyDto.RowVersion;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating business party {BusinessPartyId}.", id);
            throw new InvalidOperationException("Il record è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(businessParty, "Update", currentUser, originalBusinessParty, cancellationToken);

        logger.LogInformation("Business party {BusinessPartyId} updated by {User}", id, currentUser);

        var addressCount = await context.Addresses
            .AsNoTracking()
            .CountAsync(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted, cancellationToken);
        var contactCount = await context.Contacts
            .AsNoTracking()
            .CountAsync(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted, cancellationToken);
        var referenceCount = await context.References
            .AsNoTracking()
            .CountAsync(r => r.OwnerType == "BusinessParty" && r.OwnerId == id && !r.IsDeleted, cancellationToken);
        var hasAccountingData = await context.BusinessPartyAccountings
            .AsNoTracking()
            .AnyAsync(bpa => bpa.BusinessPartyId == id && !bpa.IsDeleted, cancellationToken);

        // Get primary address for location info
        var primaryAddress = await context.Addresses
            .AsNoTracking()
            .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && !a.IsDeleted)
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Get contacts for tooltip
        var contacts = await context.Contacts
            .AsNoTracking()
            .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && !c.IsDeleted)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.ContactType)
            .ToListAsync(cancellationToken);

        return MapToBusinessPartyDto(businessParty, addressCount, contactCount, referenceCount, hasAccountingData, primaryAddress, contacts);
    }

    public async Task<bool> DeleteBusinessPartyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var originalBusinessParty = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalBusinessParty is null)
            return false;

        var businessParty = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == id && bp.TenantId == currentTenantId.Value && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessParty is null)
            return false;

        businessParty.IsDeleted = true;
        businessParty.DeletedAt = DateTime.UtcNow;
        businessParty.DeletedBy = currentUser;
        businessParty.ModifiedAt = DateTime.UtcNow;
        businessParty.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting business party {BusinessPartyId}.", id);
            throw new InvalidOperationException("L'anagrafica è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(businessParty, "Delete", currentUser, originalBusinessParty, cancellationToken);

        logger.LogInformation("Business party {BusinessPartyId} deleted by {User}", id, currentUser);

        return true;
    }

}
