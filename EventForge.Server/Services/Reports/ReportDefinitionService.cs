using EventForge.Server.Data.Entities.Reports;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace EventForge.Server.Services.Reports;

/// <summary>
/// Implementation of <see cref="IReportDefinitionService"/>.
/// Handles CRUD for Bold Reports report definitions and serves data source payloads
/// consumed by the JavaScript designer/viewer.
/// </summary>
public class ReportDefinitionService(
    EventForgeDbContext context,
    ITenantContext tenantContext,
    ILogger<ReportDefinitionService> logger) : IReportDefinitionService
{
    // ── CRUD ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<PagedResult<ReportListItemDto>> GetReportsAsync(
        string? category    = null,
        string? searchTerm  = null,
        int     page        = 1,
        int     pageSize    = 25,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var query = context.ReportDefinitions
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category == category);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(term) ||
                (r.Description != null && r.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportListItemDto
            {
                Id              = r.Id,
                Name            = r.Name,
                Description     = r.Description,
                Category        = r.Category,
                HasDesign       = r.ReportContent != null && r.ReportContent.Length > 0,
                IsPublic        = r.IsPublic,
                IsActive        = r.IsActive,
                CreatedAt       = r.CreatedAt,
                CreatedBy       = r.CreatedBy,
                ModifiedAt      = r.ModifiedAt,
                DataSourceCount = r.DataSources.Count(ds => !ds.IsDeleted),
            })
            .ToListAsync(ct);

        return new PagedResult<ReportListItemDto>
        {
            Items      = items,
            Page       = page,
            PageSize   = pageSize,
            TotalCount = totalCount,
        };
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = GetTenantIdOrThrow();

        var report = await context.ReportDefinitions
            .AsNoTracking()
            .Include(r => r.DataSources.Where(ds => !ds.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted, ct);

        return report is null ? null : MapToDto(report);
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto> CreateReportAsync(CreateReportDto dto, CancellationToken ct = default)
    {
        var tenantId  = GetTenantIdOrThrow();
        var createdBy = tenantContext.CurrentUserId?.ToString() ?? "System";

        var report = new ReportDefinition
        {
            TenantId    = tenantId,
            Name        = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Category    = dto.Category?.Trim(),
            IsPublic    = dto.IsPublic,
            CreatedBy   = createdBy,
        };

        foreach (var dsDto in dto.DataSources)
        {
            report.DataSources.Add(new ReportDataSource
            {
                TenantId       = tenantId,
                DataSourceName = dsDto.DataSourceName.Trim(),
                EntityType     = dsDto.EntityType.Trim(),
                Description    = dsDto.Description?.Trim(),
                CreatedBy      = createdBy,
            });
        }

        context.ReportDefinitions.Add(report);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created ReportDefinition {ReportId} ({Name}) for tenant {TenantId}",
            report.Id, report.Name, tenantId);

        return MapToDto(report);
    }

    /// <inheritdoc/>
    public async Task<ReportDefinitionDto?> UpdateReportAsync(Guid id, UpdateReportDto dto, CancellationToken ct = default)
    {
        var tenantId   = GetTenantIdOrThrow();
        var modifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

        var report = await context.ReportDefinitions
            .Include(r => r.DataSources.Where(ds => !ds.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted, ct);

        if (report is null) return null;

        report.Name        = dto.Name.Trim();
        report.Description = dto.Description?.Trim();
        report.Category    = dto.Category?.Trim();
        report.IsPublic    = dto.IsPublic;
        report.IsActive    = dto.IsActive;
        report.ModifiedAt  = DateTime.UtcNow;
        report.ModifiedBy  = modifiedBy;

        // Update RDLC content only when the caller explicitly passes a value
        if (dto.ReportContent is not null)
            report.ReportContent = dto.ReportContent;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Updated ReportDefinition {ReportId} for tenant {TenantId}", id, tenantId);

        return MapToDto(report);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveReportContentAsync(Guid id, string rdlcContent, CancellationToken ct = default)
    {
        var tenantId   = GetTenantIdOrThrow();
        var modifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

        var report = await context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted, ct);

        if (report is null) return false;

        report.ReportContent = rdlcContent;
        report.ModifiedAt    = DateTime.UtcNow;
        report.ModifiedBy    = modifiedBy;

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Saved RDLC content for ReportDefinition {ReportId} (tenant {TenantId})", id, tenantId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteReportAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId   = GetTenantIdOrThrow();
        var deletedBy  = tenantContext.CurrentUserId?.ToString() ?? "System";

        var report = await context.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId && !r.IsDeleted, ct);

        if (report is null) return false;

        report.IsDeleted  = true;
        report.IsActive   = false;
        report.DeletedBy  = deletedBy;
        report.DeletedAt  = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted ReportDefinition {ReportId} for tenant {TenantId}", id, tenantId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var tenantId = GetTenantIdOrThrow();

        return await context.ReportDefinitions
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && !r.IsDeleted && r.Category != null)
            .Select(r => r.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    // ── Data source endpoints ────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<object> GetDataSourceDataAsync(
        string    entityType,
        DateTime? dateFrom = null,
        DateTime? dateTo   = null,
        CancellationToken ct = default)
    {
        var tenantId = GetTenantIdOrThrow();
        var from     = dateFrom ?? DateTime.UtcNow.AddMonths(-12);
        var to       = dateTo   ?? DateTime.UtcNow;

        return entityType switch
        {
            ReportDataSourceEntityTypes.DocumentHeaders => await GetDocumentHeadersDataAsync(tenantId, from, to, ct),
            ReportDataSourceEntityTypes.DocumentRows    => await GetDocumentRowsDataAsync(tenantId, from, to, ct),
            ReportDataSourceEntityTypes.Products        => await GetProductsDataAsync(tenantId, ct),
            ReportDataSourceEntityTypes.BusinessParties => await GetBusinessPartiesDataAsync(tenantId, ct),
            ReportDataSourceEntityTypes.Sales           => await GetSalesDataAsync(tenantId, from, to, ct),
            ReportDataSourceEntityTypes.Warehouse       => await GetWarehouseDataAsync(tenantId, ct),
            ReportDataSourceEntityTypes.Fiscal          => await GetFiscalDataAsync(tenantId, from, to, ct),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}", nameof(entityType)),
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private Guid GetTenantIdOrThrow()
    {
        return tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("Tenant context is required for report operations.");
    }

    private static ReportDefinitionDto MapToDto(ReportDefinition r) => new()
    {
        Id            = r.Id,
        TenantId      = r.TenantId,
        Name          = r.Name,
        Description   = r.Description,
        Category      = r.Category,
        ReportContent = r.ReportContent,
        IsPublic      = r.IsPublic,
        IsActive      = r.IsActive,
        CreatedAt     = r.CreatedAt,
        CreatedBy     = r.CreatedBy,
        ModifiedAt    = r.ModifiedAt,
        ModifiedBy    = r.ModifiedBy,
        DataSources   = r.DataSources.Select(ds => new ReportDataSourceDto
        {
            Id             = ds.Id,
            DataSourceName = ds.DataSourceName,
            EntityType     = ds.EntityType,
            Description    = ds.Description,
        }).ToList(),
    };

    // ── Data source queries ──────────────────────────────────────────────────

    private async Task<object> GetDocumentHeadersDataAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await context.DocumentHeaders
            .AsNoTracking()
            .Where(dh => dh.TenantId == tenantId && !dh.IsDeleted
                && dh.Date >= from && dh.Date <= to)
            .OrderByDescending(dh => dh.Date)
            .Select(dh => new
            {
                dh.Id,
                dh.Number,
                dh.Date,
                TotalAmount      = dh.TotalNetAmount,
                GrossAmount      = dh.TotalGrossAmount,
                TotalDiscount    = dh.TotalDiscount,
                BusinessPartyName = dh.BusinessParty != null ? dh.BusinessParty.Name : null,
                DocumentTypeName  = dh.DocumentType  != null ? dh.DocumentType.Name  : null,
                dh.Status,
                dh.CreatedAt,
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetDocumentRowsDataAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await context.DocumentRows
            .AsNoTracking()
            .Where(dr => dr.TenantId == tenantId && !dr.IsDeleted
                && dr.DocumentHeader != null
                && dr.DocumentHeader.Date >= from
                && dr.DocumentHeader.Date <= to)
            .OrderByDescending(dr => dr.CreatedAt)
            .Select(dr => new
            {
                dr.Id,
                dr.DocumentHeaderId,
                dr.ProductCode,
                dr.Description,
                dr.Quantity,
                dr.UnitPrice,
                LineTotal        = dr.UnitPrice * dr.Quantity,
                dr.LineDiscountValue,
                DocumentDate = dr.DocumentHeader != null ? dr.DocumentHeader.Date : (DateTime?)null,
                dr.CreatedAt,
            })
            .Take(10000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetProductsDataAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                DefaultPrice = p.DefaultPrice,
                p.IsActive,
                p.CreatedAt,
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetBusinessPartiesDataAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await context.BusinessParties
            .AsNoTracking()
            .Where(bp => bp.TenantId == tenantId && !bp.IsDeleted)
            .OrderBy(bp => bp.Name)
            .Select(bp => new
            {
                bp.Id,
                bp.Name,
                FiscalCode = bp.TaxCode,
                bp.VatNumber,
                PartyType  = bp.PartyType.ToString(),
                bp.IsActive,
                bp.CreatedAt,
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetSalesDataAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await context.SaleSessions
            .AsNoTracking()
            .Where(ss => ss.TenantId == tenantId && !ss.IsDeleted
                && ss.CreatedAt >= from && ss.CreatedAt <= to)
            .OrderByDescending(ss => ss.CreatedAt)
            .Select(ss => new
            {
                ss.Id,
                TotalAmount = ss.FinalTotal,
                Status      = ss.Status.ToString(),
                ss.CreatedAt,
                ss.ClosedAt,
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetWarehouseDataAsync(Guid tenantId, CancellationToken ct)
    {
        var rows = await context.Stocks
            .AsNoTracking()
            .Where(se => se.TenantId == tenantId && !se.IsDeleted)
            .OrderBy(se => se.ProductId)
            .Select(se => new
            {
                se.Id,
                se.ProductId,
                LocationId  = se.StorageLocationId,
                Quantity    = se.Quantity,
                ReservedQty = se.ReservedQuantity,
                se.ReorderPoint,
                se.LastMovementDate,
                LastUpdated = se.ModifiedAt,
            })
            .Take(5000)
            .ToListAsync(ct);

        return rows;
    }

    private async Task<object> GetFiscalDataAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await context.DailyClosureRecords
            .AsNoTracking()
            .Where(dcr => dcr.TenantId == tenantId && !dcr.IsDeleted
                && dcr.ClosedAt >= from && dcr.ClosedAt <= to)
            .OrderByDescending(dcr => dcr.ClosedAt)
            .Select(dcr => new
            {
                dcr.Id,
                ClosureDate    = dcr.ClosedAt,
                dcr.TotalAmount,
                dcr.ZReportNumber,
                dcr.ReceiptCount,
                dcr.CreatedAt,
            })
            .Take(1000)
            .ToListAsync(ct);

        return rows;
    }
}
