using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<PagedResult<Prym.DTOs.Documents.DocumentHeaderDto>> GetBusinessPartyDocumentsAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? documentTypeId = null,
        string? searchNumber = null,
        PaginationParameters pagination = default!,
        CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for business party operations.");
        }

        var query = context.DocumentHeaders
            .AsNoTracking()
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

        var totalCount = await query.CountAsync(cancellationToken);

        var documents = await query
            .OrderByDescending(dh => dh.Date)
            .ThenByDescending(dh => dh.CreatedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(dh => new Prym.DTOs.Documents.DocumentHeaderDto
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
                Status = (Prym.DTOs.Common.DocumentStatus)dh.Status,
                CreatedAt = dh.CreatedAt,
                CreatedBy = dh.CreatedBy,
                ModifiedAt = dh.ModifiedAt,
                ModifiedBy = dh.ModifiedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<Prym.DTOs.Documents.DocumentHeaderDto>
        {
            Items = documents,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = totalCount
        };
    }

}
