using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<PagedResult<BusinessPartyAccountingDto>> GetBusinessPartyAccountingAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for business party accounting operations.");

        var query = context.BusinessPartyAccountings
            .AsNoTracking()
            .Include(bpa => bpa.Bank)
            .Include(bpa => bpa.PaymentTerm)
            .Where(bpa => bpa.TenantId == currentTenantId && !bpa.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);
        var businessPartyAccountings = await query
            .OrderBy(bpa => bpa.BusinessPartyId)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Single batch query instead of one query per accounting record (N+1 elimination)
        var bpaIds = businessPartyAccountings.Select(bpa => bpa.BusinessPartyId).ToList();
        var nameMap = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bpaIds.Contains(bp.Id) && !bp.IsDeleted)
            .Select(bp => new { bp.Id, bp.Name })
            .ToDictionaryAsync(bp => bp.Id, bp => bp.Name, cancellationToken);

        var businessPartyAccountingDtos = businessPartyAccountings
            .Select(bpa => MapToBusinessPartyAccountingDto(bpa, nameMap.GetValueOrDefault(bpa.BusinessPartyId)))
            .ToList();

        return new PagedResult<BusinessPartyAccountingDto>
        {
            Items = businessPartyAccountingDtos,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var businessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Include(bpa => bpa.Bank)
            .Include(bpa => bpa.PaymentTerm)
            .Where(bpa => bpa.Id == id && bpa.TenantId == currentTenantId.Value && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessPartyAccounting is null)
            return null;

        var businessPartyName = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
            .Select(bp => bp.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToBusinessPartyAccountingDto(businessPartyAccounting, businessPartyName);
    }

    public async Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var businessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Include(bpa => bpa.Bank)
            .Include(bpa => bpa.PaymentTerm)
            .Where(bpa => bpa.BusinessPartyId == businessPartyId && bpa.TenantId == currentTenantId.Value && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessPartyAccounting is null)
            return null;

        var businessPartyName = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == businessPartyId && !bp.IsDeleted)
            .Select(bp => bp.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToBusinessPartyAccountingDto(businessPartyAccounting, businessPartyName);
    }

    public async Task<BusinessPartyAccountingDto> CreateBusinessPartyAccountingAsync(CreateBusinessPartyAccountingDto createBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
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

        _ = context.BusinessPartyAccountings.Add(businessPartyAccounting);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Insert", currentUser, null, cancellationToken);

        logger.LogInformation("Business party accounting created with ID {BusinessPartyAccountingId} by {User}",
            businessPartyAccounting.Id, currentUser);

        // Reload with includes
        var createdBusinessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Include(bpa => bpa.Bank)
            .Include(bpa => bpa.PaymentTerm)
            .FirstAsync(bpa => bpa.Id == businessPartyAccounting.Id, cancellationToken);

        var businessPartyName = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
            .Select(bp => bp.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToBusinessPartyAccountingDto(createdBusinessPartyAccounting, businessPartyName);
    }

    public async Task<BusinessPartyAccountingDto?> UpdateBusinessPartyAccountingAsync(Guid id, UpdateBusinessPartyAccountingDto updateBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for business party accounting operations.");

        var originalBusinessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Where(bpa => bpa.Id == id && bpa.TenantId == currentTenantId && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalBusinessPartyAccounting is null)
            return null;

        var businessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Where(bpa => bpa.Id == id && bpa.TenantId == currentTenantId && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessPartyAccounting is null)
            return null;

        businessPartyAccounting.BusinessPartyId = updateBusinessPartyAccountingDto.BusinessPartyId;
        businessPartyAccounting.Iban = updateBusinessPartyAccountingDto.Iban;
        businessPartyAccounting.BankId = updateBusinessPartyAccountingDto.BankId;
        businessPartyAccounting.PaymentTermId = updateBusinessPartyAccountingDto.PaymentTermId;
        businessPartyAccounting.CreditLimit = updateBusinessPartyAccountingDto.CreditLimit;
        businessPartyAccounting.Notes = updateBusinessPartyAccountingDto.Notes;
        businessPartyAccounting.ModifiedAt = DateTime.UtcNow;
        businessPartyAccounting.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating business party accounting {BusinessPartyAccountingId}.", id);
            throw new InvalidOperationException("I dati contabili sono stati modificati da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Update", currentUser, originalBusinessPartyAccounting, cancellationToken);

        logger.LogInformation("Business party accounting {BusinessPartyAccountingId} updated by {User}", id, currentUser);

        // Reload with includes
        var updatedBusinessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Include(bpa => bpa.Bank)
            .Include(bpa => bpa.PaymentTerm)
            .FirstAsync(bpa => bpa.Id == id, cancellationToken);

        var businessPartyName = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == businessPartyAccounting.BusinessPartyId && !bp.IsDeleted)
            .Select(bp => bp.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return MapToBusinessPartyAccountingDto(updatedBusinessPartyAccounting, businessPartyName);
    }

    public async Task<bool> DeleteBusinessPartyAccountingAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for business party accounting operations.");

        var originalBusinessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Where(bpa => bpa.Id == id && bpa.TenantId == currentTenantId && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (originalBusinessPartyAccounting is null)
            return false;

        var businessPartyAccounting = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Where(bpa => bpa.Id == id && bpa.TenantId == currentTenantId && !bpa.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessPartyAccounting is null)
            return false;

        businessPartyAccounting.IsDeleted = true;
        businessPartyAccounting.DeletedAt = DateTime.UtcNow;
        businessPartyAccounting.DeletedBy = currentUser;
        businessPartyAccounting.ModifiedAt = DateTime.UtcNow;
        businessPartyAccounting.ModifiedBy = currentUser;

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict deleting business party accounting {BusinessPartyAccountingId}.", id);
            throw new InvalidOperationException("I dati contabili sono stati modificati da un altro utente. Ricarica la pagina e riprova.", ex);
        }

        _ = await auditLogService.TrackEntityChangesAsync(businessPartyAccounting, "Delete", currentUser, originalBusinessPartyAccounting, cancellationToken);

        logger.LogInformation("Business party accounting {BusinessPartyAccountingId} deleted by {User}", id, currentUser);

        return true;
    }

}
