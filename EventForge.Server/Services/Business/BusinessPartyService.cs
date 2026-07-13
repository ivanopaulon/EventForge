using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service implementation for managing business parties and their accounting data.
/// </summary>
public partial class BusinessPartyService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<BusinessPartyService> logger) : IBusinessPartyService
{

    #region Helper Methods

    /// <summary>
    /// Enriches a list of BusinessParty entities by fetching all related counts and data in
    /// 6 parallel batch queries instead of 6 sequential per-entity queries (N+1 elimination).
    /// </summary>
    private async Task<List<BusinessPartyDto>> EnrichBusinessPartiesAsync(
        List<BusinessParty> businessParties,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (businessParties.Count == 0) return new List<BusinessPartyDto>();

        var bpIds = businessParties.Select(bp => bp.Id).ToList();

        // 4 sequential DB queries (EF Core DbContext does not support concurrent operations).
        // Address count and primary address are both derived in-memory from the same result set.
        // Contact count and contacts map are both derived in-memory from the same result set.
        var referenceCountsList = await context.References
            .AsNoTracking()
            .Where(r => r.OwnerType == "BusinessParty" && bpIds.Contains(r.OwnerId) && !r.IsDeleted && r.TenantId == tenantId)
            .GroupBy(r => r.OwnerId)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var accountingIdsList = await context.BusinessPartyAccountings
            .AsNoTracking()
            .Where(bpa => bpIds.Contains(bpa.BusinessPartyId) && !bpa.IsDeleted && bpa.TenantId == tenantId)
            .Select(bpa => bpa.BusinessPartyId)
            .ToListAsync(cancellationToken);

        var allAddresses = await context.Addresses
            .AsNoTracking()
            .Where(a => a.OwnerType == "BusinessParty" && bpIds.Contains(a.OwnerId) && !a.IsDeleted && a.TenantId == tenantId)
            .OrderBy(a => a.OwnerId).ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var contactsList = await context.Contacts
            .AsNoTracking()
            .Where(c => c.OwnerType == "BusinessParty" && bpIds.Contains(c.OwnerId) && !c.IsDeleted && c.TenantId == tenantId)
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.ContactType)
            .ToListAsync(cancellationToken);

        var addressGroups = allAddresses.GroupBy(a => a.OwnerId).ToDictionary(g => g.Key, g => g.ToList());
        var contactGroups = contactsList.GroupBy(c => c.OwnerId).ToDictionary(g => g.Key, g => g.ToList());
        var addressCountMap = addressGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        var contactCountMap = contactGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        var referenceCountMap = referenceCountsList.ToDictionary(x => x.OwnerId, x => x.Count);
        var accountingSet = new HashSet<Guid>(accountingIdsList);
        var primaryAddressMap = addressGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.First());
        var contactsMap = contactGroups;

        return businessParties.Select(bp => MapToBusinessPartyDto(
            bp,
            addressCountMap.GetValueOrDefault(bp.Id),
            contactCountMap.GetValueOrDefault(bp.Id),
            referenceCountMap.GetValueOrDefault(bp.Id),
            accountingSet.Contains(bp.Id),
            primaryAddressMap.GetValueOrDefault(bp.Id),
            contactsMap.GetValueOrDefault(bp.Id) ?? new List<Data.Entities.Common.Contact>()
        )).ToList();
    }

    public async Task<bool> BusinessPartyExistsAsync(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        return await context.BusinessParties
            .AsNoTracking()
            .AnyAsync(bp => bp.Id == businessPartyId && !bp.IsDeleted, cancellationToken);
    }

    public async Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesWithBirthdayAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue) return Enumerable.Empty<BusinessPartyDto>();

        var parties = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => !bp.IsDeleted && bp.DateOfBirth.HasValue && bp.TenantId == currentTenantId.Value)
            .OrderBy(bp => bp.Name)
            .ToListAsync(cancellationToken);

        return parties.Select(bp => new BusinessPartyDto
        {
            Id = bp.Id,
            PartyType = BusinessPartyTypeMapper.ToDto(bp.PartyType),
            Name = bp.Name,
            DateOfBirth = bp.DateOfBirth,
            IsActive = bp.IsActive,
            CreatedAt = bp.CreatedAt,
            CreatedBy = bp.CreatedBy
        });
    }

    private static BusinessPartyDto MapToBusinessPartyDto(BusinessParty businessParty, int addressCount, int contactCount, int referenceCount, bool hasAccountingData, Data.Entities.Common.Address? primaryAddress, List<Data.Entities.Common.Contact> contacts)
    {
        return new BusinessPartyDto
        {
            Id = businessParty.Id,
            PartyType = BusinessPartyTypeMapper.ToDto(businessParty.PartyType),
            Name = businessParty.Name,
            TaxCode = businessParty.TaxCode,
            VatNumber = businessParty.VatNumber,
            SdiCode = businessParty.SdiCode,
            Pec = businessParty.Pec,
            Notes = businessParty.Notes,
            DateOfBirth = businessParty.DateOfBirth,
            AddressCount = addressCount,
            ContactCount = contactCount,
            ReferenceCount = referenceCount,
            HasAccountingData = hasAccountingData,
            City = primaryAddress?.City,
            Province = primaryAddress?.Province,
            Country = primaryAddress?.Country,
            Contacts = contacts.Select(c => new Prym.DTOs.Common.ContactDto
            {
                Id = c.Id,
                OwnerId = c.OwnerId,
                OwnerType = c.OwnerType,
                ContactType = (Prym.DTOs.Common.ContactType)c.ContactType,
                Value = c.Value,
                Purpose = (Prym.DTOs.Common.ContactPurpose)c.Purpose,
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
            ModifiedBy = businessParty.ModifiedBy,
            DefaultSalesPriceListId = businessParty.DefaultSalesPriceListId,
            DefaultSalesPriceListName = businessParty.DefaultSalesPriceList?.Name,
            DefaultPurchasePriceListId = businessParty.DefaultPurchasePriceListId,
            DefaultPurchasePriceListName = businessParty.DefaultPurchasePriceList?.Name,
            DefaultPriceApplicationMode = businessParty.DefaultPriceApplicationMode,
            ForcedPriceListId = businessParty.ForcedPriceListId,
            ForcedPriceListName = businessParty.ForcedPriceList?.Name,
            RowVersion = businessParty.RowVersion
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

    private static ClassificationNodeDto MapToClassificationNodeDto(ClassificationNode node)
    {
        return new ClassificationNodeDto
        {
            Id = node.Id,
            Code = node.Code,
            Name = node.Name,
            Description = node.Description,
            Type = node.Type.ToDto(),
            Status = node.Status.ToDto(),
            Level = node.Level,
            ApplicableTo = node.ApplicableTo,
            Order = node.Order,
            ParentId = node.ParentId,
            IsActive = node.IsActive,
            CreatedAt = node.CreatedAt,
            CreatedBy = node.CreatedBy,
            ModifiedAt = node.ModifiedAt,
            ModifiedBy = node.ModifiedBy
        };
    }

    #endregion

}
