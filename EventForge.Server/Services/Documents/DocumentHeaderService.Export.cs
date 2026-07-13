using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{

    public async Task<IEnumerable<Prym.DTOs.Export.DocumentExportDto>> GetDocumentsForExportAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for document operations.");
        }

        var query = context.DocumentHeaders
            .AsNoTracking()
            .Include(d => d.DocumentType)
            .Include(d => d.BusinessParty)
            .Where(d => !d.IsDeleted && d.TenantId == currentTenantId.Value)
            .OrderBy(d => d.Date);

        var totalCount = await query.CountAsync(ct);


        // Use batch processing for large datasets
        if (totalCount > 10000)
        {
            logger.LogWarning("Large export: {Count} records. Using batch processing.", totalCount);
            return await GetDocumentsInBatchesAsync(query, ct);
        }

        // Standard export for smaller datasets
        var items = await query
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return items.Select(d => new Prym.DTOs.Export.DocumentExportDto
        {
            Id = d.Id,
            DocumentNumber = d.Number,
            DocumentType = d.DocumentType?.Name ?? string.Empty,
            DocumentDate = d.Date,
            BusinessParty = d.BusinessParty?.Name ?? string.Empty,
            TotalAmount = d.TotalGrossAmount,
            TotalVat = d.VatAmount,
            NetAmount = d.TotalNetAmount,
            Status = d.Status.ToString(),
            Notes = d.Notes,
            CreatedAt = d.CreatedAt
        });
    }

    private async Task<IEnumerable<Prym.DTOs.Export.DocumentExportDto>> GetDocumentsInBatchesAsync(
        IQueryable<DocumentHeader> query,
        CancellationToken ct)
    {
        const int batchSize = 5000;
        var results = new List<Prym.DTOs.Export.DocumentExportDto>();
        var skip = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var batch = await query
                .Skip(skip)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            results.AddRange(batch.Select(d => new Prym.DTOs.Export.DocumentExportDto
            {
                Id = d.Id,
                DocumentNumber = d.Number,
                DocumentType = d.DocumentType?.Name ?? string.Empty,
                DocumentDate = d.Date,
                BusinessParty = d.BusinessParty?.Name ?? string.Empty,
                TotalAmount = d.TotalGrossAmount,
                TotalVat = d.VatAmount,
                NetAmount = d.TotalNetAmount,
                Status = d.Status.ToString(),
                Notes = d.Notes,
                CreatedAt = d.CreatedAt
            }));

            skip += batchSize;

        }

        return results;
    }

}
