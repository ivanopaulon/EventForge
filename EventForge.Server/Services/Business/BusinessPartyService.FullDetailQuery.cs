using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    /// <summary>
    /// Recupera tutti i dettagli completi di un BusinessParty in una singola query ottimizzata.
    /// Ottimizzazione FASE 5: riduce N+1 queries.
    /// </summary>
    public async Task<BusinessPartyFullDetailDto?> GetFullDetailAsync(
        Guid id,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {

        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        // ⚡ Single query con eager loading ottimizzato
        var businessParty = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.Id == id && !bp.IsDeleted && bp.TenantId == currentTenantId.Value)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);

        if (businessParty is null)
        {
            logger.LogWarning("BusinessParty {Id} not found", id);
            return null;
        }

        // Query contacts and addresses directly via OwnerId (polymorphic association)
        var contactsQuery = context.Contacts
            .AsNoTracking()
            .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && c.TenantId == currentTenantId.Value);
        var addressesQuery = context.Addresses
            .AsNoTracking()
            .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && a.TenantId == currentTenantId.Value);

        var allContacts = includeInactive
            ? await context.Contacts.AsNoTracking().IgnoreQueryFilters()
                .Where(c => c.OwnerType == "BusinessParty" && c.OwnerId == id && c.TenantId == currentTenantId.Value)
                .ToListAsync(cancellationToken)
            : await contactsQuery.Where(c => !c.IsDeleted).ToListAsync(cancellationToken);

        var allAddresses = includeInactive
            ? await context.Addresses.AsNoTracking().IgnoreQueryFilters()
                .Where(a => a.OwnerType == "BusinessParty" && a.OwnerId == id && a.TenantId == currentTenantId.Value)
                .ToListAsync(cancellationToken)
            : await addressesQuery.Where(a => !a.IsDeleted).ToListAsync(cancellationToken);

        // Filter contacts and addresses based on includeInactive parameter
        var filteredContacts = allContacts;
        var filteredAddresses = allAddresses;

        // ⚡ Carica i listini prezzi associati separatamente per evitare problemi con Include filtrati
        var priceListsQuery = await context.PriceListBusinessParties
            .AsNoTracking()
            .Where(plbp => plbp.BusinessPartyId == id
                        && !plbp.IsDeleted
                        && plbp.TenantId == currentTenantId.Value
                        && !plbp.PriceList.IsDeleted
                        && plbp.PriceList.Status == Data.Entities.PriceList.PriceListStatus.Active)
            .Include(plbp => plbp.PriceList)
                .ThenInclude(pl => pl.Event)
            .Include(plbp => plbp.PriceList)
                .ThenInclude(pl => pl.ProductPrices)
            .ToListAsync(cancellationToken);

        // Map a DTO aggregato
        var result = new BusinessPartyFullDetailDto
        {
            BusinessParty = MapToBusinessPartyDtoSimple(businessParty),

            Contacts = filteredContacts
                .Select(MapToContactDto)
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.ContactType)
                .ToList(),

            Addresses = filteredAddresses
                .Select(MapToAddressDto)
                .OrderBy(a => a.AddressType)
                .ToList(),

            AssignedPriceLists = priceListsQuery
                .Select(plbp => MapToPriceListDto(plbp.PriceList))
                .OrderByDescending(pl => pl.IsDefault)
                .ThenBy(pl => pl.Priority)
                .ToList(),

            Statistics = await CalculateStatisticsAsync(id, currentTenantId.Value, filteredContacts.Count, filteredAddresses.Count, priceListsQuery.Count, cancellationToken)
        };

        logger.LogInformation(
            "Full detail loaded for BusinessParty {Id}: {ContactCount} contacts, {AddressCount} addresses, {PriceListCount} price lists",
            id, result.Contacts.Count, result.Addresses.Count, result.AssignedPriceLists.Count);

        return result;
    }

    /// <summary>
    /// Calcola statistiche aggregate per BusinessParty.
    /// I contatori di contatti, indirizzi e listini vengono passati direttamente dal chiamante
    /// (già caricati in memoria), evitando query ridondanti. Una sola query DB recupera
    /// i dati sui documenti e calcola count, ultima data e revenue YTD in-memory.
    /// </summary>
    private async Task<BusinessPartyStatisticsDto> CalculateStatisticsAsync(
        Guid businessPartyId,
        Guid tenantId,
        int totalContacts,
        int totalAddresses,
        int totalPriceLists,
        CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;

        // Single query: only the scalar fields needed for document-level statistics.
        var docStats = await context.DocumentHeaders
            .AsNoTracking()
            .Where(d => d.BusinessPartyId == businessPartyId && !d.IsDeleted && d.TenantId == tenantId)
            .Select(d => new
            {
                d.Date,
                d.TotalGrossAmount,
                IsRevenueLine = d.DocumentType != null && !d.DocumentType.IsStockIncrease
            })
            .ToListAsync(cancellationToken);

        return new BusinessPartyStatisticsDto
        {
            TotalContacts = totalContacts,
            TotalAddresses = totalAddresses,
            TotalPriceLists = totalPriceLists,
            ActiveFidelityCards = 0,
            TotalDocuments = docStats.Count,
            LastOrderDate = docStats.Count > 0 ? docStats.Max(d => (DateTime?)d.Date) : null,
            TotalRevenueYTD = docStats
                .Where(d => d.IsRevenueLine && d.Date.Year == currentYear)
                .Sum(d => d.TotalGrossAmount)
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
            PartyType = BusinessPartyTypeMapper.ToDto(businessParty.PartyType),
            Name = businessParty.Name,
            TaxCode = businessParty.TaxCode,
            VatNumber = businessParty.VatNumber,
            SdiCode = businessParty.SdiCode,
            Pec = businessParty.Pec,
            Notes = businessParty.Notes,
            DateOfBirth = businessParty.DateOfBirth,
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
            DefaultSalesPriceListName = businessParty.DefaultSalesPriceList?.Name,
            DefaultPurchasePriceListId = businessParty.DefaultPurchasePriceListId,
            DefaultPurchasePriceListName = businessParty.DefaultPurchasePriceList?.Name,
            DefaultPriceApplicationMode = businessParty.DefaultPriceApplicationMode,
            ForcedPriceListId = businessParty.ForcedPriceListId,
            ForcedPriceListName = businessParty.ForcedPriceList?.Name,
            RowVersion = businessParty.RowVersion
        };
    }

    /// <summary>
    /// Maps Contact entity to DTO
    /// </summary>
    private static Prym.DTOs.Common.ContactDto MapToContactDto(Data.Entities.Common.Contact contact)
    {
        return new Prym.DTOs.Common.ContactDto
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
    private static Prym.DTOs.Common.AddressDto MapToAddressDto(Data.Entities.Common.Address address)
    {
        return new Prym.DTOs.Common.AddressDto
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
    private static Prym.DTOs.PriceLists.PriceListDto MapToPriceListDto(Data.Entities.PriceList.PriceList priceList)
    {
        return new Prym.DTOs.PriceLists.PriceListDto
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
            Status = (Prym.DTOs.Common.PriceListStatus)priceList.Status,
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

}
