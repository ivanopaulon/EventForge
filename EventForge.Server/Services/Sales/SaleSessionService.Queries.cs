using EventForge.Server.Data.Entities.Sales;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;
using Prym.DTOs.Promotions;
using Prym.DTOs.Sales;


namespace EventForge.Server.Services.Sales;

public partial class SaleSessionService
{
    public async Task<PagedResult<SaleSessionDto>> GetPOSSessionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var baseQuery = context.SaleSessions
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Use AsSplitQuery to prevent cartesian explosion with multiple collections
        var sessions = await baseQuery
            .AsSplitQuery()
            .Include(s => s.Items.Where(i => !i.IsDeleted))
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
        var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
        var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

        var operators = await context.StoreUsers
            .AsNoTracking()
            .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var poses = await context.StorePoses
            .AsNoTracking()
            .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // Load all products for all sessions in a single batch
        var allProductIds = sessions
            .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var allProducts = await context.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var dtos = new List<SaleSessionDto>();
        foreach (var session in sessions)
        {
            var dto = MapToDtoWithProducts(session, allProducts);
            dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
            dto.PosName = poses.GetValueOrDefault(session.PosId);
            dtos.Add(dto);
        }

        return new PagedResult<SaleSessionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<SaleSessionDto>> GetSessionsByOperatorAsync(Guid operatorId, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var baseQuery = context.SaleSessions
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value && !s.IsDeleted && s.OperatorId == operatorId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Use AsSplitQuery to prevent cartesian explosion with multiple collections
        var sessions = await baseQuery
            .AsSplitQuery()
            .Include(s => s.Items.Where(i => !i.IsDeleted))
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
        var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

        var operatorName = await context.StoreUsers
            .AsNoTracking()
            .Where(u => u.Id == operatorId && u.TenantId == currentTenantId.Value)
            .Select(u => u.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var poses = await context.StorePoses
            .AsNoTracking()
            .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // Load all products for all sessions in a single batch
        var allProductIds = sessions
            .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var allProducts = await context.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var dtos = new List<SaleSessionDto>();
        foreach (var session in sessions)
        {
            var dto = MapToDtoWithProducts(session, allProducts);
            dto.OperatorName = operatorName;
            dto.PosName = poses.GetValueOrDefault(session.PosId);
            dtos.Add(dto);
        }

        return new PagedResult<SaleSessionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<SaleSessionDto>> GetSessionsByDateAsync(DateTime startDate, DateTime? endDate, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var end = endDate ?? DateTime.UtcNow;

        var baseQuery = context.SaleSessions
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                && !s.IsDeleted
                && s.CreatedAt >= startDate
                && s.CreatedAt <= end);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Use AsSplitQuery to prevent cartesian explosion with multiple collections
        var sessions = await baseQuery
            .AsSplitQuery()
            .Include(s => s.Items.Where(i => !i.IsDeleted))
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
        var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
        var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

        var operators = await context.StoreUsers
            .AsNoTracking()
            .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var poses = await context.StorePoses
            .AsNoTracking()
            .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // Load all products for all sessions in a single batch
        var allProductIds = sessions
            .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var allProducts = await context.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var dtos = new List<SaleSessionDto>();
        foreach (var session in sessions)
        {
            var dto = MapToDtoWithProducts(session, allProducts);
            dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
            dto.PosName = poses.GetValueOrDefault(session.PosId);
            dtos.Add(dto);
        }

        return new PagedResult<SaleSessionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<PagedResult<SaleSessionDto>> GetOpenSessionsAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for sale session operations.");
        }

        var baseQuery = context.SaleSessions
            .AsNoTracking()
            .Where(s => s.TenantId == currentTenantId.Value
                && !s.IsDeleted
                && !s.ClosedAt.HasValue); // Session still open

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Use AsSplitQuery to prevent cartesian explosion with multiple collections
        var sessions = await baseQuery
            .AsSplitQuery()
            .Include(s => s.Items.Where(i => !i.IsDeleted))
            .Include(s => s.Payments.Where(p => !p.IsDeleted))
            .Include(s => s.Notes).ThenInclude(n => n.NoteFlag)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        // Load Operator and POS names in a single query (no IsDeleted filter for historical data integrity)
        var operatorIds = sessions.Select(s => s.OperatorId).Distinct().ToList();
        var posIds = sessions.Select(s => s.PosId).Distinct().ToList();

        var operators = await context.StoreUsers
            .AsNoTracking()
            .Where(u => operatorIds.Contains(u.Id) && u.TenantId == currentTenantId.Value)
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var poses = await context.StorePoses
            .AsNoTracking()
            .Where(p => posIds.Contains(p.Id) && p.TenantId == currentTenantId.Value)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        // Load all products for all sessions in a single batch
        var allProductIds = sessions
            .SelectMany(s => s.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var allProducts = await context.Products
            .AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id) && !p.IsDeleted)
            .Include(p => p.Brand)
            .Include(p => p.VatRate)
            .Include(p => p.ImageDocument)
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        var dtos = new List<SaleSessionDto>();
        foreach (var session in sessions)
        {
            var dto = MapToDtoWithProducts(session, allProducts);
            dto.OperatorName = operators.GetValueOrDefault(session.OperatorId);
            dto.PosName = poses.GetValueOrDefault(session.PosId);
            dtos.Add(dto);
        }

        return new PagedResult<SaleSessionDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

}
