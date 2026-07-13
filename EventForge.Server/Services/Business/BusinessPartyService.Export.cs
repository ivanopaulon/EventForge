using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<IEnumerable<Prym.DTOs.Export.BusinessPartyExportDto>> GetBusinessPartiesForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var query = context.BusinessParties
            .AsNoTracking()
            .Include(bp => bp.Addresses)
            .Include(bp => bp.Contacts)
            .Where(bp => !bp.IsDeleted && bp.TenantId == currentTenantId.Value)
            .OrderBy(bp => bp.Name);

        var totalCount = await query.CountAsync(ct);


        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetBusinessPartiesInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(bp => new Prym.DTOs.Export.BusinessPartyExportDto
        {
            Id = bp.Id,
            Code = bp.TaxCode ?? string.Empty,
            Name = bp.Name,
            PartyType = bp.PartyType.ToString(),
            VatNumber = bp.VatNumber,
            FiscalCode = bp.TaxCode,
            Email = bp.Contacts.FirstOrDefault(c => c.ContactType == Prym.DTOs.Common.ContactType.Email)?.Value,
            Phone = bp.Contacts.FirstOrDefault(c => c.ContactType == Prym.DTOs.Common.ContactType.Phone)?.Value,
            Address = bp.Addresses.FirstOrDefault()?.Street,
            City = bp.Addresses.FirstOrDefault()?.City,
            PostalCode = bp.Addresses.FirstOrDefault()?.ZipCode,
            Country = bp.Addresses.FirstOrDefault()?.Country,
            IsActive = bp.IsActive,
            CreatedAt = bp.CreatedAt
        });
    }

    private async Task<IEnumerable<Prym.DTOs.Export.BusinessPartyExportDto>> GetBusinessPartiesInBatchesAsync(
        IQueryable<BusinessParty> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.BusinessPartyExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(bp => new Prym.DTOs.Export.BusinessPartyExportDto
            {
                Id = bp.Id,
                Code = bp.TaxCode ?? string.Empty,
                Name = bp.Name,
                PartyType = bp.PartyType.ToString(),
                VatNumber = bp.VatNumber,
                FiscalCode = bp.TaxCode,
                Email = bp.Contacts.FirstOrDefault(c => c.ContactType == Prym.DTOs.Common.ContactType.Email)?.Value,
                Phone = bp.Contacts.FirstOrDefault(c => c.ContactType == Prym.DTOs.Common.ContactType.Phone)?.Value,
                Address = bp.Addresses.FirstOrDefault()?.Street,
                City = bp.Addresses.FirstOrDefault()?.City,
                PostalCode = bp.Addresses.FirstOrDefault()?.ZipCode,
                Country = bp.Addresses.FirstOrDefault()?.Country,
                IsActive = bp.IsActive,
                CreatedAt = bp.CreatedAt
            }));

            skip += batchSize;

        }

        return results;
    }

}
