using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<PagedResult<DocumentHeaderDto>> GetPagedDocumentHeadersAsync(
        DocumentHeaderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = BuildDocumentHeaderQuery(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        // Include related entities
        query = query.Include(dh => dh.DocumentType)
                     .Include(dh => dh.BusinessParty)
                     .Include(dh => dh.SourceWarehouse)
                     .Include(dh => dh.DestinationWarehouse)
                     .Include(dh => dh.PriceList);

        // Include Rows if requested
        if (queryParameters.IncludeRows)
        {
            query = query.Include(dh => dh.Rows.Where(r => !r.IsDeleted));
        }

        var items = await query
            .OrderByDescending(dh => dh.Date)
            .Skip(queryParameters.Skip)
            .Take(queryParameters.PageSize)
            .Select(dh => dh.ToDto())
            .ToListAsync(cancellationToken);

        return new PagedResult<DocumentHeaderDto>
        {
            Items = items,
            Page = queryParameters.Page,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DocumentHeaderDto?> GetDocumentHeaderByIdAsync(
        Guid id,
        bool includeRows = false,
        CancellationToken cancellationToken = default)
    {
        var query = context.DocumentHeaders
            .AsNoTracking()
            .Include(dh => dh.DocumentType)
            .Include(dh => dh.PriceList)
            .Where(dh => dh.Id == id && !dh.IsDeleted);

        if (includeRows)
        {
            query = query.Include(dh => dh.Rows.Where(r => !r.IsDeleted));
        }

        var documentHeader = await query.FirstOrDefaultAsync(cancellationToken);

        if (documentHeader is null)
        {
            logger.LogWarning("Document header with ID {Id} not found.", id);
            return null;
        }

        return documentHeader.ToDto();
    }

    public async Task<IEnumerable<DocumentHeaderDto>> GetDocumentHeadersByBusinessPartyAsync(
        Guid businessPartyId,
        CancellationToken cancellationToken = default)
    {
        var documentHeaders = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.BusinessPartyId == businessPartyId && !dh.IsDeleted)
            .OrderByDescending(dh => dh.Date)
            .Include(dh => dh.DocumentType)
            .Select(dh => dh.ToDto())
            .ToListAsync(cancellationToken);

        return documentHeaders;
    }

}
